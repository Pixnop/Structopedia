using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Structopedia.Preview;

/// <summary>
/// Rich text component that draws a structure as a 3D mesh inside the handbook text flow, with an
/// orbital camera driven by mouse drag.
/// </summary>
/// <remarks>
/// Structure copied from <c>SlideshowItemstackTextComponent</c> (bounds, scissor, render hook) and
/// <c>InventoryItemRenderer.RenderItemstackToGui</c> (matrix build and uniform reset).
/// </remarks>
internal sealed class StructurePreviewComponent : RichTextComponentBase
{
    /// <summary>Unscaled height of the viewport reserved in the text flow.</summary>
    private const double UnscaledHeight = 320.0;

    /// <summary>
    /// Depth offset added to the rich text z. Mirrors the vanilla slideshow component, which draws its
    /// flat texture at <c>GuiElementRichtext.zPos</c> (50) and its 3D stack at 100.
    /// </summary>
    private const float RenderZOffset = 50f;

    /// <summary>Share of the viewport height the structure diagonal should occupy at rest.</summary>
    private const float FitRatio = 0.75f;

    private const float DegreesPerPixel = 0.75f;
    private const float ZoomPerPixel = 0.005f;
    private const float ZoomPerWheelStep = 0.12f;
    private const float MinZoomFactor = 0.35f;
    private const float MaxZoomFactor = 4f;
    private const float InitialYaw = 45f;
    private const float InitialPitch = -25f;

    /// <summary>How long after the last frame the component still claims the wheel.</summary>
    private const long HoverGraceMs = 250L;

    private readonly ICoreClientAPI capi;
    private readonly Func<PreviewMesh?> meshProvider;
    private readonly Matrixf modelMat = new Matrixf();

    private float yaw = InitialYaw;
    private float pitch = InitialPitch;
    private float zoom;
    private float baseZoom;

    private bool rotating;
    private bool zooming;
    private int lastMouseX;
    private int lastMouseY;

    private double lastRenderX;
    private double lastRenderY;
    private double lastRenderWidth;
    private double lastRenderHeight;
    private long lastRenderMs = long.MinValue;

    private bool wheelSubscribed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructurePreviewComponent"/> class, with the
    /// camera back at its resting angles.
    /// </summary>
    /// <param name="api">Client API.</param>
    /// <param name="meshProvider">
    /// Hands out the mesh to draw, or null when there is none. Called on every frame, so the owner
    /// stays free to build it late or to drop it.
    /// </param>
    internal StructurePreviewComponent(ICoreClientAPI api, Func<PreviewMesh?> meshProvider)
        : base(api)
    {
        capi = api;
        this.meshProvider = meshProvider;

        // Own line in the flow: GuiElementRichtext advances posY by our full height for EnumFloat.None.
        Float = EnumFloat.None;
        VerticalAlign = EnumVerticalAlign.Top;
        BoundsPerLine = new LineRectangled[1] { new LineRectangled(0.0, 0.0, 0.0, GuiElement.scaled(UnscaledHeight)) };

        capi.Event.MouseWheelMove += OnMouseWheelMove;
        wheelSubscribed = true;
    }

    /// <inheritdoc/>
    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        TextFlowPath? section = GetCurrentFlowPathSection(flowPath, lineY);
        double x1 = section?.X1 ?? 0.0;
        double x2 = section?.X2 ?? 0.0;

        BoundsPerLine = new LineRectangled[1]
        {
            new LineRectangled(x1, lineY, Math.Max(0.0, x2 - x1), GuiElement.scaled(UnscaledHeight))
        };

