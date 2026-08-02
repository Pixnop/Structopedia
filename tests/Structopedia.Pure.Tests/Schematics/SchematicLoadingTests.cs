using System;
using System.Collections.Generic;
using System.IO;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

/// <summary>
/// Runs the reader against hand written schematic files, so the parser contract is pinned down
/// without shipping any game asset in the repository.
/// </summary>
public sealed class SchematicLoadingTests
{
    [Fact]
    public void LoadFromString_Reads_A_Well_Formed_Schematic()
    {
        string error = string.Empty;

        BlockSchematic? schematic = BlockSchematic.LoadFromString(Fixture("minimal.json"), ref error);

        Assert.NotNull(schematic);
        Assert.Equal(2, schematic.SizeX);
        Assert.Equal(2, schematic.SizeY);
        Assert.Equal(1, schematic.SizeZ);
        Assert.Equal(4, schematic.Indices.Count);
        Assert.Equal(4, schematic.BlockIds.Count);
    }

    [Fact]
    public void ReadCells_Decodes_The_Fixture_Down_To_Its_Cells()
    {
        string error = string.Empty;
        BlockSchematic schematic = BlockSchematic.LoadFromString(Fixture("minimal.json"), ref error)!;

        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        Assert.Equal(4, cells.Count);
        Assert.Equal(new SchematicCell(0, 0, 0, new AssetLocation("game:rock-granite"), false), cells[0]);
        Assert.Equal(new SchematicCell(0, 1, 0, new AssetLocation("game:rock-granite"), false), cells[1]);
        Assert.Equal(new SchematicCell(0, 1, 0, new AssetLocation("game:water-still-7"), true), cells[2]);
        Assert.Equal(new SchematicCell(1, 0, 0, new AssetLocation("game:meta-filler"), false), cells[3]);
    }

    [Fact]
    public void The_Fixture_Tallies_Its_Blocks_And_Filters_Its_Marker()
    {
        string error = string.Empty;
        BlockSchematic schematic = BlockSchematic.LoadFromString(Fixture("minimal.json"), ref error)!;

        TallyResult tally = BlockTally.Count(SchematicCellReader.ReadCells(schematic));

        Assert.Equal(2, tally.Blocks.Count);
        Assert.Equal("game:rock-granite", tally.Blocks[0].Code.ToString());
        Assert.Equal(2, tally.Blocks[0].Count);
        Assert.Equal("game:water-still-7", tally.Blocks[1].Code.ToString());
        Assert.Equal(1, tally.MetaCount);
        Assert.Equal(0, tally.UnknownCount);
    }

    [Fact]
    public void LoadFromString_Reports_A_Corrupt_File_Instead_Of_Throwing()
    {
        string error = string.Empty;

        BlockSchematic? schematic = BlockSchematic.LoadFromString(Fixture("corrupt.json"), ref error);

        Assert.Null(schematic);
        Assert.NotEqual(string.Empty, error);
    }

    private static string Fixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
