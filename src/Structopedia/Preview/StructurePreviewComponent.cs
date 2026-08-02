using System;
using Structopedia.Schematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Structopedia.Preview;

/// <summary>
/// Rich text component that draws a structure as a 3D mesh inside the handbook text flow, with an
/// orbital camera driven by mouse drag and a slider that cuts the structure at a layer.
/// </summary>
/// <remarks>
/// Structure copied from <c>SlideshowItemstackTextComponent</c> (bounds, scissor, render hook) and
/// <c>InventoryItemRenderer.RenderItemstackToGui</c> (matrix build and uniform reset).
/// </remarks>
internal sealed class StructurePreviewComponent : RichTextComponentBase
{
    /// <summary>Unscaled height of the 3D viewport.</summary>
    private const double UnscaledViewportHeight = 320.0;

    /// <summary>Unscaled height reserved under the viewport for the slider and its label.</summary>
    private const double UnscaledSliderHeight = 34.0;

    /// <summary>Unscaled gap between the viewport and the slider track.</summary>
    private const double UnscaledTrackTopGap = 6.0;

    /// <summary>Unscaled thickness of the slider track.</summary>
    private const double UnscaledTrackThickness = 4.0;

    /// <summary>Unscaled inset of the track from both ends of the component.</summary>
    private const double UnscaledTrackInset = 6.0;

    /// <summary>Unscaled width of the slider handle.</summary>
    private const double UnscaledHandleWidth = 9.0;

    /// <summary>Unscaled height of the slider handle, centred on the track.</summary>
    private const double UnscaledHandleHeight = 16.0;

    /// <summary>Unscaled gap between the track and the label under it.</summary>
    private const double UnscaledLabelTopGap = 9.0;

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

    /// <summary>Side of the flat white texture the slider is painted with.</summary>
    private const int FillTextureSize = 2;

    private static readonly Vec4f TrackColor = new Vec4f(0.09f, 0.08f, 0.07f, 0.75f);
    private static readonly Vec4f FilledColor = new Vec4f(0.45f, 0.38f, 0.27f, 0.9f);
    private static readonly Vec4f HandleColor = new Vec4f(0.87f, 0.83f, 0.72f, 1f);
    private static readonly Vec4f OpaqueWhite = new Vec4f(1f, 1f, 1f, 1f);

    private readonly ICoreClientAPI capi;
    private readonly Func<PreviewEntry?> entryProvider;
    private readonly Matrixf modelMat = new Matrixf();

    private float yaw = InitialYaw;
    private float pitch = InitialPitch;
    private float zoom;
    private float baseZoom;

    private bool rotating;
    private bool zooming;
    private bool slicing;
    private int lastMouseX;
    private int lastMouseY;

    private double lastRenderX;
    private double lastRenderY;
    private double lastRenderWidth;
    private double lastRenderHeight;
    private long lastRenderMs = long.MinValue;

    private bool wheelSubscribed;

    /// <summary>Entry the cutoff belongs to, so another structure starts whole again.</summary>
    private PreviewEntry? cutoffOwner;
    private int cutoffLayer;

    private LoadedTexture? fillTexture;
    private LoadedTexture? labelTexture;
    private int labelLayer = int.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructurePreviewComponent"/> class, with the
    /// camera back at its resting angles.
    /// </summary>
    /// <param name="api">Client API.</param>
    /// <param name="entryProvider">
    /// Hands out the preview to draw, or null when there is none. Called on every frame, so the owner
    /// stays free to build it late or to drop it.
    /// </param>
    internal StructurePreviewComponent(ICoreClientAPI api, Func<PreviewEntry?> entryProvider)
        : base(api)
    {
        capi = api;
        this.entryProvider = entryProvider;

        // Own line in the flow: GuiElementRichtext advances posY by our full height for EnumFloat.None.
        Float = EnumFloat.None;
        VerticalAlign = EnumVerticalAlign.Top;
        BoundsPerLine = new LineRectangled[1] { new LineRectangled(0.0, 0.0, 0.0, ComponentHeight) };

        capi.Event.MouseWheelMove += OnMouseWheelMove;
        wheelSubscribed = true;
    }

    /// <summary>Scaled height of the whole component, viewport plus slider strip.</summary>
    private static double ComponentHeight => GuiElement.scaled(UnscaledViewportHeight + UnscaledSliderHeight);

    /// <summary>Left end of the slider track, in screen space.</summary>
    private double TrackX => lastRenderX + GuiElement.scaled(UnscaledTrackInset);

    /// <summary>Width of the slider track, in screen space.</summary>
    private double TrackWidth => Math.Max(0.0, lastRenderWidth - (2.0 * GuiElement.scaled(UnscaledTrackInset)));

    /// <summary>Top of the slider track, in screen space.</summary>
    private double TrackY => lastRenderY + GuiElement.scaled(UnscaledViewportHeight + UnscaledTrackTopGap);

    /// <inheritdoc/>
    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        TextFlowPath? section = GetCurrentFlowPathSection(flowPath, lineY);
        double x1 = section?.X1 ?? 0.0;
        double x2 = section?.X2 ?? 0.0;

