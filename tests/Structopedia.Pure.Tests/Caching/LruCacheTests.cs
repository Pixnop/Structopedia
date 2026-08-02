using System;
using System.Collections.Generic;
using Structopedia.Caching;
using Xunit;

namespace Structopedia.Pure.Tests.Caching;

public sealed class LruCacheTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Rejects_A_Capacity_Below_One(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, string>(capacity, _ => { }));
    }

    [Fact]
    public void Constructor_Rejects_A_Missing_Eviction_Callback()
    {
        Assert.Throws<ArgumentNullException>(() => new LruCache<string, string>(2, null!));
    }

    [Fact]
    public void TryGet_Reports_A_Miss_On_An_Empty_Cache()
    {
        var cache = new LruCache<string, string>(2, _ => { });

        Assert.False(cache.TryGet("a", out string? value));
        Assert.Null(value);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void TryGet_Returns_What_Set_Stored()
    {
        var cache = new LruCache<string, string>(2, _ => { });

        cache.Set("a", "alpha");

        Assert.True(cache.TryGet("a", out string? value));
        Assert.Equal("alpha", value);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Set_Evicts_The_Least_Recently_Used_Entry_Past_Capacity()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        cache.Set("c", "charlie");

        Assert.Equal(["alpha"], evicted);
        Assert.Equal(2, cache.Count);
        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [Fact]
    public void TryGet_Refreshes_Recency()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        Assert.True(cache.TryGet("a", out _));
        cache.Set("c", "charlie");

        Assert.Equal(["bravo"], evicted);
        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
    }

    [Fact]
    public void A_Missed_TryGet_Does_Not_Change_Recency()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        Assert.False(cache.TryGet("missing", out _));
        cache.Set("c", "charlie");

        Assert.Equal(["alpha"], evicted);
    }

    [Fact]
    public void Set_On_An_Existing_Key_Evicts_The_Value_It_Replaces()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("a", "alpha2");

        Assert.Equal(["alpha"], evicted);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet("a", out string? value));
        Assert.Equal("alpha2", value);
    }

    [Fact]
    public void Set_On_An_Existing_Key_Refreshes_Recency()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        cache.Set("a", "alpha2");
        cache.Set("c", "charlie");

        Assert.Equal(["alpha", "bravo"], evicted);
        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
    }

    [Fact]
    public void Clear_Evicts_Everything_Least_Recent_First()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(3, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        cache.Set("c", "charlie");
        cache.Clear();

        Assert.Equal(["alpha", "bravo", "charlie"], evicted);
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void Clear_On_An_Empty_Cache_Evicts_Nothing()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(2, evicted.Add);

        cache.Clear();

        Assert.Empty(evicted);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Clear_Leaves_The_Cache_Usable()
    {
        var cache = new LruCache<string, string>(2, _ => { });

        cache.Set("a", "alpha");
        cache.Clear();
        cache.Set("b", "bravo");

        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public void A_Cache_Of_Capacity_One_Keeps_Only_The_Last_Entry()
    {
        var evicted = new List<string>();
        var cache = new LruCache<string, string>(1, evicted.Add);

        cache.Set("a", "alpha");
        cache.Set("b", "bravo");
        cache.Set("c", "charlie");

        Assert.Equal(["alpha", "bravo"], evicted);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet("c", out _));
    }

    [Fact]
    public void Eviction_Follows_Insertion_Order_When_Nothing_Is_Read()
    {
        var evicted = new List<int>();
        var cache = new LruCache<int, int>(2, evicted.Add);

        for (int i = 0; i < 5; i++)
        {
            cache.Set(i, i * 10);
        }

        Assert.Equal([0, 10, 20], evicted);
        Assert.Equal(2, cache.Count);
    }
}
