using System;
using System.Collections.Generic;
using System.Linq;
using Structopedia.Catalog;
using Xunit;

namespace Structopedia.Pure.Tests.Catalog;

public sealed class CuratedOriginsTests
{
    private const string OwnMod = "Structopedia";

    [Fact]
    public void Apply_Marks_Our_Own_Build_Guides_As_Curated()
    {
        ScannedSchematic result = Single(Ours("builds/bloomery/with-chimney.json"));

        Assert.Equal(StructureOriginKind.Curated, result.Origin.Kind);
        Assert.Equal(OwnMod, result.Origin.DisplayName);
    }

    [Fact]
    public void Apply_Keeps_The_Path_Untouched()
    {
        Assert.Equal(
            "builds/bloomery/with-chimney.json",
            Single(Ours("builds/bloomery/with-chimney.json")).RelativePath);
    }

    [Fact]
    public void Apply_Reads_Windows_Separators()
    {
        Assert.Equal(
            StructureOriginKind.Curated,
            Single(Ours(@"builds\bloomery\with-chimney.json")).Origin.Kind);
    }

    [Fact]
    public void Apply_Leaves_Our_Other_Folders_Alone()
    {
        ScannedSchematic result = Single(Ours("surface/hut1.json"));

        Assert.Equal(StructureOriginKind.Mod, result.Origin.Kind);
        Assert.Equal(OwnMod, result.Origin.DisplayName);
    }

    [Fact]
    public void Apply_Leaves_A_Folder_Merely_Starting_With_The_Word_Alone()
    {
        Assert.Equal(StructureOriginKind.Mod, Single(Ours("buildsite/hut1.json")).Origin.Kind);
    }

    [Fact]
    public void Apply_Leaves_A_Root_Level_File_Alone()
    {
        Assert.Equal(StructureOriginKind.Mod, Single(Ours("builds.json")).Origin.Kind);
    }

    /// <summary>
    /// The label says Structopedia vouches for the layout, so it can only go on files Structopedia
    /// ships. Another mod with a builds folder is still that mod's content.
    /// </summary>
    [Fact]
    public void Apply_Leaves_Another_Mods_Builds_Folder_Alone()
    {
        ScannedSchematic result = Single(
            new ScannedSchematic("builds/bloomery/with-chimney.json", new StructureOrigin(StructureOriginKind.Mod, "Some mod")));

        Assert.Equal(StructureOriginKind.Mod, result.Origin.Kind);
        Assert.Equal("Some mod", result.Origin.DisplayName);
    }

    [Fact]
    public void Apply_Leaves_The_Base_Game_Alone()
    {
        ScannedSchematic result = Single(
            new ScannedSchematic("builds/bloomery/with-chimney.json", new StructureOrigin(StructureOriginKind.Game, "Vintage Story")));

        Assert.Equal(StructureOriginKind.Game, result.Origin.Kind);
    }

    [Fact]
    public void Apply_Compares_The_Mod_Name_Case_Insensitively()
    {
        ScannedSchematic result = Single(
            new ScannedSchematic("builds/bloomery/with-chimney.json", new StructureOrigin(StructureOriginKind.Mod, "structopedia")),
            ownModName: "Structopedia");

        Assert.Equal(StructureOriginKind.Curated, result.Origin.Kind);
    }

    [Fact]
    public void Apply_Reuses_One_Origin_For_Every_Guide()
    {
        IReadOnlyList<ScannedSchematic> results = CuratedOrigins.Apply(
            [Ours("builds/bloomery/a.json"), Ours("builds/charcoal-pit/b.json")],
            OwnMod);

        // The catalog groups by origin, so two guides that do not share one would never sit in the
        // same block of the list.
        Assert.Single(results.Select(result => result.Origin).Distinct());
    }

    [Fact]
    public void Apply_Passes_Everything_Through_When_The_Mod_Name_Is_Blank()
    {
        Assert.Equal(StructureOriginKind.Mod, Single(Ours("builds/bloomery/a.json"), ownModName: " ").Origin.Kind);
    }

    [Fact]
    public void Apply_Rejects_Null_Arguments()
    {
        Assert.Throws<ArgumentNullException>(() => CuratedOrigins.Apply(null!, OwnMod));
        Assert.Throws<ArgumentNullException>(() => CuratedOrigins.Apply([], null!));
    }

    private static ScannedSchematic Ours(string relativePath)
        => new(relativePath, new StructureOrigin(StructureOriginKind.Mod, OwnMod));

    private static ScannedSchematic Single(ScannedSchematic scanned, string ownModName = OwnMod)
        => Assert.Single(CuratedOrigins.Apply([scanned], ownModName));
}
