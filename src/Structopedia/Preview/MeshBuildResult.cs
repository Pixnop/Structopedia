using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>
/// Outcome of a structure mesh build: the merged CPU mesh, the structure extents and per-block
/// counters describing what went in and what was left out.
/// </summary>
internal sealed class MeshBuildResult
{
    internal MeshBuildResult(MeshData mesh, int sizeX, int sizeY, int sizeZ)
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

    /// <summary>
    /// True when the build stopped on the vertex budget instead of running out of blocks. The mesh is
    /// still drawable, it just does not show the whole structure.
    /// </summary>
    internal bool Truncated { get; set; }
}
