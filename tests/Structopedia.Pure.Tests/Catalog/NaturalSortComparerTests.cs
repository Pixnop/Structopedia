using System.Collections.Generic;
using System.Linq;
using Structopedia.Catalog;
using Xunit;

namespace Structopedia.Pure.Tests.Catalog;

public sealed class NaturalSortComparerTests
{
    [Theory]
    [InlineData("ruin-2", "ruin-10")]
    [InlineData("gear-1", "gear-2")]
    [InlineData("gear-9", "gear-10")]
    [InlineData("gear-10", "gear-11")]
    [InlineData("arcticsupplies2", "arcticsupplies11")]
    [InlineData("small-9", "small-20")]
    [InlineData("a", "b")]
    [InlineData("1", "2")]
    [InlineData("2", "10")]
    [InlineData("item", "item1")]
    [InlineData("tent1", "tent2")]
    [InlineData("alpha-2-b", "alpha-2-c")]
    [InlineData("alpha-2-z", "alpha-10-a")]
    public void Compare_Orders_The_Left_Value_First(string left, string right)
    {
        Assert.True(NaturalSortComparer.Instance.Compare(left, right) < 0);
        Assert.True(NaturalSortComparer.Instance.Compare(right, left) > 0);
    }

    [Fact]
    public void Compare_Reports_Identical_Strings_As_Equal()
    {
        Assert.Equal(0, NaturalSortComparer.Instance.Compare("gear-10", "gear-10"));
    }

    [Fact]
    public void Compare_Falls_Back_To_An_Ordinal_Tiebreak_On_Leading_Zeros()
    {
        // Numerically equal, so the comparer must still pick a stable side rather than call it a tie.
        Assert.True(NaturalSortComparer.Instance.Compare("gear-01", "gear-1") < 0);
        Assert.True(NaturalSortComparer.Instance.Compare("gear-1", "gear-01") > 0);
    }

    [Fact]
    public void Compare_Handles_Very_Long_Digit_Runs()
    {
        Assert.True(NaturalSortComparer.Instance.Compare("x-99999999999999999999", "x-100000000000000000000") < 0);
    }

    [Fact]
    public void Compare_Puts_Null_First()
    {
        Assert.True(NaturalSortComparer.Instance.Compare(null, "a") < 0);
        Assert.True(NaturalSortComparer.Instance.Compare("a", null) > 0);
        Assert.Equal(0, NaturalSortComparer.Instance.Compare(null, null));
    }

    [Fact]
    public void Sorting_A_Real_Variant_List_Puts_The_Numbers_In_Order()
    {
        List<string> names = ["ruin-10", "ruin-1", "ruin-20", "ruin-2", "ruin-3"];

        names.Sort(NaturalSortComparer.Instance);

        Assert.Equal(["ruin-1", "ruin-2", "ruin-3", "ruin-10", "ruin-20"], names);
    }

    [Fact]
    public void Sorting_Is_Independent_Of_The_Starting_Order()
    {
        string[] names = ["small-2", "small-13", "small-1", "small-20", "small-9"];

        Assert.Equal(
            names.OrderBy(name => name, NaturalSortComparer.Instance),
            names.Reverse().OrderBy(name => name, NaturalSortComparer.Instance));
    }
}
