using System.Collections.Generic;
using System.Linq;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class BlockTallyTests
{
    [Fact]
    public void Count_Returns_An_Empty_Tally_For_No_Cells()
    {
        TallyResult result = BlockTally.Count([]);

        Assert.Empty(result.Blocks);
        Assert.Equal(0, result.MetaCount);
        Assert.Equal(0, result.MultiblockCount);
        Assert.Equal(0, result.UnknownCount);
    }

    [Fact]
    public void Count_Aggregates_Cells_Sharing_A_Code()
    {
        TallyResult result = BlockTally.Count([Cell("game:rock-granite"), Cell("game:rock-granite")]);

        (AssetLocation Code, int Count) row = Assert.Single(result.Blocks);
        Assert.Equal(new AssetLocation("game:rock-granite"), row.Code);
        Assert.Equal(2, row.Count);
    }

    [Fact]
    public void Count_Keeps_Distinct_Variants_On_Distinct_Rows()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:crystal-smokyquartz-small1-up"),
            Cell("game:crystal-smokyquartz-small2-up"),
            Cell("game:crystal-smokyquartz-small1-up")
        ]);

        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal("game:crystal-smokyquartz-small1-up", result.Blocks[0].Code.ToString());
        Assert.Equal(2, result.Blocks[0].Count);
        Assert.Equal("game:crystal-smokyquartz-small2-up", result.Blocks[1].Code.ToString());
        Assert.Equal(1, result.Blocks[1].Count);
    }

    [Fact]
    public void Count_Orders_By_Descending_Count()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:one"),
            Cell("game:three"), Cell("game:three"), Cell("game:three"),
            Cell("game:two"), Cell("game:two")
        ]);

        Assert.Equal(["game:three", "game:two", "game:one"], Codes(result));
    }

    [Fact]
    public void Count_Breaks_Ties_By_Ascending_Code()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:charlie"), Cell("game:charlie"),
            Cell("game:alpha"), Cell("game:alpha"),
            Cell("game:bravo"), Cell("game:bravo")
        ]);

        Assert.Equal(["game:alpha", "game:bravo", "game:charlie"], Codes(result));
    }

    [Fact]
    public void Count_Sorts_Ties_Across_Domains_Too()
    {
        TallyResult result = BlockTally.Count([Cell("zmod:brick"), Cell("amod:brick")]);

        Assert.Equal(["amod:brick", "zmod:brick"], Codes(result));
    }

    [Fact]
    public void Count_Is_Stable_Whatever_The_Input_Order()
    {
        SchematicCell[] cells =
        [
            Cell("game:bravo"), Cell("game:alpha"), Cell("game:bravo"), Cell("game:alpha")
        ];

        Assert.Equal(Codes(BlockTally.Count(cells)), Codes(BlockTally.Count(cells.Reverse().ToArray())));
    }

    [Fact]
    public void Count_Treats_A_Fluid_Layer_Cell_As_A_Real_Block()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:rock-granite"),
            Cell("game:water-still-7", isFluidLayer: true)
        ]);

        Assert.Equal(["game:rock-granite", "game:water-still-7"], Codes(result));
        Assert.All(result.Blocks, row => Assert.Equal(1, row.Count));
    }

    [Fact]
    public void Count_Excludes_Meta_Markers_And_Counts_Them_Apart()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:meta-filler"),
            Cell("game:meta-underground"),
            Cell("game:rock-granite")
        ]);

        Assert.Equal(["game:rock-granite"], Codes(result));
        Assert.Equal(2, result.MetaCount);
    }

    [Fact]
    public void Count_Excludes_Multiblock_Ghosts_And_Counts_Them_Apart()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:multiblock-monolithic-2x1x0"),
            Cell("game:rock-granite")
        ]);

        Assert.Equal(["game:rock-granite"], Codes(result));
        Assert.Equal(1, result.MultiblockCount);
    }

    [Fact]
    public void Count_Excludes_Worldgen_Randomizers_And_Counts_Them_Apart()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:randomizer-normal"),
            Cell("game:randomizer-normal"),
            Cell("game:rock-granite")
        ]);

        Assert.Equal(["game:rock-granite"], Codes(result));
        Assert.Equal(2, result.RandomizerCount);
    }

    [Fact]
    public void Count_Counts_Unresolved_Codes_Apart()
    {
        TallyResult result = BlockTally.Count([new SchematicCell(0, 0, 0, null, false), Cell("game:rock-granite")]);

        Assert.Equal(["game:rock-granite"], Codes(result));
        Assert.Equal(1, result.UnknownCount);
    }

    [Fact]
    public void Count_Handles_A_Mixed_Schematic()
    {
        TallyResult result = BlockTally.Count(
        [
            Cell("game:meta-filler"),
            Cell("game:rock-granite"),
            Cell("game:water-still-7", isFluidLayer: true),
            Cell("game:rock-granite"),
            Cell("game:multiblock-monolithic-2x1x0"),
            new SchematicCell(4, 0, 0, null, false),
            Cell("game:meta-underground"),
            Cell("game:randomizer-normal"),
            Cell("game:rock-granite")
        ]);

        Assert.Equal(["game:rock-granite", "game:water-still-7"], Codes(result));
        Assert.Equal(3, result.Blocks[0].Count);
        Assert.Equal(1, result.Blocks[1].Count);
        Assert.Equal(2, result.MetaCount);
        Assert.Equal(1, result.MultiblockCount);
        Assert.Equal(1, result.RandomizerCount);
        Assert.Equal(1, result.UnknownCount);
    }

    private static SchematicCell Cell(string code, bool isFluidLayer = false)
        => new(0, 0, 0, new AssetLocation(code), isFluidLayer);

    private static IReadOnlyList<string> Codes(TallyResult result)
        => result.Blocks.Select(row => row.Code.ToString()).ToList();
}
