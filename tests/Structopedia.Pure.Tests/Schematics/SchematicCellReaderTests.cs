using System.Collections.Generic;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class SchematicCellReaderTests
{
    private const int Stone = 1;
    private const int Water = 2;

    [Theory]
    [InlineData(0u, 0, 0, 0)]
    [InlineData(1u, 1, 0, 0)]
    [InlineData(1023u, 1023, 0, 0)]
    [InlineData(1023u << 10, 0, 0, 1023)]
    [InlineData(1023u << 20, 0, 1023, 0)]
    [InlineData((1023u << 20) | (1023u << 10) | 1023u, 1023, 1023, 1023)]
    [InlineData((7u << 20) | (3u << 10) | 5u, 5, 7, 3)]
    [InlineData(2100224u, 0, 2, 3)]
    public void ReadCells_Unpacks_Index_Into_Coordinates(uint index, int x, int y, int z)
    {
        BlockSchematic schematic = Build([index], [Stone]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        SchematicCell cell = Assert.Single(cells);
        Assert.Equal(x, cell.X);
        Assert.Equal(y, cell.Y);
        Assert.Equal(z, cell.Z);
    }

    [Fact]
    public void ReadCells_Pairs_Indices_With_BlockIds_By_List_Position()
    {
        BlockSchematic schematic = Build([0u, 1u, 2u], [Stone, Water, Stone]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.Equal(3, cells.Count);
        Assert.Equal(new AssetLocation("game", "rock-granite"), cells[0].Code);
        Assert.Equal(new AssetLocation("game", "water-still-7"), cells[1].Code);
        Assert.Equal(new AssetLocation("game", "rock-granite"), cells[2].Code);
    }

    [Fact]
    public void ReadCells_Preserves_List_Order()
    {
        BlockSchematic schematic = Build([5u, 1u, 3u], [Stone, Stone, Stone]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.Equal([5, 1, 3], [cells[0].X, cells[1].X, cells[2].X]);
    }

    [Fact]
    public void ReadCells_Marks_Second_Of_A_Consecutive_Duplicate_As_Fluid_Layer()
    {
        BlockSchematic schematic = Build([42u, 42u], [Stone, Water]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.False(cells[0].IsFluidLayer);
        Assert.True(cells[1].IsFluidLayer);
        Assert.Equal(cells[0].X, cells[1].X);
        Assert.Equal(cells[0].Y, cells[1].Y);
        Assert.Equal(cells[0].Z, cells[1].Z);
    }

    [Fact]
    public void ReadCells_Does_Not_Mark_A_Non_Consecutive_Duplicate_As_Fluid_Layer()
    {
        BlockSchematic schematic = Build([42u, 7u, 42u], [Stone, Stone, Water]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.All(cells, cell => Assert.False(cell.IsFluidLayer));
    }

    [Fact]
    public void ReadCells_Never_Marks_The_First_Cell_As_Fluid_Layer()
    {
        BlockSchematic schematic = Build([42u], [Water]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.False(Assert.Single(cells).IsFluidLayer);
    }

    [Fact]
    public void ReadCells_Marks_Only_The_Second_Of_Three_Identical_Indices()
    {
        BlockSchematic schematic = Build([42u, 42u, 42u], [Stone, Water, Water]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.False(cells[0].IsFluidLayer);
        Assert.True(cells[1].IsFluidLayer);
        Assert.True(cells[2].IsFluidLayer);
    }

    [Fact]
    public void ReadCells_Yields_A_Null_Code_When_The_Block_Id_Is_Not_In_BlockCodes()
    {
        BlockSchematic schematic = Build([0u, 1u], [Stone, 999]);

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.NotNull(cells[0].Code);
        Assert.Null(cells[1].Code);
    }

    [Fact]
    public void ReadCells_Still_Decodes_Coordinates_For_An_Unknown_Block_Id()
    {
        BlockSchematic schematic = Build([(7u << 20) | (3u << 10) | 5u], [999]);

        SchematicCell cell = Assert.Single(SchematicCellReader.ReadCells(schematic));

        Assert.Equal(new SchematicCell(5, 7, 3, null, false), cell);
    }

    [Fact]
    public void ReadCells_Stops_At_The_Shortest_List_When_Indices_Is_Longer()
    {
        BlockSchematic schematic = Build([0u, 1u, 2u], [Stone, Stone]);

        Assert.Equal(2, SchematicCellReader.ReadCells(schematic).Count);
    }

    [Fact]
    public void ReadCells_Stops_At_The_Shortest_List_When_BlockIds_Is_Longer()
    {
        BlockSchematic schematic = Build([0u], [Stone, Stone, Stone]);

        Assert.Single(SchematicCellReader.ReadCells(schematic));
    }

    [Fact]
    public void ReadCells_Returns_Empty_For_An_Empty_Schematic()
    {
        Assert.Empty(SchematicCellReader.ReadCells(new BlockSchematic()));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 3)]
    [InlineData(1023, 1023, 1023)]
    [InlineData(7, 0, 511)]
    public void PackIndex_Is_Undone_By_ReadCells(int x, int y, int z)
    {
        // Block entity data is keyed by the same packed index as the block lists, so a cell has to be
        // able to find its own.
        BlockSchematic schematic = Build([SchematicCellReader.PackIndex(x, y, z)], [Stone]);

        SchematicCell cell = Assert.Single(SchematicCellReader.ReadCells(schematic));

        Assert.Equal((x, y, z), (cell.X, cell.Y, cell.Z));
    }

    private static BlockSchematic Build(uint[] indices, int[] blockIds)
    {
        var schematic = new BlockSchematic
        {
            SizeX = 1024,
            SizeY = 1024,
            SizeZ = 1024
        };

        schematic.BlockCodes[Stone] = new AssetLocation("game", "rock-granite");
        schematic.BlockCodes[Water] = new AssetLocation("game", "water-still-7");
        schematic.Indices.AddRange(indices);
        schematic.BlockIds.AddRange(blockIds);

        return schematic;
    }
}