        BoundsPerLine = new LineRectangled[1]
        {
            new LineRectangled(x1, lineY, Math.Max(0.0, x2 - x1), ComponentHeight)
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

        PreviewEntry? entry = entryProvider();
        if (entry == null || !entry.IsUsable)
        {
            return;
        }

        AdoptEntry(entry);
        EnsureZoom(entry, rect);
        RenderLayers(entry, renderZ);
        RenderSlider(entry, renderZ);
    }

    /// <inheritdoc/>
    public override void OnMouseDown(MouseEvent args)
    {
        // Coordinates are local to the richtext element, so they line up with BoundsPerLine directly
        // (GuiElementRichtext.OnMouseDownOnElement subtracts Bounds.absX / absY before dispatching).
        LineRectangled rect = BoundsPerLine[0];
        if (!rect.PointInside(args.X, args.Y))
        {
            return;
        }

        if (args.Button != EnumMouseButton.Left && args.Button != EnumMouseButton.Right)
        {
            return;
        }

        // The slider strip is not part of the viewport: a drag there cuts layers and never rotates.
        if (args.Y - rect.Y >= GuiElement.scaled(UnscaledViewportHeight))
        {
            if (args.Button == EnumMouseButton.Left)
            {
                slicing = true;
                SetCutoffFromPointer();
                args.Handled = true;
            }

            return;
        }

        if (args.Button == EnumMouseButton.Left)
        {
            rotating = true;
        }
        else
        {
            zooming = true;
        }

        lastMouseX = capi.Input.MouseX;
        lastMouseY = capi.Input.MouseY;
        args.Handled = true;
    }

    /// <inheritdoc/>
    public override void OnMouseMove(MouseEvent args)
    {
        if (slicing)
        {
            SetCutoffFromPointer();
            args.Handled = true;
            return;
        }

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
        if (!rotating && !zooming && !slicing)
        {
            return;
        }

        rotating = false;
        zooming = false;
        slicing = false;
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

        fillTexture?.Dispose();
        fillTexture = null;
        labelTexture?.Dispose();
        labelTexture = null;

        // The layer meshes belong to the preview store and outlive this component.
        base.Dispose();
    }

    private static float Wrap360(float degrees)
    {
        float wrapped = degrees % 360f;
        return wrapped < 0f ? wrapped + 360f : wrapped;
    }

