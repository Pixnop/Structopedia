using System;
using Structopedia.Schematics;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class LayerBudgetTests
{
    [Fact]
    public void A_Fresh_Budget_Has_Spent_Nothing()
    {
        var budget = new LayerBudget(100);

        Assert.Equal(0, budget.Used);
        Assert.False(budget.Exhausted);
        Assert.Null(budget.StoppedAtLayer);
    }

    [Fact]
    public void TryAdd_Accumulates_Across_Layers()
    {
        var budget = new LayerBudget(100);

        Assert.True(budget.TryAdd(0, 40));
        Assert.True(budget.TryAdd(1, 40));

        Assert.Equal(80, budget.Used);
        Assert.False(budget.Exhausted);
    }

    [Fact]
    public void TryAdd_Refuses_What_Would_Take_It_Over_The_Ceiling()
    {
        var budget = new LayerBudget(100);
        budget.TryAdd(0, 90);

        Assert.False(budget.TryAdd(3, 20));

        Assert.Equal(90, budget.Used);
        Assert.True(budget.Exhausted);
        Assert.Equal(3, budget.StoppedAtLayer);
    }

    [Fact]
    public void TryAdd_Accepts_Landing_Exactly_On_The_Ceiling()
    {
        var budget = new LayerBudget(100);

        Assert.True(budget.TryAdd(0, 100));

        Assert.Equal(100, budget.Used);
        Assert.False(budget.Exhausted);
    }

    [Fact]
    public void TryAdd_Refuses_Everything_Once_Exhausted()
    {
        var budget = new LayerBudget(100);
        budget.TryAdd(0, 100);
        budget.TryAdd(2, 1);

        Assert.False(budget.TryAdd(2, 1));

        Assert.Equal(2, budget.StoppedAtLayer);
    }

    [Fact]
    public void StoppedAtLayer_Keeps_The_Layer_It_First_Gave_Up_On()
    {
        var budget = new LayerBudget(10);
        budget.TryAdd(0, 10);
        budget.TryAdd(5, 1);
        budget.TryAdd(9, 1);

        Assert.Equal(5, budget.StoppedAtLayer);
    }

    [Fact]
    public void The_First_Block_Always_Goes_In()
    {
        // A single block over the whole ceiling would otherwise leave an empty preview, which reads
        // as a broken structure rather than as a heavy one.
        var budget = new LayerBudget(10);

        Assert.True(budget.TryAdd(0, 4000));

        Assert.Equal(4000, budget.Used);
        Assert.False(budget.Exhausted);
    }

    [Fact]
    public void Adding_Nothing_Is_Always_Allowed()
    {
        var budget = new LayerBudget(10);
        budget.TryAdd(0, 10);

        Assert.True(budget.TryAdd(1, 0));

        Assert.False(budget.Exhausted);
    }

    [Fact]
    public void A_Ceiling_Below_One_Is_Refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayerBudget(0));
    }
}
