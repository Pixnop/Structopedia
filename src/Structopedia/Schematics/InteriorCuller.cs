using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// Finds the blocks a viewer can never see because opaque neighbours seal them in on all six sides.
/// <para>
/// This only holds for the complete view of a structure. A layer mesh must be built from the full
/// cell list, because slicing a structure open exposes exactly the blocks this pass drops.
/// </para>
/// </summary>
internal static class InteriorCuller
{
    /// <summary>
    /// Collects the positions of the visible blocks that are fully enclosed by opaque full cubes.
    /// Fluid layer cells take no part: they neither occlude nor get culled, since a waterlogged cell
    /// is already represented by its solid half.
    /// </summary>
    /// <param name="cells">Cells of the whole structure.</param>
    /// <param name="isOpaqueFullCube">
    /// Tells whether a block code renders as a solid opaque cube. The client layer answers this from
    /// the loaded block list; it is a parameter so this pass stays free of any world dependency.
    /// Fully qualified because Vintagestory.API.Common declares a Func delegate of its own.
    /// </param>
    /// <returns>Positions that can be skipped when meshing the complete structure.</returns>
    internal static HashSet<(int X, int Y, int Z)> FindHiddenCells(
        IReadOnlyCollection<SchematicCell> cells,
        System.Func<AssetLocation, bool> isOpaqueFullCube)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(isOpaqueFullCube);

        var candidates = new List<(int X, int Y, int Z)>(cells.Count);
        var occluders = new HashSet<(int X, int Y, int Z)>();

        foreach (SchematicCell cell in cells)
        {
            if (cell.IsFluidLayer || BlockClassifier.Classify(cell.Code) != BlockRole.Visible)
            {
                continue;
            }

            (int X, int Y, int Z) position = (cell.X, cell.Y, cell.Z);
            candidates.Add(position);

            if (isOpaqueFullCube(cell.Code!))
            {
                occluders.Add(position);
            }
        }

        var hidden = new HashSet<(int X, int Y, int Z)>();
        foreach ((int x, int y, int z) in candidates)
        {
            bool sealedIn = occluders.Contains((x - 1, y, z))
                && occluders.Contains((x + 1, y, z))
                && occluders.Contains((x, y - 1, z))
                && occluders.Contains((x, y + 1, z))
                && occluders.Contains((x, y, z - 1))
                && occluders.Contains((x, y, z + 1));

            if (sealedIn)
            {
                hidden.Add((x, y, z));
            }
        }

        return hidden;
    }
}
