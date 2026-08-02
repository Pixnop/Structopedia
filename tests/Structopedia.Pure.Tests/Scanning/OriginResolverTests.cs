using System.Collections.Generic;
using Structopedia.Catalog;
using Structopedia.Scanning;
using Xunit;

namespace Structopedia.Pure.Tests.Scanning;

public sealed class OriginResolverTests
{
    private static readonly IReadOnlyDictionary<string, string> NoMods = new Dictionary<string, string>();

    [Fact]
    public void Resolve_Reports_The_Game_For_An_Origin_Below_The_Game_Assets()
    {
        StructureOrigin origin = OriginResolver.Resolve("/opt/vs/assets/survival", "/opt/vs/assets", NoMods);

        Assert.Equal(StructureOriginKind.Game, origin.Kind);
        Assert.Equal("Vintage Story", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Reports_The_Game_For_The_Assets_Root_Itself()
    {
        StructureOrigin origin = OriginResolver.Resolve("/opt/vs/assets", "/opt/vs/assets", NoMods);

        Assert.Equal(StructureOriginKind.Game, origin.Kind);
    }

    [Fact]
    public void Resolve_Ignores_Trailing_Separators()
    {
        StructureOrigin origin = OriginResolver.Resolve("/opt/vs/assets/game/", "/opt/vs/assets/", NoMods);

        Assert.Equal(StructureOriginKind.Game, origin.Kind);
    }

    [Fact]
    public void Resolve_Accepts_Backslash_Separators()
    {
        StructureOrigin origin = OriginResolver.Resolve(
            @"C:\Games\VintageStory\assets\survival\",
            @"C:\Games\VintageStory\assets",
            NoMods);

        Assert.Equal(StructureOriginKind.Game, origin.Kind);
    }

    [Fact]
    public void Resolve_Does_Not_Mistake_A_Sibling_Folder_For_The_Game_Assets()
    {
        StructureOrigin origin = OriginResolver.Resolve("/opt/vs/assetsbackup/survival", "/opt/vs/assets", NoMods);

        Assert.Equal(StructureOriginKind.Mod, origin.Kind);
    }

    [Fact]
    public void Resolve_Names_A_Mod_From_The_Loaded_Mod_Folders()
    {
        var mods = new Dictionary<string, string> { ["/home/p/VintagestoryData/Mods/tidy"] = "Tidy Structures" };

        StructureOrigin origin = OriginResolver.Resolve(
            "/home/p/VintagestoryData/Mods/tidy/assets",
            "/opt/vs/assets",
            mods);

        Assert.Equal(StructureOriginKind.Mod, origin.Kind);
        Assert.Equal("Tidy Structures", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Matches_A_Mod_Folder_Written_With_A_Trailing_Separator()
    {
        var mods = new Dictionary<string, string> { ["/mods/tidy/"] = "Tidy Structures" };

        StructureOrigin origin = OriginResolver.Resolve("/mods/tidy/assets/", "/opt/vs/assets", mods);

        Assert.Equal("Tidy Structures", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Skips_A_Mod_Folder_That_Carries_No_Name()
    {
        var mods = new Dictionary<string, string> { ["/mods/tidy"] = "   " };

        StructureOrigin origin = OriginResolver.Resolve("/mods/tidy/assets", "/opt/vs/assets", mods);

        Assert.Equal("tidy", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Falls_Back_To_The_Folder_Holding_The_Assets()
    {
        StructureOrigin origin = OriginResolver.Resolve("/mods/coolstructures/assets", "/opt/vs/assets", NoMods);

        Assert.Equal(StructureOriginKind.Mod, origin.Kind);
        Assert.Equal("coolstructures", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Drops_The_Hash_An_Unpacked_Archive_Carries()
    {
        StructureOrigin origin = OriginResolver.Resolve("/cache/unpack/monmod_ab12cd/assets", "/opt/vs/assets", NoMods);

        Assert.Equal("monmod", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Drops_Both_The_Hash_And_The_Archive_Extension()
    {
        StructureOrigin origin = OriginResolver.Resolve(
            "/cache/unpack/monmod_1.0.0.zip_ab12cd34ef56/assets",
            "/opt/vs/assets",
            NoMods);

        Assert.Equal("monmod_1.0.0", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Keeps_A_Suffix_That_Is_Too_Short_To_Be_A_Hash()
    {
        StructureOrigin origin = OriginResolver.Resolve("/mods/mod_ab1/assets", "/opt/vs/assets", NoMods);

        Assert.Equal("mod_ab1", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Keeps_A_Suffix_That_Is_Not_Hexadecimal()
    {
        StructureOrigin origin = OriginResolver.Resolve("/mods/mod_winter/assets", "/opt/vs/assets", NoMods);

        Assert.Equal("mod_winter", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Uses_The_Last_Segment_When_The_Origin_Is_Not_An_Assets_Folder()
    {
        StructureOrigin origin = OriginResolver.Resolve("/somewhere/extradomain", "/opt/vs/assets", NoMods);

        Assert.Equal("extradomain", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Names_An_Unreadable_Origin_Rather_Than_Failing()
    {
        StructureOrigin origin = OriginResolver.Resolve("/", "/opt/vs/assets", NoMods);

        Assert.Equal(StructureOriginKind.Mod, origin.Kind);
        Assert.Equal("Unknown", origin.DisplayName);
    }

    [Fact]
    public void Resolve_Ignores_An_Empty_Game_Assets_Path()
    {
        StructureOrigin origin = OriginResolver.Resolve("/mods/tidy/assets", string.Empty, NoMods);

        Assert.Equal(StructureOriginKind.Mod, origin.Kind);
        Assert.Equal("tidy", origin.DisplayName);
    }
}
