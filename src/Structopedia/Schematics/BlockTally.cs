using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// Turns decoded schematic cells into the block list shown next to a structure preview.
/// </summary>
internal static class BlockTally
{
    /// <summary>
    /// Counts every visible block and tallies the entries that were filtered out.
    /// Fluid layer cells count as ordinary blocks: a water block is a block the player will see.
    /// </summary>
    /// <param name="cells">Cells to count.</param>
    /// <returns>The block list plus the filtered entry counters.</returns>
    internal static TallyResult Count(IEnumerable<SchematicCell> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        var counts = new Dictionary<AssetLocation, int>();
        int metaCount = 0;
        int multiblockCount = 0;
        int randomizerCount = 0;
        int unknownCount = 0;

        foreach (SchematicCell cell in cells)
        {
            switch (BlockClassifier.Classify(cell.Code))
            {
                case BlockRole.MetaMarker:
                    metaCount++;
                    break;

                case BlockRole.MultiblockGhost:
                    multiblockCount++;
                    break;

                case BlockRole.WorldgenRandomizer:
                    randomizerCount++;
                    break;

                case BlockRole.UnknownCode:
                    unknownCount++;
                    break;

                default:
                    AssetLocation code = cell.Code!;
                    counts[code] = counts.TryGetValue(code, out int seen) ? seen + 1 : 1;
                    break;
            }
        }

        var blocks = new List<(AssetLocation Code, int Count)>(counts.Count);
        foreach (KeyValuePair<AssetLocation, int> entry in counts)
        {
            blocks.Add((entry.Key, entry.Value));
        }

        // Dictionary order is an implementation detail, so the comparison always ends on the code:
        // the same cells in a different order must produce the same list.
        blocks.Sort(static (left, right) => right.Count != left.Count
            ? right.Count.CompareTo(left.Count)
            : string.CompareOrdinal(left.Code.ToString(), right.Code.ToString()));

        return new TallyResult(blocks, metaCount, multiblockCount, randomizerCount, unknownCount);
    }
}
