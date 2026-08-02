using Structopedia.Scanning;
using Xunit;

namespace Structopedia.Pure.Tests.Scanning;

public sealed class SchematicAssetPathTests
{
    [Theory]
    [InlineData("worldgen/schematics/trader/cold/tent1.json", "trader/cold/tent1.json")]
    [InlineData("worldgen/schematics/well.json", "well.json")]
    [InlineData("WorldGen/Schematics/Well.json", "Well.json")]
    public void TryGetRelativePath_Accepts_A_Schematic(string assetPath, string expected)
    {
        Assert.True(SchematicAssetPath.TryGetRelativePath(assetPath, out string relativePath));
        Assert.Equal(expected, relativePath);
    }

    [Theory]
    [InlineData("worldgen/structures/ruin.json")]
    [InlineData("worldgen/schematics/readme.txt")]
    [InlineData("worldgen/schematics/")]
    [InlineData("worldgen/schematics/.json")]
    [InlineData("worldgen/schematics/nested/.json")]
    [InlineData("blocktypes/stone.json")]
    [InlineData("")]
    public void TryGetRelativePath_Rejects_Anything_Else(string assetPath)
    {
        Assert.False(SchematicAssetPath.TryGetRelativePath(assetPath, out string relativePath));
        Assert.Equal(string.Empty, relativePath);
    }

    [Fact]
    public void TryGetRelativePath_Normalizes_Separators()
    {
        Assert.True(SchematicAssetPath.TryGetRelativePath(@"worldgen/schematics/vug\amethyst\small1.json", out string relativePath));
        Assert.Equal("vug/amethyst/small1.json", relativePath);
    }
}
