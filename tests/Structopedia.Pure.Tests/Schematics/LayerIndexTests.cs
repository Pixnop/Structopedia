using System.Collections.Generic;
using System.Linq;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class LayerIndexTests
{
    [Fact]
    public void Build_On_No_Cells_Has_No_Layers()
    {
        LayerIndex index = LayerIndex.Build([]);

        Assert.Null(index.MinLayer);
        Assert.Null(index.MaxLayer);
        Assert.Equal(0, index.LayerCount);
        Assert.Empty(index.CellsAt(0));
    }

    [Fact]
    public void Build_On_A_Single_Layer_Bounds_It_To_That_Layer()
    {
        LayerIndex index = LayerIndex.Build([Cell(0, 3, 0), Cell(1, 3, 0)]);

        Assert.Equal(3, index.MinLayer);
        Assert.Equal(3, index.MaxLayer);
        Assert.Equal(1, index.LayerCount);
        Assert.Equal(2, index.CellsAt(3).Count);
    }

    [Fact]
    public void CellsAt_Returns_Only_The_Cells_Of_That_Layer()
    {
        LayerIndex index = LayerIndex.Build([Cell(0, 0, 0), Cell(1, 1, 0), Cell(2, 1, 0)]);

        Assert.Equal([0], index.CellsAt(0).Select(cell => cell.X));
        Assert.Equal([1, 2], index.CellsAt(1).Select(cell => cell.X));
    }

    [Fact]
    public void CellsAt_Preserves_The_Input_Order_Inside_A_Layer()
    {
        LayerIndex index = LayerIndex.Build([Cell(5, 2, 0), Cell(1, 2, 0), Cell(3, 2, 0)]);

        Assert.Equal([5, 1, 3], index.CellsAt(2).Select(cell => cell.X));
    }

    [Fact]
    public void Build_Spans_Empty_Layers_Between_Populated_Ones()
    {
        LayerIndex index = LayerIndex.Build([Cell(0, 0, 0), Cell(0, 5, 0)]);

        Assert.Equal(0, index.MinLayer);
        Assert.Equal(5, index.MaxLayer);
        Assert.Equal(6, index.LayerCount);
        Assert.Empty(index.CellsAt(1));
        Assert.Empty(index.CellsAt(3));
        Assert.Empty(index.CellsAt(4));
    }

    [Fact]
    public void Bounds_Ignore_Layers_That_Hold_Nothing()
    {
        LayerIndex index = LayerIndex.Build([Cell(0, 2, 0), Cell(0, 4, 0)]);

        Assert.Equal(2, index.MinLayer);
        Assert.Equal(4, index.MaxLayer);
        Assert.Equal(3, index.LayerCount);
    }

    [Fact]
    public void CellsAt_Returns_Empty_Outside_The_Bounds()
    {
        LayerIndex index = LayerIndex.Build([Cell(0, 2, 0)]);

        Assert.Empty(index.CellsAt(-1));
        Assert.Empty(index.CellsAt(1));
        Assert.Empty(index.CellsAt(3));
        Assert.Empty(index.CellsAt(9999));
    }

    [Fact]
    public void Build_Keeps_Both_Halves_Of_A_Waterlogged_Cell_In_Their_Layer()
    {
        LayerIndex index = LayerIndex.Build(
        [
            new SchematicCell(1, 4, 1, new AssetLocation("game:rock-granite"), false),
            new SchematicCell(1, 4, 1, new AssetLocation("game:water-still-7"), true)
        ]);

        IReadOnlyList<SchematicCell> layer = index.CellsAt(4);
        Assert.Equal(2, layer.Count);
        Assert.False(layer[0].IsFluidLayer);
        Assert.True(layer[1].IsFluidLayer);
    }

    private static SchematicCell Cell(int x, int y, int z)
        => new(x, y, z, new AssetLocation("game:rock-granite"), false);
}
