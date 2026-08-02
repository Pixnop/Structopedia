using System;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// Sorts block codes into the roles the previews care about: real geometry versus the worldgen
/// bookkeeping blocks that must never reach a mesh or a block list.
/// </summary>
internal static class BlockClassifier
{
    /// <summary>Prefix worn by the worldgen markers the generator strips before placing a structure.</summary>
    private const string MetaPrefix = "meta-";

    /// <summary>Prefix worn by the placeholder blocks standing in for a multiblock body.</summary>
    private const string MultiblockPrefix = "multiblock";

    /// <summary>
    /// First code part of the worldgen randomizer. It sits in <c>blocktypes/meta</c> and is excluded
    /// from the handbook, but it wears no <c>meta-</c> prefix, so it needs naming on its own.
    /// </summary>
    private const string RandomizerCode = "randomizer";

    /// <summary>
    /// Classifies a block code by its path. The domain is ignored, so a mod shipping its own
    /// meta markers is treated exactly like the base game. Matching is ordinal and case sensitive,
    /// which is how the game itself writes those codes.
    /// </summary>
    /// <param name="code">Block code, or null when the schematic did not resolve one.</param>
    /// <returns>The role of that code.</returns>
    internal static BlockRole Classify(AssetLocation? code)
    {
        if (code == null)
        {
            return BlockRole.UnknownCode;
        }

        string path = code.Path;
        if (path.StartsWith(MetaPrefix, StringComparison.Ordinal))
        {
            return BlockRole.MetaMarker;
        }

        if (path.StartsWith(MultiblockPrefix, StringComparison.Ordinal))
        {
            return BlockRole.MultiblockGhost;
        }

        if (IsCodeFamily(path, RandomizerCode))
        {
            return BlockRole.WorldgenRandomizer;
        }

        return BlockRole.Visible;
    }

    /// <summary>
    /// Tells whether a path is a block type or one of its variants, which means the whole first code
    /// part has to match. Unlike the prefix tests above this cannot swallow an unrelated block whose
    /// name merely starts the same way, which matters for a word as ordinary as <c>randomizer</c>.
    /// </summary>
    private static bool IsCodeFamily(string path, string code)
        => path.StartsWith(code, StringComparison.Ordinal)
            && (path.Length == code.Length || path[code.Length] == '-');
}
