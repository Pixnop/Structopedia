using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests;

public sealed class StructopediaModSystemTests
{
    [Fact]
    public void ShouldLoad_Is_True_On_Client()
    {
        var system = new StructopediaModSystem();
        Assert.True(system.ShouldLoad(EnumAppSide.Client));
    }

    [Fact]
    public void ShouldLoad_Is_False_On_Server()
    {
        var system = new StructopediaModSystem();
        Assert.False(system.ShouldLoad(EnumAppSide.Server));
    }

    [Fact]
    public void ShouldLoad_Is_False_On_Universal()
    {
        var system = new StructopediaModSystem();
        Assert.False(system.ShouldLoad(EnumAppSide.Universal));
    }
}