    /// <summary>Draws every layer at or below the cut, clipped to the viewport.</summary>
    private void RenderLayers(PreviewEntry entry, double renderZ)
    {
        double viewportHeight = GuiElement.scaled(UnscaledViewportHeight);

        // Same construction as SlideshowItemstackTextComponent: a window-parented bounds positioned at
        // the viewport rectangle, pushed with stacking so it intersects the handbook clip region.
        ElementBounds clipBounds = ElementBounds.FixedSize(
            (int)(lastRenderWidth / RuntimeEnv.GUIScale),
            (int)(viewportHeight / RuntimeEnv.GUIScale));
        clipBounds.ParentBounds = capi.Gui.WindowBounds;
        clipBounds.CalcWorldBounds();
        clipBounds.absFixedX = lastRenderX;
        clipBounds.absFixedY = lastRenderY;

        capi.Render.PushScissor(clipBounds, stacking: true);

        modelMat
            .Identity()
            .Translate(lastRenderX + (lastRenderWidth / 2.0), lastRenderY + (viewportHeight / 2.0), renderZ + RenderZOffset)
            .Scale(zoom, -zoom, zoom)
            .RotateXDeg(pitch)
            .RotateYDeg(yaw)
            .Translate(-entry.SizeX / 2f, -entry.SizeY / 2f, -entry.SizeZ / 2f);

        IShaderProgram prog = capi.Render.CurrentActiveShader;

        // Order matters: modelMatrix is uploaded before ReverseMul, which mutates modelMat in place.
        prog.UniformMatrix("modelMatrix", modelMat.Values);
        prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
        prog.UniformMatrix("modelViewMatrix", modelMat.ReverseMul(capi.Render.CurrentModelviewMatrix).Values);
        prog.Uniform("applyModelMat", 1);
        prog.Uniform("applyColor", 0);
        prog.Uniform("rgbaIn", OpaqueWhite);
        prog.Uniform("normalShaded", 1);
        prog.Uniform("alphaTest", 0.05f);
        prog.Uniform("extraGlow", 0);
        prog.Uniform("overlayOpacity", 0f);

        // The GUI pass leaves culling off; the cull mode itself stays at the GL default (back).
        capi.Render.GlEnableCullFace();

        foreach (PreviewLayer layer in entry.Layers)
        {
            if (layer.Y > cutoffLayer)
            {
                // The builder emits layers from the ground up, so the first one above the cut ends it.
                break;
            }

            capi.Render.RenderMultiTextureMesh(layer.MeshRef, "tex2d");
        }

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

    /// <summary>
    /// Draws the slider under the viewport: the track, the part of it below the cut, the handle and
    /// the label saying where the cut sits.
    /// </summary>
    /// <remarks>
    /// The parts go through <c>Render2DTexture</c> over a flat white texture rather than through
    /// <c>RenderRectangle</c>, which draws a line loop and could only outline them. That call leaves
    /// <c>rgbaIn</c> holding the colour it was handed, and the gui program is shared with every other
    /// element on the page, so it is put back to white straight after.
    /// </remarks>
    private void RenderSlider(PreviewEntry entry, double renderZ)
    {
        LoadedTexture fill = EnsureFillTexture();
        float z = (float)(renderZ + RenderZOffset);

        double trackX = TrackX;
        double trackWidth = TrackWidth;
        double trackY = TrackY;
        double thickness = GuiElement.scaled(UnscaledTrackThickness);
        double handleX = LayerCutoff.ToPixel(cutoffLayer, trackX, trackWidth, entry.MinLayer, entry.MaxLayer);
        double handleWidth = GuiElement.scaled(UnscaledHandleWidth);
        double handleHeight = GuiElement.scaled(UnscaledHandleHeight);

        capi.Render.Render2DTexture(
            fill.TextureId, (float)trackX, (float)trackY, (float)trackWidth, (float)thickness, z, TrackColor);
        capi.Render.Render2DTexture(
            fill.TextureId, (float)trackX, (float)trackY, (float)(handleX - trackX), (float)thickness, z, FilledColor);
        capi.Render.Render2DTexture(
            fill.TextureId,
            (float)(handleX - (handleWidth / 2.0)),
            (float)(trackY + (thickness / 2.0) - (handleHeight / 2.0)),
            (float)handleWidth,
            (float)handleHeight,
            z,
            HandleColor);

        capi.Render.CurrentActiveShader.Uniform("rgbaIn", OpaqueWhite);

        capi.Render.Render2DLoadedTexture(
            EnsureLabelTexture(entry),
            (float)trackX,
            (float)(trackY + thickness + GuiElement.scaled(UnscaledLabelTopGap)),
            z);
    }

    /// <summary>
    /// Notices that another structure is on screen and puts the cut back at the top, which is the
    /// whole structure. Anything else would open a page on a view of half a building.
    /// </summary>
    private void AdoptEntry(PreviewEntry entry)
    {
        if (ReferenceEquals(cutoffOwner, entry))
        {
            return;
        }

        cutoffOwner = entry;
        cutoffLayer = entry.MaxLayer;
        baseZoom = 0f;
    }

    private void SetCutoffFromPointer()
    {
        if (cutoffOwner is not PreviewEntry entry)
        {
            return;
        }

        cutoffLayer = LayerCutoff.FromPixel(capi.Input.MouseX, TrackX, TrackWidth, entry.MinLayer, entry.MaxLayer);
    }

    /// <summary>
    /// Uploads the flat white texture the slider is painted with, once. Raw pixels rather than an
    /// asset: it keeps the mod from shipping an image to draw three rectangles.
    /// </summary>
    private LoadedTexture EnsureFillTexture()
    {
        if (fillTexture != null)
        {
            return fillTexture;
        }

        var pixels = new int[FillTextureSize * FillTextureSize];
        Array.Fill(pixels, unchecked((int)0xFFFFFFFF));

        LoadedTexture texture = new LoadedTexture(capi, 0, FillTextureSize, FillTextureSize);
        capi.Render.LoadOrUpdateTextureFromRgba(pixels, linearMag: false, clampMode: 1, ref texture);

        fillTexture = texture;
        return texture;
    }

    private LoadedTexture EnsureLabelTexture(PreviewEntry entry)
    {
        if (labelTexture != null && labelLayer == cutoffLayer)
        {
            return labelTexture;
        }

        labelTexture?.Dispose();
        labelLayer = cutoffLayer;
        labelTexture = new TextTextureUtil(capi).GenTextTexture(
            Lang.Get(
                "structopedia:preview-layer",
                cutoffLayer - entry.MinLayer + 1,
                entry.MaxLayer - entry.MinLayer + 1),
            CairoFont.WhiteDetailText());

        return labelTexture;
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

        if (IsControlHeld() && cutoffOwner is PreviewEntry entry)
        {
            cutoffLayer = LayerCutoff.Step(cutoffLayer, Math.Sign(args.delta), entry.MinLayer, entry.MaxLayer);
        }
        else
        {
            ApplyZoom(args.delta * ZoomPerWheelStep);
        }

        args.SetHandled();
    }

    /// <summary>
    /// Reads the raw key state rather than the filtered one: an open dialog is exactly the case where
    /// the filtered state has already been claimed by something else.
    /// </summary>
    private bool IsControlHeld()
    {
        bool[] keys = capi.Input.KeyboardKeyStateRaw;
        return keys[(int)GlKeys.ControlLeft] || keys[(int)GlKeys.ControlRight];
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

    private void EnsureZoom(PreviewEntry entry, LineRectangled rect)
    {
        if (baseZoom > 0f)
        {
            return;
        }

        float diagonal = Math.Max(1f, entry.Diagonal);
        double viewport = Math.Min(rect.Width, GuiElement.scaled(UnscaledViewportHeight));
        baseZoom = (float)(viewport * FitRatio) / diagonal;
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
