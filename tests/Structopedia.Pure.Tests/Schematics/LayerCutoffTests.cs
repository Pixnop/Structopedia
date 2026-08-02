using Structopedia.Schematics;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class LayerCutoffTests
{
    private const double TrackX = 100.0;
    private const double TrackWidth = 200.0;

    [Fact]
    public void FromPixel_Maps_The_Left_End_To_The_Lowest_Layer()
    {
        Assert.Equal(4, LayerCutoff.FromPixel(TrackX, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void FromPixel_Maps_The_Right_End_To_The_Highest_Layer()
    {
        Assert.Equal(14, LayerCutoff.FromPixel(TrackX + TrackWidth, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void FromPixel_Maps_The_Middle_To_The_Middle_Layer()
    {
        Assert.Equal(9, LayerCutoff.FromPixel(TrackX + (TrackWidth / 2), TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void FromPixel_Clamps_Outside_The_Track()
    {
        Assert.Equal(4, LayerCutoff.FromPixel(TrackX - 500.0, TrackX, TrackWidth, 4, 14));
        Assert.Equal(14, LayerCutoff.FromPixel(TrackX + TrackWidth + 500.0, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void FromPixel_Rounds_To_The_Nearest_Layer()
    {
        // Eleven layers over 200 pixels: a layer is 20 pixels, so the boundary sits at 10.
        Assert.Equal(4, LayerCutoff.FromPixel(TrackX + 9.0, TrackX, TrackWidth, 4, 14));
        Assert.Equal(5, LayerCutoff.FromPixel(TrackX + 11.0, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void FromPixel_Steps_Through_Layers_That_Hold_Nothing()
    {
        // The range is continuous on purpose: a cut at layer 7 has to mean layer 7, whether or not
        // the schematic put a block there.
        Assert.Equal(7, LayerCutoff.FromPixel(TrackX + 70.0, TrackX, TrackWidth, 0, 20));
    }

    [Fact]
    public void FromPixel_On_A_Single_Layer_Structure_Returns_That_Layer()
    {
        Assert.Equal(3, LayerCutoff.FromPixel(TrackX, TrackX, TrackWidth, 3, 3));
        Assert.Equal(3, LayerCutoff.FromPixel(TrackX + TrackWidth, TrackX, TrackWidth, 3, 3));
    }

    [Fact]
    public void FromPixel_On_A_Track_Of_No_Width_Returns_The_Highest_Layer()
    {
        Assert.Equal(14, LayerCutoff.FromPixel(TrackX, TrackX, 0.0, 4, 14));
    }

    [Fact]
    public void ToPixel_Puts_The_Lowest_Layer_At_The_Left_End()
    {
        Assert.Equal(TrackX, LayerCutoff.ToPixel(4, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void ToPixel_Puts_The_Highest_Layer_At_The_Right_End()
    {
        Assert.Equal(TrackX + TrackWidth, LayerCutoff.ToPixel(14, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void ToPixel_Clamps_A_Layer_Outside_The_Range()
    {
        Assert.Equal(TrackX, LayerCutoff.ToPixel(-40, TrackX, TrackWidth, 4, 14));
        Assert.Equal(TrackX + TrackWidth, LayerCutoff.ToPixel(40, TrackX, TrackWidth, 4, 14));
    }

    [Fact]
    public void ToPixel_On_A_Single_Layer_Structure_Sits_At_The_Right_End()
    {
        // One layer means the view is always whole, and a handle parked at the end says so.
        Assert.Equal(TrackX + TrackWidth, LayerCutoff.ToPixel(3, TrackX, TrackWidth, 3, 3));
    }

    [Fact]
    public void ToPixel_Undoes_FromPixel()
    {
        for (int layer = 4; layer <= 14; layer++)
        {
            double x = LayerCutoff.ToPixel(layer, TrackX, TrackWidth, 4, 14);
            Assert.Equal(layer, LayerCutoff.FromPixel(x, TrackX, TrackWidth, 4, 14));
        }
    }

    [Fact]
    public void Step_Moves_By_One_Layer()
    {
        Assert.Equal(8, LayerCutoff.Step(7, 1, 4, 14));
        Assert.Equal(6, LayerCutoff.Step(7, -1, 4, 14));
    }

    [Fact]
    public void Step_Stops_At_The_Ends_Of_The_Range()
    {
        Assert.Equal(14, LayerCutoff.Step(14, 1, 4, 14));
        Assert.Equal(4, LayerCutoff.Step(4, -1, 4, 14));
    }

    [Fact]
    public void Step_Pulls_A_Cutoff_Left_Over_From_Another_Structure_Back_Into_Range()
    {
        Assert.Equal(14, LayerCutoff.Step(80, 1, 4, 14));
        Assert.Equal(4, LayerCutoff.Step(-80, -1, 4, 14));
    }

    [Fact]
    public void Clamp_Bounds_A_Layer_To_The_Range()
    {
        Assert.Equal(4, LayerCutoff.Clamp(0, 4, 14));
        Assert.Equal(9, LayerCutoff.Clamp(9, 4, 14));
        Assert.Equal(14, LayerCutoff.Clamp(200, 4, 14));
    }
}
