using Vintagestory.API.Client;

namespace Structopedia.Spike;

/// <summary>
/// Outcome of a schematic mesh build: the merged CPU mesh, the schematic extents and per-block counters.
/// </summary>
internal sealed class SpikeMeshBuildResult
{
    internal SpikeMeshBuildResult(MeshData mesh, int sizeX, int sizeY, int sizeZ)
    {
        Mesh = mesh;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    internal MeshData Mesh { get; }

    internal int SizeX { get; }

    internal int SizeY { get; }

    internal int SizeZ { get; }

    /// <summary>Blocks whose mesh was merged into <see cref="Mesh"/>.</summary>
    internal int MergedCount { get; set; }

    /// <summary>Blocks skipped because their code is a meta or multiblock placeholder.</summary>
    internal int FilteredCount { get; set; }

    /// <summary>Blocks skipped because the code did not resolve to a loaded block.</summary>
    internal int UnknownCount { get; set; }

    /// <summary>Blocks that resolved but produced no geometry, neither cached nor tesselated.</summary>
    internal int EmptyMeshCount { get; set; }
}
