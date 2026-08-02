using Structopedia.Preview;
using Xunit;

namespace Structopedia.Pure.Tests.Preview;

public sealed class PreviewDepthTests
{
    /// <summary>Rich text z of a page, the value the component is handed every frame.</summary>
    private const double RenderZ = 50.0;

    /// <summary>Where a mesh sits when nothing pushes it forward.</summary>
    private const double RestingZ = RenderZ + PreviewDepth.RenderZOffset;

    [Fact]
    public void CenterZ_Leaves_A_Small_Mesh_At_Its_Resting_Depth()
    {
        // Half depth of 50 units: the whole mesh fits between the far plane and the resting depth.
        Assert.Equal(RestingZ, PreviewDepth.CenterZ(RenderZ, 1.0, 100.0));
    }

    [Fact]
    public void CenterZ_Follows_The_Rich_Text_Z_While_The_Mesh_Is_Small()
    {
        Assert.Equal(PreviewDepth.RenderZOffset, PreviewDepth.CenterZ(0.0, 1.0, 100.0));
        Assert.Equal(120.0 + PreviewDepth.RenderZOffset, PreviewDepth.CenterZ(120.0, 1.0, 100.0));
    }

    [Fact]
    public void CenterZ_Ignores_A_Structure_Of_No_Size_And_A_Zoom_Of_Zero()
    {
        Assert.Equal(RestingZ, PreviewDepth.CenterZ(RenderZ, 4.0, 0.0));
        Assert.Equal(RestingZ, PreviewDepth.CenterZ(RenderZ, 0.0, 400.0));
    }

    [Fact]
    public void CenterZ_Pushes_A_Large_Zoomed_Mesh_Forward()
    {
        // Half depth of 480 units, which is what the zoom ceiling reaches on a full sized viewport.
        double z = PreviewDepth.CenterZ(RenderZ, 4.0, 240.0);

        Assert.True(z > RestingZ);
        Assert.Equal(480.0 + PreviewDepth.FarPlaneMargin - PreviewDepth.FarPlaneHeadroom, z);
    }

    [Fact]
    public void CenterZ_Parks_The_Back_Of_A_Pushed_Mesh_On_The_Margin()
    {
        const double halfDepth = 480.0;
        double z = PreviewDepth.CenterZ(RenderZ, 4.0, 240.0);
        double backOfMesh = z - halfDepth;

        // Exactly at the threshold: the far plane sits at -FarPlaneHeadroom and the back of the mesh
        // stops the margin short of it, whichever way the camera has turned.
        Assert.Equal(PreviewDepth.FarPlaneMargin - PreviewDepth.FarPlaneHeadroom, backOfMesh);
        Assert.True(backOfMesh > -PreviewDepth.FarPlaneHeadroom);
    }

    [Fact]
    public void CenterZ_Switches_Over_Where_The_Resting_Depth_Stops_Covering_The_Mesh()
    {
        // The resting depth covers a half depth of up to RestingZ + FarPlaneHeadroom - FarPlaneMargin.
        const double threshold = RestingZ + PreviewDepth.FarPlaneHeadroom - PreviewDepth.FarPlaneMargin;

        Assert.Equal(RestingZ, PreviewDepth.CenterZ(RenderZ, 2.0, threshold - 1.0));
        Assert.Equal(RestingZ, PreviewDepth.CenterZ(RenderZ, 2.0, threshold));
        Assert.Equal(RestingZ + 1.0, PreviewDepth.CenterZ(RenderZ, 2.0, threshold + 1.0));
    }

    [Fact]
    public void CenterZ_Never_Moves_Back_As_The_Zoom_Grows()
    {
        double previous = double.MinValue;

        for (double zoom = 0.1; zoom <= 8.0; zoom += 0.1)
        {
            double z = PreviewDepth.CenterZ(RenderZ, zoom, 240.0);
            Assert.True(z >= previous);
            previous = z;
        }
    }

    [Fact]
    public void CenterZ_Never_Moves_Back_As_The_Structure_Grows()
    {
        double previous = double.MinValue;

        for (double diagonal = 1.0; diagonal <= 600.0; diagonal += 1.0)
        {
            double z = PreviewDepth.CenterZ(RenderZ, 2.0, diagonal);
            Assert.True(z >= previous);
            previous = z;
        }
    }

    [Fact]
    public void CenterZ_Keeps_Every_Mesh_A_Handbook_Can_Show_Inside_The_Depth_Range()
    {
        // Sweep the whole space the component can hand over: a page z of a few hundred, the zoom
        // range, and structures from a single block to the largest the game ships.
        for (double renderZ = 0.0; renderZ <= 400.0; renderZ += 25.0)
        {
            for (double zoom = 0.1; zoom <= 8.0; zoom += 0.1)
            {
                for (double diagonal = 1.0; diagonal <= 800.0; diagonal += 25.0)
                {
                    double halfDepth = zoom * diagonal / 2.0;
                    double z = PreviewDepth.CenterZ(renderZ, zoom, diagonal);

                    Assert.True(z - halfDepth >= -PreviewDepth.FarPlaneHeadroom);
                    Assert.True(z + halfDepth <= PreviewDepth.NearPlaneHeadroom);
                }
            }
        }
    }
}
