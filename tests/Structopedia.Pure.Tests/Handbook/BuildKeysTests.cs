using System;
using Structopedia.Handbook;
using Xunit;

namespace Structopedia.Pure.Tests.Handbook;

public sealed class BuildKeysTests
{
    [Fact]
    public void Title_Names_The_Key_Of_The_Last_Folder()
    {
        Assert.Equal(
            "structopedia:build-title-cementation-furnace",
            BuildKeys.Title("builds/cementation-furnace"));
    }

    [Fact]
    public void Description_Names_The_Key_Of_The_Last_Folder()
    {
        Assert.Equal(
            "structopedia:build-desc-charcoal-pit",
            BuildKeys.Description("builds/charcoal-pit"));
    }

    [Fact]
    public void Keys_Read_Windows_Separators()
    {
        Assert.Equal("structopedia:build-title-bloomery", BuildKeys.Title(@"builds\bloomery"));
    }

    [Theory]
    [InlineData("builds/bloomery/")]
    [InlineData("builds/bloomery//")]
    public void Keys_Ignore_Trailing_Separators(string groupKey)
    {
        Assert.Equal("structopedia:build-title-bloomery", BuildKeys.Title(groupKey));
    }

    [Fact]
    public void Keys_Take_A_Single_Segment_As_It_Is()
    {
        Assert.Equal("structopedia:build-title-builds", BuildKeys.Title("builds"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("//")]
    public void Keys_Are_Empty_When_There_Is_No_Segment_To_Name(string groupKey)
    {
        Assert.Equal(string.Empty, BuildKeys.Title(groupKey));
        Assert.Equal(string.Empty, BuildKeys.Description(groupKey));
    }

    [Fact]
    public void Keys_Reject_Null()
    {
        Assert.Throws<ArgumentNullException>(() => BuildKeys.Title(null!));
        Assert.Throws<ArgumentNullException>(() => BuildKeys.Description(null!));
    }
}
