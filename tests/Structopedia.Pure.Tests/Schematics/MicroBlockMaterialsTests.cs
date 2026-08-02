using System.Collections.Generic;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Xunit;

namespace Structopedia.Pure.Tests.Schematics;

public sealed class MicroBlockMaterialsTests
{
    private static readonly Dictionary<int, AssetLocation> ExportCodes = new()
    {
        [11] = new AssetLocation("game", "rock-granite"),
        [12] = new AssetLocation("game", "planks-oak"),
        [13] = new AssetLocation("somemod", "fancybrick")
    };

    [Fact]
    public void Remap_Turns_Export_Ids_Into_World_Ids()
    {
        int[]? remapped = MicroBlockMaterials.Remap([11, 12], ExportCodes, KnownWorld, substituteUnresolved: true);

        Assert.NotNull(remapped);
        Assert.Equal<int>([911, 912], remapped);
    }

    [Fact]
    public void Remap_Returns_Null_On_No_Ids()
    {
        Assert.Null(MicroBlockMaterials.Remap(null, ExportCodes, KnownWorld, substituteUnresolved: true));
        Assert.Null(MicroBlockMaterials.Remap([], ExportCodes, KnownWorld, substituteUnresolved: true));
    }

    [Fact]
    public void Remap_Keeps_The_Empty_Id_As_Is()
    {
        int[]? remapped = MicroBlockMaterials.Remap([0, 11, 0], ExportCodes, KnownWorld, substituteUnresolved: false);

        Assert.NotNull(remapped);
        Assert.Equal<int>([0, 911, 0], remapped);
    }

    [Fact]
    public void Remap_Substitutes_An_Id_The_Export_Table_Does_Not_Name()
    {
        // 99 is not in the code table at all, which is what a hand edited schematic looks like.
        int[]? remapped = MicroBlockMaterials.Remap([11, 99], ExportCodes, KnownWorld, substituteUnresolved: true);

        Assert.NotNull(remapped);
        Assert.Equal<int>([911, 911], remapped);
    }

    [Fact]
    public void Remap_Substitutes_A_Code_This_Install_Does_Not_Have()
    {
        // 13 names a mod block, which is exactly what a half installed mod set looks like.
        int[]? remapped = MicroBlockMaterials.Remap([13, 12], ExportCodes, KnownWorld, substituteUnresolved: true);

        Assert.NotNull(remapped);
        Assert.Equal<int>([912, 912], remapped);
    }

    [Fact]
    public void Remap_Substitutes_With_The_First_Resolved_Id_Not_The_First_Id()
    {
        int[]? remapped = MicroBlockMaterials.Remap([13, 12, 11], ExportCodes, KnownWorld, substituteUnresolved: true);

        Assert.NotNull(remapped);
        Assert.Equal<int>([912, 912, 911], remapped);
    }

    [Fact]
    public void Remap_Gives_Up_When_Nothing_Resolves()
    {
        Assert.Null(MicroBlockMaterials.Remap([13, 99], ExportCodes, KnownWorld, substituteUnresolved: true));
    }

    [Fact]
    public void Remap_Without_Substitution_Gives_Up_On_The_First_Miss()
    {
        // Decor is positional: face three carrying the material of face one is worse than no decor.
        Assert.Null(MicroBlockMaterials.Remap([11, 13], ExportCodes, KnownWorld, substituteUnresolved: false));
    }

    [Fact]
    public void Remap_Without_Substitution_Keeps_A_Fully_Resolved_Array()
    {
        int[]? remapped = MicroBlockMaterials.Remap([12, 0, 11], ExportCodes, KnownWorld, substituteUnresolved: false);

        Assert.NotNull(remapped);
        Assert.Equal<int>([912, 0, 911], remapped);
    }

    [Fact]
    public void Remap_Leaves_The_Source_Untouched()
    {
        int[] source = [11, 12];

        MicroBlockMaterials.Remap(source, ExportCodes, KnownWorld, substituteUnresolved: true);

        Assert.Equal<int>([11, 12], source);
    }

    /// <summary>
    /// Stands in for the block registry of the running client: the two game blocks are installed,
    /// the mod block is not.
    /// </summary>
    private static int? KnownWorld(AssetLocation code) => code.ToShortString() switch
    {
        "rock-granite" => 911,
        "planks-oak" => 912,
        _ => null
    };
}
