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

        return BlockRole.Visible;
    }
}
