using System;
using System.Collections.Generic;

namespace Structopedia.Schematics;

/// <summary>
/// Groups schematic cells by their Y coordinate so the preview can build one mesh per layer and let
/// a slider reveal the structure from the ground up.
/// </summary>
internal sealed class LayerIndex
{
    private readonly Dictionary<int, List<SchematicCell>> _layers;

    private LayerIndex(Dictionary<int, List<SchematicCell>> layers, int? minLayer, int? maxLayer)
    {
        _layers = layers;
        MinLayer = minLayer;
        MaxLayer = maxLayer;
    }

    /// <summary>Lowest layer holding at least one cell, or null when there is no cell at all.</summary>
    internal int? MinLayer { get; }

    /// <summary>Highest layer holding at least one cell, or null when there is no cell at all.</summary>
    internal int? MaxLayer { get; }

    /// <summary>
    /// Number of layers from <see cref="MinLayer"/> to <see cref="MaxLayer"/> inclusive, empty layers
    /// in between included, because the slider still has to step through them. Zero when empty.
    /// </summary>
    internal int LayerCount => MinLayer is int min && MaxLayer is int max ? (max - min) + 1 : 0;

    /// <summary>Groups cells by layer.</summary>
    /// <param name="cells">Cells to index.</param>
    /// <returns>The layer index.</returns>
    internal static LayerIndex Build(IEnumerable<SchematicCell> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        var layers = new Dictionary<int, List<SchematicCell>>();
        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (SchematicCell cell in cells)
        {
            if (!layers.TryGetValue(cell.Y, out List<SchematicCell>? layer))
            {
                layer = [];
                layers[cell.Y] = layer;
            }

            layer.Add(cell);

            min = Math.Min(min, cell.Y);
            max = Math.Max(max, cell.Y);
        }

        return layers.Count == 0
            ? new LayerIndex(layers, null, null)
            : new LayerIndex(layers, min, max);
    }

    /// <summary>Returns the cells sitting on one layer, in the order they were read.</summary>
    /// <param name="y">Layer to look up.</param>
    /// <returns>The cells of that layer, empty when the layer holds nothing.</returns>
    internal IReadOnlyList<SchematicCell> CellsAt(int y)
        => _layers.TryGetValue(y, out List<SchematicCell>? layer) ? layer : Array.Empty<SchematicCell>();
}