        nextOffsetX = 0.0;
        return EnumCalcBoundsResult.Nextline;
    }

    /// <inheritdoc/>
    public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
    {
        LineRectangled rect = BoundsPerLine[0];
        lastRenderX = renderX + rect.X;
        lastRenderY = renderY + rect.Y;
        lastRenderWidth = rect.Width;
        lastRenderHeight = rect.Height;
        lastRenderMs = capi.ElapsedMilliseconds;

        if (rect.Width <= 0.0 || rect.Height <= 0.0)
        {
            return;
        }

        PreviewMesh? mesh = meshProvider();
        if (mesh == null || !mesh.IsUsable)
        {
            return;
        }

        EnsureZoom(mesh, rect);

        // Same construction as SlideshowItemstackTextComponent: a window-parented bounds positioned at
        // the component rectangle, pushed with stacking so it intersects the handbook clip region.
        ElementBounds clipBounds = ElementBounds.FixedSize(
            (int)(rect.Width / RuntimeEnv.GUIScale),
            (int)(rect.Height / RuntimeEnv.GUIScale));
        clipBounds.ParentBounds = capi.Gui.WindowBounds;
        clipBounds.CalcWorldBounds();
        clipBounds.absFixedX = lastRenderX;
        clipBounds.absFixedY = lastRenderY;

        capi.Render.PushScissor(clipBounds, stacking: true);

        modelMat
            .Identity()
            .Translate(lastRenderX + (rect.Width / 2.0), lastRenderY + (rect.Height / 2.0), renderZ + RenderZOffset)
            .Scale(zoom, -zoom, zoom)
            .RotateXDeg(pitch)
            .RotateYDeg(yaw)
            .Translate(-mesh.SizeX / 2f, -mesh.SizeY / 2f, -mesh.SizeZ / 2f);

        IShaderProgram prog = capi.Render.CurrentActiveShader;

        // Order matters: modelMatrix is uploaded before ReverseMul, which mutates modelMat in place.
        prog.UniformMatrix("modelMatrix", modelMat.Values);
        prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
        prog.UniformMatrix("modelViewMatrix", modelMat.ReverseMul(capi.Render.CurrentModelviewMatrix).Values);
        prog.Uniform("applyModelMat", 1);
        prog.Uniform("applyColor", 0);
        prog.Uniform("rgbaIn", new Vec4f(1f, 1f, 1f, 1f));
        prog.Uniform("normalShaded", 1);
        prog.Uniform("alphaTest", 0.05f);
        prog.Uniform("extraGlow", 0);
        prog.Uniform("overlayOpacity", 0f);

        // The GUI pass leaves culling off; the cull mode itself stays at the GL default (back).
        capi.Render.GlEnableCullFace();
        capi.Render.RenderMultiTextureMesh(mesh.MeshRef, "tex2d");
        capi.Render.GlDisableCullFace();

        // Same reset as RenderItemstackToGui: these uniforms are global to the gui program, leaving
        // them set corrupts every element drawn after us.
        prog.Uniform("applyModelMat", 0);
        prog.Uniform("normalShaded", 0);
        prog.Uniform("tempGlowMode", 0);
        prog.Uniform("damageEffect", 0f);
        prog.Uniform("alphaTest", 0f);
        prog.Uniform("rgbaGlowIn", new Vec4f(0f, 0f, 0f, 0f));

        capi.Render.PopScissor();
    }

    /// <inheritdoc/>
    public override void OnMouseDown(MouseEvent args)
    {
        // Coordinates are local to the richtext element, so they line up with BoundsPerLine directly
        // (GuiElementRichtext.OnMouseDownOnElement subtracts Bounds.absX / absY before dispatching).
        if (!BoundsPerLine[0].PointInside(args.X, args.Y))
        {
            return;
        }

        if (args.Button == EnumMouseButton.Left)
        {
            rotating = true;
        }
        else if (args.Button == EnumMouseButton.Right)
        {
            zooming = true;
        }
        else
        {
            return;
        }

        lastMouseX = capi.Input.MouseX;
        lastMouseY = capi.Input.MouseY;
        args.Handled = true;
    }

    /// <inheritdoc/>
    public override void OnMouseMove(MouseEvent args)
    {
        if (!rotating && !zooming)
        {
            return;
        }

        // Screen-space deltas: they stay correct even if the page scrolls mid-drag.
        int dx = capi.Input.MouseX - lastMouseX;
        int dy = capi.Input.MouseY - lastMouseY;
        lastMouseX = capi.Input.MouseX;
        lastMouseY = capi.Input.MouseY;

        if (rotating)
        {
            yaw = Wrap360(yaw + (dx * DegreesPerPixel));
            pitch = GameMath.Clamp(pitch + (dy * DegreesPerPixel), -89f, 89f);
        }
        else
        {
            ApplyZoom(-dy * ZoomPerPixel);
        }

        args.Handled = true;
    }

    /// <inheritdoc/>
    public override void OnMouseUp(MouseEvent args)
    {
        if (!rotating && !zooming)
        {
            return;
        }

        rotating = false;
        zooming = false;
        args.Handled = true;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (wheelSubscribed)
        {
            capi.Event.MouseWheelMove -= OnMouseWheelMove;
            wheelSubscribed = false;
        }

        // The mesh ref belongs to the preview store and outlives this component.
        base.Dispose();
    }

    private static float Wrap360(float degrees)
    {
        float wrapped = degrees % 360f;
        return wrapped < 0f ? wrapped + 360f : wrapped;
    }

    /// <summary>
    /// RichTextComponentBase has no wheel hook, but ClientMain.OnMouseWheel triggers
    /// IClientEventAPI.MouseWheelMove before handing the event to any client system, GuiManager
    /// included. Marking it handled here keeps the handbook from scrolling under the cursor.
    /// </summary>
    private void OnMouseWheelMove(MouseWheelEventArgs args)
    {
        if (args.IsHandled || !IsHovered())
        {
            return;
        }

        ApplyZoom(args.delta * ZoomPerWheelStep);
        args.SetHandled();
    }

    /// <summary>
    /// The wheel event is global, so hovering is tested against the rectangle of the last frame we
    /// actually drew. A stale component (page left, dialog closed) stops claiming the wheel.
    /// </summary>
    private bool IsHovered()
    {
        if (baseZoom <= 0f || capi.ElapsedMilliseconds - lastRenderMs > HoverGraceMs)
        {
            return false;
        }

        double mx = capi.Input.MouseX;
        double my = capi.Input.MouseY;
        return mx >= lastRenderX && mx <= lastRenderX + lastRenderWidth
            && my >= lastRenderY && my <= lastRenderY + lastRenderHeight;
    }

    private void EnsureZoom(PreviewMesh mesh, LineRectangled rect)
    {
        if (baseZoom > 0f)
        {
            return;
        }

        float diagonal = Math.Max(1f, mesh.Diagonal);
        baseZoom = (float)(Math.Min(rect.Width, rect.Height) * FitRatio) / diagonal;
        zoom = baseZoom;
    }

    private void ApplyZoom(float exponent)
    {
        if (baseZoom <= 0f)
        {
            return;
        }

        zoom = GameMath.Clamp(zoom * MathF.Exp(exponent), baseZoom * MinZoomFactor, baseZoom * MaxZoomFactor);
    }
}
