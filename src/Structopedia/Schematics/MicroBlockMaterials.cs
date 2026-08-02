using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// Moves the material ids a chiselled block carries from the world it was exported in onto the world
/// that is running now.
/// <para>
/// A microblock keeps its materials as raw block ids in its block entity data, and those ids are
/// only meaningful in the install the schematic was saved from. The schematic carries the table that
/// gives them a meaning, <c>BlockSchematic.BlockCodes</c>, and the game walks it at placement time
/// through <c>BlockEntityMicroBlock.OnLoadCollectibleMappings</c>. A preview never places anything,
/// so it has to walk that table itself, and that is all this does.
/// </para>
/// </summary>
internal static class MicroBlockMaterials
{
    /// <summary>
    /// Id standing for "no block here". Decor arrays use it for a face carrying nothing, and it is
    /// never a code the export table names, so the remap hands it through untouched.
    /// </summary>
    internal const int Empty = 0;

    /// <summary>
    /// Remaps a run of export block ids onto the running world.
    /// </summary>
    /// <param name="exportIds">Ids as stored in the block entity data. Never mutated.</param>
    /// <param name="exportCodes">
    /// Export id to block code table of the schematic, that is <c>BlockSchematic.BlockCodes</c>.
    /// </param>
    /// <param name="resolve">
    /// Turns a block code into the id it has in the running world, or null when this install has no
    /// such block.
    /// </param>
    /// <param name="substituteUnresolved">
    /// What to do with an id this install cannot place. True stands it in for the first id that did
    /// resolve, which keeps the shape of the block at the cost of one wrong material. False gives up
    /// on the whole run, which is what a positional array like decor needs, since a face wearing the
    /// material of another face is worse than a face wearing none.
    /// </param>
    /// <returns>
    /// The remapped ids, or null when there is nothing to remap or when nothing could be resolved.
    /// </returns>
    internal static int[]? Remap(
        IReadOnlyList<int>? exportIds,
        IReadOnlyDictionary<int, AssetLocation> exportCodes,
        System.Func<AssetLocation, int?> resolve,
        bool substituteUnresolved)
    {
        ArgumentNullException.ThrowIfNull(exportCodes);
        ArgumentNullException.ThrowIfNull(resolve);

        if (exportIds == null || exportIds.Count == 0)
        {
            return null;
        }

        var remapped = new int[exportIds.Count];
        int firstResolved = Empty;
        bool anyUnresolved = false;

        for (int i = 0; i < exportIds.Count; i++)
        {
            int exportId = exportIds[i];
            if (exportId == Empty)
            {
                remapped[i] = Empty;
                continue;
            }

            int? worldId = exportCodes.TryGetValue(exportId, out AssetLocation? code) ? resolve(code) : null;
            if (worldId == null)
            {
                anyUnresolved = true;
                continue;
            }

            remapped[i] = worldId.Value;
            if (firstResolved == Empty)
            {
                firstResolved = worldId.Value;
            }
        }

        if (!anyUnresolved)
        {
            return remapped;
        }

        if (!substituteUnresolved || firstResolved == Empty)
        {
            return null;
        }

        for (int i = 0; i < remapped.Length; i++)
        {
            if (remapped[i] == Empty && exportIds[i] != Empty)
            {
                remapped[i] = firstResolved;
            }
        }

        return remapped;
    }
}
