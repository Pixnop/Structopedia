using System.Collections.Generic;
using Structopedia.Catalog;
using Structopedia.Handbook;
using Xunit;

namespace Structopedia.Pure.Tests.Handbook;

public sealed class PageCodesTests
{
    private static readonly StructureOrigin Game = new StructureOrigin(StructureOriginKind.Game, "Vintage Story");

    [Fact]
    public void Assign_Builds_A_Code_From_The_Folder_Path()
    {
        Assert.Equal(["structopedia-trader-cold"], PageCodes.Assign([Group("trader/cold", Game)]));
    }

    [Fact]
    public void Assign_Names_The_Root_Group()
    {
        Assert.Equal(["structopedia-misc"], PageCodes.Assign([Group(string.Empty, Game)]));
    }

    [Fact]
    public void Assign_Adds_The_Mod_Name_So_Two_Origins_Never_Collide()
    {
        StructureOrigin mod = new StructureOrigin(StructureOriginKind.Mod, "Cool Structures");

        IReadOnlyList<string> codes = PageCodes.Assign([Group("trader/cold", Game), Group("trader/cold", mod)]);

        Assert.Equal(["structopedia-trader-cold", "structopedia-trader-cold-cool-structures"], codes);
    }

    [Fact]
    public void Assign_Replaces_Anything_A_Page_Code_Cannot_Carry()
    {
        Assert.Equal(
            ["structopedia-tour-d-eau-2"],
            PageCodes.Assign([Group("Tour d'Eau/#2", Game)]));
    }

    [Fact]
    public void Assign_Keeps_Digits_Where_The_Folder_Puts_Them()
    {
        Assert.Equal(["structopedia-vug2b"], PageCodes.Assign([Group("vug2b", Game)]));
    }

    [Fact]
    public void Assign_Numbers_Codes_That_Would_Repeat()
    {
        StructureOrigin first = new StructureOrigin(StructureOriginKind.Mod, "Ruins!");
        StructureOrigin second = new StructureOrigin(StructureOriginKind.Mod, "Ruins?");

        IReadOnlyList<string> codes = PageCodes.Assign([Group("surface", first), Group("surface", second)]);

        Assert.Equal(["structopedia-surface-ruins", "structopedia-surface-ruins-2"], codes);
    }

    [Fact]
    public void Assign_Falls_Back_To_The_Folder_When_A_Mod_Name_Slugs_To_Nothing()
    {
        StructureOrigin mod = new StructureOrigin(StructureOriginKind.Mod, "!!!");

        Assert.Equal(["structopedia-surface"], PageCodes.Assign([Group("surface", mod)]));
    }

    [Fact]
    public void Assign_Returns_One_Code_Per_Group_In_Order()
    {
        IReadOnlyList<string> codes = PageCodes.Assign(
        [
            Group("well", Game),
            Group("vug/amethyst", Game),
            Group("trader/cold", Game)
        ]);

        Assert.Equal(["structopedia-well", "structopedia-vug-amethyst", "structopedia-trader-cold"], codes);
    }

    private static StructureGroup Group(string key, StructureOrigin origin)
        => new StructureGroup(key, key, origin, false, []);
}
