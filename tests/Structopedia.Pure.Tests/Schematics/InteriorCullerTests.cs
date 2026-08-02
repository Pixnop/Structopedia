using System.Collections.Generic;
using System.Linq;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class InteriorCullerTests
{
    // Fully qualified: Vintagestory.API.Common declares a Func delegate of its own.
    private static readonly System.Func<AssetLocation, bool> AllOpaque = _ => true;

    [Fact]
    public void FindHiddenCells_Returns_Nothing_For_No_Cells()
    {
        Assert.Empty(InteriorCuller.FindHiddenCells([], AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Hides_Only_The_Core_Of_A_Solid_Cube()
    {
        HashSet<(int X, int Y, int Z)> hidden = InteriorCuller.FindHiddenCells(SolidCube(), AllOpaque);

        Assert.Equal((1, 1, 1), Assert.Single(hidden));
    }

    [Fact]
    public void FindHiddenCells_Never_Hides_A_Cell_On_The_Border()
    {
        HashSet<(int X, int Y, int Z)> hidden = InteriorCuller.FindHiddenCells(SolidCube(), AllOpaque);

        Assert.DoesNotContain((0, 0, 0), hidden);
        Assert.DoesNotContain((1, 1, 0), hidden);
        Assert.DoesNotContain((2, 2, 2), hidden);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 2)]
    public void FindHiddenCells_Stops_Hiding_When_Any_Neighbour_Is_Missing(int x, int y, int z)
    {
        List<SchematicCell> cells = SolidCube()
            .Where(cell => cell.X != x || cell.Y != y || cell.Z != z)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Stops_Hiding_When_A_Neighbour_Is_Not_An_Opaque_Full_Cube()
    {
        List<SchematicCell> cells = SolidCube()
            .Select(cell => cell is { X: 0, Y: 1, Z: 1 } ? cell with { Code = new AssetLocation("game:glass") } : cell)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, code => code.Path != "glass"));
    }

    [Fact]
    public void FindHiddenCells_Does_Not_Let_A_Fluid_Cell_Occlude()
    {
        List<SchematicCell> cells = SolidCube()
            .Select(cell => cell is { X: 0, Y: 1, Z: 1 } ? cell with { IsFluidLayer = true } : cell)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Never_Hides_A_Fluid_Cell()
    {
        List<SchematicCell> cells = SolidCube();
        cells.Add(new SchematicCell(1, 1, 1, new AssetLocation("game:water-still-7"), true));

        HashSet<(int X, int Y, int Z)> hidden = InteriorCuller.FindHiddenCells(cells, AllOpaque);

        // The solid half of the waterlogged cell is still hidden, but it is reported once only.
        Assert.Equal((1, 1, 1), Assert.Single(hidden));
    }

    [Fact]
    public void FindHiddenCells_Still_Occludes_Through_The_Solid_Half_Of_A_Waterlogged_Neighbour()
    {
        List<SchematicCell> cells = SolidCube();
        cells.Add(new SchematicCell(0, 1, 1, new AssetLocation("game:water-still-7"), true));

        Assert.Contains((1, 1, 1), InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Does_Not_Let_A_Meta_Marker_Occlude()
    {
        List<SchematicCell> cells = SolidCube()
            .Select(cell => cell is { X: 0, Y: 1, Z: 1 } ? cell with { Code = new AssetLocation("game:meta-filler") } : cell)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Never_Hides_A_Meta_Marker()
    {
        List<SchematicCell> cells = SolidCube()
            .Select(cell => cell is { X: 1, Y: 1, Z: 1 } ? cell with { Code = new AssetLocation("game:meta-filler") } : cell)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Never_Hides_A_Cell_Whose_Code_Is_Unknown()
    {
        List<SchematicCell> cells = SolidCube()
            .Select(cell => cell is { X: 1, Y: 1, Z: 1 } ? cell with { Code = null } : cell)
            .ToList();

        Assert.Empty(InteriorCuller.FindHiddenCells(cells, AllOpaque));
    }

    [Fact]
    public void FindHiddenCells_Hides_The_Inner_Column_Of_A_Taller_Block()
    {
        var cells = new List<SchematicCell>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    cells.Add(Cell(x, y, z));
                }
            }
        }

        HashSet<(int X, int Y, int Z)> hidden = InteriorCuller.FindHiddenCells(cells, AllOpaque);

        Assert.Equal(2, hidden.Count);
        Assert.Contains((1, 1, 1), hidden);
        Assert.Contains((1, 2, 1), hidden);
    }

    private static List<SchematicCell> SolidCube()
    {
        var cells = new List<SchematicCell>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    cells.Add(Cell(x, y, z));
                }
            }
        }

        return cells;
    }

    private static SchematicCell Cell(int x, int y, int z)
        => new(x, y, z, new AssetLocation("game:rock-granite"), false);
}
