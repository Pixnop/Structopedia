using System;

namespace Structopedia.Preview;

/// <summary>
/// Places a preview mesh along the depth axis of the gui pass, far enough forward that a zoomed
/// structure stays whole instead of being sliced by the far plane.
/// </summary>
/// <remarks>
/// <para>
/// The gui pass sets its own projection up in <c>ClientMain.OrthoMode</c> (decompiled 1.22.6,
/// <c>Vintagestory.Client.NoObf/ClientMain.cs:1554</c>). It loads
/// <c>GlOrtho(0, width, height, 0, 0.4, 20001)</c> (line 1565) and then translates the model view by
/// <c>-19849</c> along z (line 1571). An orthographic projection keeps eye space z between
/// <c>-zFar</c> and <c>-zNear</c>, so a gui z survives the clip when
/// </para>
/// <code>
/// -20001 &lt;= z - 19849 &lt;= -0.4    which is    -152 &lt;= z &lt;= 19848.6
/// </code>
/// <para>
/// The far end is the tight one. Gui elements are flat and sit at a z of 50 to a few hundred, so
/// nothing vanilla ever comes close to it, but a structure drawn at the zoom ceiling is around a
/// thousand units deep and its back half falls straight through. The clip happens in clip space,
/// along the view axis, which is why the cut turns with the camera rather than staying put on the
/// structure. Pushing the centre forward until the back of the mesh clears the far plane fixes it,
/// and the near end has some nineteen thousand units to spare for whatever the push costs.
/// </para>
/// </remarks>
internal static class PreviewDepth
{
    /// <summary>
    /// Depth offset added to the rich text z, which is where a mesh rests when nothing pushes it
    /// forward. Mirrors the vanilla slideshow component, which draws its flat texture at
    /// <c>GuiElementRichtext.zPos</c> (50) and its 3D stack at 100.
    /// </summary>
    internal const double RenderZOffset = 50.0;

    /// <summary>
    /// How far behind gui z zero the far plane of the gui pass sits, from the <c>20001</c> far
    /// distance of <c>GlOrtho</c> minus the <c>19849</c> the model view is translated by.
    /// </summary>
    internal const double FarPlaneHeadroom = 152.0;

    /// <summary>
    /// How far in front of gui z zero the near plane of the gui pass sits, from the <c>19849</c>
    /// translation minus the <c>0.4</c> near distance of <c>GlOrtho</c>. Room enough that pushing a
    /// mesh off the far plane can never reach it.
    /// </summary>
    internal const double NearPlaneHeadroom = 19848.6;

    /// <summary>
    /// Slack left between the back of the mesh and the far plane, so rounding in the projection and
    /// a vertex sitting exactly on the corner of the bounding box still have somewhere to go.
    /// </summary>
    internal const double FarPlaneMargin = 24.0;

    /// <summary>
    /// Reads the depth to centre a preview mesh at.
    /// <para>
    /// A mesh reaches at most half its bounding box diagonal away from its centre, whichever way the
    /// camera has turned it, so half the scaled diagonal is how deep it can grow towards the far
    /// plane. Anything that fits at the resting depth is left there, and anything that does not is
    /// lifted by exactly what it takes to clear the plane, which keeps the mesh drawn over the same
    /// gui elements as before for as long as it can be.
    /// </para>
    /// </summary>
    /// <param name="renderZ">Rich text z the component is drawing at.</param>
    /// <param name="zoom">Scale the mesh is drawn at, in screen units per block.</param>
    /// <param name="diagonal">Bounding box diagonal of the structure, in blocks.</param>
    /// <returns>The depth to centre the mesh at.</returns>
    internal static double CenterZ(double renderZ, double zoom, double diagonal)
    {
        double restingZ = renderZ + RenderZOffset;
        double halfDepth = zoom * diagonal / 2.0;

        return Math.Max(restingZ, halfDepth + FarPlaneMargin - FarPlaneHeadroom);
    }
}
