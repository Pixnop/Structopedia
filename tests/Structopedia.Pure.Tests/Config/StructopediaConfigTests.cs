using Structopedia.Config;
using Xunit;

namespace Structopedia.Pure.Tests.Config;

public sealed class StructopediaConfigTests
{
    [Fact]
    public void A_New_Config_Carries_The_Documented_Defaults()
    {
        var config = new StructopediaConfig();

        Assert.False(config.ShowStoryStructures);
        Assert.Equal(4, config.PreviewCacheSize);
        Assert.Equal(3_000_000, config.MaxPreviewVertices);
    }

    [Fact]
    public void Sanitized_Leaves_A_Default_Config_Alone()
    {
        StructopediaConfig sanitized = new StructopediaConfig().Sanitized();

        Assert.False(sanitized.ShowStoryStructures);
        Assert.Equal(4, sanitized.PreviewCacheSize);
        Assert.Equal(3_000_000, sanitized.MaxPreviewVertices);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(-1, 4)]
    [InlineData(int.MinValue, 4)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(64, 64)]
    public void Sanitized_Restores_An_Unusable_Cache_Size(int configured, int expected)
    {
        var config = new StructopediaConfig { PreviewCacheSize = configured };

        Assert.Equal(expected, config.Sanitized().PreviewCacheSize);
    }

    [Theory]
    [InlineData(0, 3_000_000)]
    [InlineData(-5, 3_000_000)]
    [InlineData(int.MinValue, 3_000_000)]
    [InlineData(1, 1)]
    [InlineData(500_000, 500_000)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Sanitized_Restores_An_Unusable_Vertex_Budget(int configured, int expected)
    {
        var config = new StructopediaConfig { MaxPreviewVertices = configured };

        Assert.Equal(expected, config.Sanitized().MaxPreviewVertices);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Sanitized_Keeps_The_Story_Toggle(bool showStory)
    {
        var config = new StructopediaConfig { ShowStoryStructures = showStory };

        Assert.Equal(showStory, config.Sanitized().ShowStoryStructures);
    }

    [Fact]
    public void Sanitized_Does_Not_Touch_The_Config_It_Was_Called_On()
    {
        var config = new StructopediaConfig { PreviewCacheSize = -3, MaxPreviewVertices = 0 };

        config.Sanitized();

        Assert.Equal(-3, config.PreviewCacheSize);
        Assert.Equal(0, config.MaxPreviewVertices);
    }

    [Fact]
    public void Sanitized_Is_Idempotent()
    {
        var config = new StructopediaConfig
        {
            ShowStoryStructures = true,
            PreviewCacheSize = -3,
            MaxPreviewVertices = 0
        };

        StructopediaConfig once = config.Sanitized();
        StructopediaConfig twice = once.Sanitized();

        Assert.Equal(once.ShowStoryStructures, twice.ShowStoryStructures);
        Assert.Equal(once.PreviewCacheSize, twice.PreviewCacheSize);
        Assert.Equal(once.MaxPreviewVertices, twice.MaxPreviewVertices);
    }
}
