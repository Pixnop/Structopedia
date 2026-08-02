using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>
/// Outcome of a structure mesh build: one CPU mesh per layer holding something, the structure
/// extents and per-block counters describing what went in and what was left out.
/// </summary>
internal sealed class MeshBuildResult
{
    internal MeshBuildResult(int sizeX, int sizeY, int sizeZ, int minLayer, int maxLayer)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        MinLayer = minLayer;
        MaxLayer = maxLayer;
    }

    /// <summary>Meshes from the lowest layer up, one per layer that ended up with geometry.</summary>
    internal List<LayerMesh> Layers { get; } = [];

    internal int SizeX { get; }

    internal int SizeY { get; }

    internal int SizeZ { get; }

    /// <summary>Lowest layer of the structure, whatever the budget let through.</summary>
    internal int MinLayer { get; }

    /// <summary>Highest layer of the structure, whatever the budget let through.</summary>
    internal int MaxLayer { get; }

    /// <summary>Blocks whose mesh was merged into a layer.</summary>
    internal int MergedCount { get; set; }

    /// <summary>Blocks skipped because their code is a meta or multiblock placeholder.</summary>
    internal int FilteredCount { get; set; }

    /// <summary>Blocks skipped because the code did not resolve to a loaded block.</summary>
    internal int UnknownCount { get; set; }

    /// <summary>Blocks that resolved but produced no geometry, neither cached nor tesselated.</summary>
    internal int EmptyMeshCount { get; set; }

    /// <summary>Chiselled blocks drawn from their block entity data rather than from their block type.</summary>
    internal int ChiseledCount { get; set; }

    /// <summary>
    /// Chiselled blocks left out because their block entity data was missing, unreadable, or built
    /// from materials this install does not have. They are left out rather than drawn as the
    /// placeholder cube their block type carries, which is all that block type has.
    /// </summary>
    internal int ChiseledFallbackCount { get; set; }

    /// <summary>
    /// Blocks left out because their shape lives in a block entity this preview has no way to
    /// rebuild, the clutter family and its relatives. Same reasoning: no placeholder cube.
    /// </summary>
    internal int ClutterSkippedCount { get; set; }

    /// <summary>
    /// True when the build stopped on the vertex budget instead of running out of blocks. The layers
    /// that were built are whole, the ones above simply are not there.
    /// </summary>
    internal bool Truncated { get; set; }

    /// <summary>Layer the budget ran out on, or null when the whole structure was built.</summary>
    internal int? TruncatedAtLayer { get; set; }

    /// <summary>Vertices across every layer built.</summary>
    internal int VerticesCount { get; set; }
}
