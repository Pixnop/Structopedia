using System.Collections.Generic;
using System.Linq;
using Structopedia.Catalog;
using Xunit;

namespace Structopedia.Pure.Tests.Catalog;

public sealed class CatalogBuilderTests
{
    [Fact]
    public void Build_Returns_Nothing_For_No_Schematics()
    {
        Assert.Empty(CatalogBuilder.Build([]));
    }

    [Fact]
    public void Build_Groups_Files_By_Their_Parent_Folder()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("trader/cold/tent1.json"),
            Game("trader/cold/tent2.json")
        ]);

        StructureGroup group = Assert.Single(groups);
        Assert.Equal("trader/cold", group.Key);
        Assert.Equal("Trader / Cold", group.Title);
        Assert.Equal(2, group.Variants.Count);
    }

    [Fact]
    public void Build_Keeps_Different_Folders_Apart()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("trader/cold/tent1.json"),
            Game("trader/forest/hut1.json")
        ]);

        Assert.Equal(["trader/cold", "trader/forest"], groups.Select(group => group.Key));
        Assert.Equal(["Trader / Cold", "Trader / Forest"], groups.Select(group => group.Title));
    }

    [Fact]
    public void Build_Puts_Root_Level_Files_In_A_Miscellaneous_Group()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build([Game("well.json")]));

        Assert.Equal(string.Empty, group.Key);
        Assert.Equal("Miscellaneous", group.Title);
        Assert.Equal("Well", Assert.Single(group.Variants).Title);
    }

    [Fact]
    public void Build_Humanizes_Variant_Names()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build([Game("vug/smokyquartz/vug-medium1.json")]));

        StructureVariant variant = Assert.Single(group.Variants);
        Assert.Equal("Vug medium 1", variant.Title);
        Assert.Equal("vug/smokyquartz/vug-medium1.json", variant.RelativePath);
    }

    [Fact]
    public void Build_Sorts_Variants_In_Natural_Order()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build(
        [
            Game("surface/ruin/ruin-10.json"),
            Game("surface/ruin/ruin-2.json"),
            Game("surface/ruin/ruin-1.json")
        ]));

        Assert.Equal(["Ruin 1", "Ruin 2", "Ruin 10"], group.Variants.Select(variant => variant.Title));
    }

    [Fact]
    public void Build_Flags_Story_Groups()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("story/devastation/gear/gear-1.json"),
            Game("trader/cold/tent1.json")
        ]);

        StructureGroup story = groups.Single(group => group.Key == "story/devastation/gear");
        Assert.True(story.IsStory);
        Assert.Equal("Story / Devastation / Gear", story.Title);
        Assert.False(groups.Single(group => group.Key == "trader/cold").IsStory);
    }

    [Fact]
    public void Build_Flags_A_Story_File_Sitting_Directly_Under_Story()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build([Game("story/devastationarea-past.json")]));

        Assert.Equal("story", group.Key);
        Assert.Equal("Story", group.Title);
        Assert.True(group.IsStory);
    }

    [Fact]
    public void Build_Does_Not_Flag_A_Folder_Merely_Containing_The_Word_Story()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("storybook/page1.json"),
            Game("surface/story/hut.json")
        ]);

        Assert.All(groups, group => Assert.False(group.IsStory));
    }

    [Fact]
    public void Build_Sorts_Story_Groups_After_Everything_Else()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("story/devastation/gear/gear-1.json"),
            Game("well.json"),
            Game("trader/cold/tent1.json")
        ]);

        Assert.Equal(["Miscellaneous", "Trader / Cold", "Story / Devastation / Gear"], groups.Select(g => g.Title));
    }

    [Fact]
    public void Build_Sorts_Story_Groups_Among_Themselves_By_Title()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("story/devastation/small/small-1.json"),
            Game("story/devastation/gear/gear-1.json")
        ]);

        Assert.Equal(["Story / Devastation / Gear", "Story / Devastation / Small"], groups.Select(g => g.Title));
    }

    [Fact]
    public void Build_Sorts_Curated_Groups_Before_Everything_Else()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("story/devastation/gear/gear-1.json"),
            Game("aqueduct/arch1.json"),
            Curated("builds/charcoal-pit/sealed-mound.json")
        ]);

        Assert.Equal(
            ["Builds / Charcoal pit", "Aqueduct", "Story / Devastation / Gear"],
            groups.Select(group => group.Title));
    }

    [Fact]
    public void Build_Sorts_Curated_Groups_Among_Themselves_By_Title()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Curated("builds/charcoal-pit/sealed-mound.json"),
            Curated("builds/bloomery/with-chimney.json")
        ]);

        Assert.Equal(["Builds / Bloomery", "Builds / Charcoal pit"], groups.Select(group => group.Title));
    }

    [Fact]
    public void Build_Never_Merges_Two_Origins_Sharing_A_Folder()
    {
        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(
        [
            Game("trader/cold/tent1.json"),
            Mod("trader/cold/tent2.json")
        ]);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, group => Assert.Equal("trader/cold", group.Key));
        Assert.All(groups, group => Assert.Single(group.Variants));
        Assert.Equal(["Base game", "Some mod"], groups.Select(group => group.Origin.DisplayName).Order());
    }

    [Fact]
    public void Build_Carries_The_Origin_Of_Its_Files()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build([Mod("trader/cold/tent1.json")]));

        Assert.Equal(StructureOriginKind.Mod, group.Origin.Kind);
        Assert.Equal("Some mod", group.Origin.DisplayName);
    }

    [Fact]
    public void Build_Normalizes_Windows_Separators()
    {
        StructureGroup group = Assert.Single(CatalogBuilder.Build([Game(@"trader\cold\tent1.json")]));

        Assert.Equal("trader/cold", group.Key);
        Assert.Equal("Trader / Cold", group.Title);
        Assert.Equal("trader/cold/tent1.json", Assert.Single(group.Variants).RelativePath);
    }

    [Fact]
    public void Build_Is_Independent_Of_The_Input_Order()
    {
        ScannedSchematic[] scanned =
        [
            Game("trader/forest/hut1.json"),
            Game("story/devastation/gear/gear-2.json"),
            Game("trader/cold/tent1.json"),
            Game("story/devastation/gear/gear-1.json"),
            Game("well.json")
        ];

        Assert.Equal(
            CatalogBuilder.Build(scanned).Select(group => group.Title),
            CatalogBuilder.Build(scanned.Reverse().ToArray()).Select(group => group.Title));
    }

    private static ScannedSchematic Game(string relativePath)
        => new(relativePath, new StructureOrigin(StructureOriginKind.Game, "Base game"));

    private static ScannedSchematic Mod(string relativePath)
        => new(relativePath, new StructureOrigin(StructureOriginKind.Mod, "Some mod"));

    private static ScannedSchematic Curated(string relativePath)
        => new(relativePath, new StructureOrigin(StructureOriginKind.Curated, "Structopedia"));
}
