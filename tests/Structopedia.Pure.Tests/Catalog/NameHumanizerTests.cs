using Structopedia.Catalog;
using Xunit;

namespace Structopedia.Pure.Tests.Catalog;

public sealed class NameHumanizerTests
{
    [Theory]
    [InlineData("vug-medium1", "Vug medium 1")]
    [InlineData("housesmall", "Housesmall")]
    [InlineData("raentrance-mid-h10-1", "Raentrance mid h 10 1")]
    [InlineData("gear-11", "Gear 11")]
    [InlineData("devastationarea-past", "Devastationarea past")]
    [InlineData("arcticsupplies14u-1", "Arcticsupplies 14 u 1")]
    [InlineData("trader", "Trader")]
    [InlineData("cold", "Cold")]
    [InlineData("u1", "U 1")]
    [InlineData("candle-maker", "Candle maker")]
    [InlineData("reapers-lost-treasure", "Reapers lost treasure")]
    [InlineData("multiblock_ghost", "Multiblock ghost")]
    [InlineData("religiouslarge", "Religiouslarge")]
    public void Humanize_Rewrites_An_Asset_Name(string raw, string expected)
    {
        Assert.Equal(expected, NameHumanizer.Humanize(raw));
    }

    [Theory]
    [InlineData("vug-medium1.json", "Vug medium 1")]
    [InlineData("gear-11.json", "Gear 11")]
    public void Humanize_Drops_A_Json_Extension(string raw, string expected)
    {
        Assert.Equal(expected, NameHumanizer.Humanize(raw));
    }

    [Fact]
    public void Humanize_Keeps_An_Extension_That_Is_Not_Json()
    {
        Assert.Equal("Notes txt", NameHumanizer.Humanize("notes.txt"));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("-", "")]
    [InlineData("__", "")]
    [InlineData("   ", "")]
    public void Humanize_Returns_Empty_For_A_Name_With_No_Words(string raw, string expected)
    {
        Assert.Equal(expected, NameHumanizer.Humanize(raw));
    }

    [Fact]
    public void Humanize_Collapses_Repeated_Separators()
    {
        Assert.Equal("Vug medium", NameHumanizer.Humanize("vug--_-medium"));
    }

    [Fact]
    public void Humanize_Leaves_An_Already_Capitalized_Name_Alone()
    {
        Assert.Equal("Story", NameHumanizer.Humanize("Story"));
    }

    [Fact]
    public void Humanize_Handles_A_Name_Starting_With_A_Digit()
    {
        Assert.Equal("2 x 1 x 0", NameHumanizer.Humanize("2x1x0"));
    }

    [Fact]
    public void Humanize_Is_Deterministic()
    {
        Assert.Equal(NameHumanizer.Humanize("raentrance-mid-h10-1"), NameHumanizer.Humanize("raentrance-mid-h10-1"));
    }
}
