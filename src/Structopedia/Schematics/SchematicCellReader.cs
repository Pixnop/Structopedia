using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// Decodes the packed block lists of a <see cref="BlockSchematic"/> into positioned cells.
/// </summary>
internal static class SchematicCellReader
{
    /// <summary>Keeps the ten bits a single coordinate occupies inside a packed index.</summary>
    private const int CoordinateMask = 0x3FF;

    /// <summary>Bit offset of the Z coordinate inside a packed index.</summary>
    private const int ZShift = 10;

    /// <summary>Bit offset of the Y coordinate inside a packed index.</summary>
    private const int YShift = 20;

    /// <summary>
    /// Walks the parallel index and block id lists and unpacks each entry into a cell.
    /// A schematic stores a waterlogged cell as two consecutive entries sharing the same index; the
    /// second one carries the fluid and is flagged as such.
    /// </summary>
    /// <param name="schematic">Schematic to read. It is never mutated.</param>
    /// <returns>The cells, in the order the schematic stores them.</returns>
    internal static IReadOnlyList<SchematicCell> ReadCells(BlockSchematic schematic)
    {
        ArgumentNullException.ThrowIfNull(schematic);

        List<uint> indices = schematic.Indices;
        List<int> blockIds = schematic.BlockIds;
        int count = Math.Min(indices.Count, blockIds.Count);

        var cells = new List<SchematicCell>(count);
        for (int i = 0; i < count; i++)
        {
            uint index = indices[i];
            schematic.BlockCodes.TryGetValue(blockIds[i], out AssetLocation? code);

            cells.Add(new SchematicCell(
                (int)(index & CoordinateMask),
                (int)((index >> YShift) & CoordinateMask),
                (int)((index >> ZShift) & CoordinateMask),
                code,
                i > 0 && indices[i - 1] == index));
        }

        return cells;
    }
}
