using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class BlockClassifierTests
{
    [Theory]
    [InlineData("game:meta-filler")]
    [InlineData("game:meta-underground")]
    [InlineData("game:meta-aboveground")]
    [InlineData("somemod:meta-x")]
    public void Classify_Reports_Meta_Markers(string code)
    {
        Assert.Equal(BlockRole.MetaMarker, BlockClassifier.Classify(new AssetLocation(code)));
    }

    [Theory]
    [InlineData("game:multiblock-monolithic-2x1x0")]
    [InlineData("game:multiblock")]
    [InlineData("somemod:multiblocksomething")]
    public void Classify_Reports_Multiblock_Ghosts(string code)
    {
        Assert.Equal(BlockRole.MultiblockGhost, BlockClassifier.Classify(new AssetLocation(code)));
    }

    [Theory]
    [InlineData("game:metal-plate-copper")]
    [InlineData("game:meta")]
    [InlineData("game:metalpress")]
    [InlineData("game:rock-granite")]
    [InlineData("game:water-still-7")]
    [InlineData("game:crystal-smokyquartz-large1-up")]
    [InlineData("somemod:multi-block-thing")]
    public void Classify_Reports_Everything_Else_As_Visible(string code)
    {
        Assert.Equal(BlockRole.Visible, BlockClassifier.Classify(new AssetLocation(code)));
    }

    [Fact]
    public void Classify_Reports_A_Null_Code_As_Unknown()
    {
        Assert.Equal(BlockRole.UnknownCode, BlockClassifier.Classify(null));
    }

    [Fact]
    public void Classify_Ignores_The_Domain()
    {
        Assert.Equal(
            BlockClassifier.Classify(new AssetLocation("game", "meta-filler")),
            BlockClassifier.Classify(new AssetLocation("othermod", "meta-filler")));
    }

    [Theory]
    [InlineData("META-filler")]
    [InlineData("Meta-filler")]
    [InlineData("MULTIBLOCK-thing")]
    public void Classify_Compares_The_Prefix_Ordinally(string path)
    {
        // The two-argument constructor keeps the path verbatim, so this reaches the ordinal compare.
        Assert.Equal(BlockRole.Visible, BlockClassifier.Classify(new AssetLocation("game", path)));
    }

    [Fact]
    public void Classify_Sees_Codes_Parsed_From_A_Schematic_Already_Lowercased()
    {
        // AssetLocation lowercases when it parses a "domain:path" string, which is the only shape a
        // schematic ever produces. Odd casing in the file therefore still classifies correctly.
        Assert.Equal(BlockRole.MetaMarker, BlockClassifier.Classify(new AssetLocation("game:META-filler")));
        Assert.Equal(BlockRole.MultiblockGhost, BlockClassifier.Classify(new AssetLocation("game:MULTIBLOCK-thing")));
    }
}
