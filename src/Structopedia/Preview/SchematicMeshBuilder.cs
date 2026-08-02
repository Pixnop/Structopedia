using System;
using System.Collections.Generic;
using Structopedia.Schematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Structopedia.Preview;

/// <summary>
/// Merges the default mesh of every block of a parsed <see cref="BlockSchematic"/> into a single
/// client-side mesh.
/// </summary>
internal static class SchematicMeshBuilder
{
    /// <summary>Vertices a fully tesselated cube contributes; used to pre-size the target mesh.</summary>
    private const int VerticesPerBlockEstimate = 24;

    /// <summary>Indices a fully tesselated cube contributes; used to pre-size the target mesh.</summary>
    private const int IndicesPerBlockEstimate = 36;

    /// <summary>Upper bound on the blocks pre-allocated for, so a huge structure cannot ask for a huge buffer.</summary>
    private const int PreallocationCeiling = 20_000;

    /// <summary>
    /// Builds the mesh of one structure. Must run on the main thread: it touches the tesselator and
    /// the block atlas.
    /// </summary>
    /// <param name="capi">Client API.</param>
    /// <param name="schematic">Parsed schematic. It is never mutated.</param>
    /// <param name="maxVertices">
    /// Vertex budget. The build stops once the mesh is past it and reports itself truncated, which is
    /// what keeps a cathedral sized structure from freezing the game.
    /// </param>
    /// <returns>The build result, never null.</returns>
    internal static MeshBuildResult Build(ICoreClientAPI capi, BlockSchematic schematic, int maxVertices)
    {
        ArgumentNullException.ThrowIfNull(capi);
        ArgumentNullException.ThrowIfNull(schematic);

        // No Init() and no Remap() on purpose: both rely on statics that only the server populates
        // (BlockRemaps / ItemRemaps, the filler and pathway block ids), so calling them from a
        // client connected to a remote server throws.
        IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

        int preallocated = Math.Clamp(cells.Count, 1, PreallocationCeiling);
        var target = new MeshData(
            preallocated * VerticesPerBlockEstimate,
            preallocated * IndicesPerBlockEstimate,
            withNormals: false,
            withUv: true,
            withRgba: true,
            withFlags: true);

        var result = new MeshBuildResult(target, schematic.SizeX, schematic.SizeY, schematic.SizeZ);

        foreach (SchematicCell cell in cells)
        {
            if (target.VerticesCount > maxVertices)
            {
                result.Truncated = true;
                break;
            }

            switch (BlockClassifier.Classify(cell.Code))
            {
                case BlockRole.MetaMarker:
                case BlockRole.MultiblockGhost:
                    result.FilteredCount++;
                    continue;

                case BlockRole.UnknownCode:
                    result.UnknownCount++;
                    continue;
            }

            Block? block = capi.World.GetBlock(cell.Code!);
            if (block == null)
            {
                result.UnknownCount++;
                continue;
            }

            MeshData? blockMesh = GetBlockMesh(capi, block);
            if (blockMesh == null)
            {
                result.EmptyMeshCount++;
                continue;
            }

            // The default block mesh belongs to the engine: never mutate or dispose it.
            target.AddMeshData(blockMesh.Clone(), cell.X, cell.Y, cell.Z);
            result.MergedCount++;
        }

        return result;
    }

    private static MeshData? GetBlockMesh(ICoreClientAPI capi, Block block)
    {
        MeshData? mesh = capi.TesselatorManager.GetDefaultBlockMesh(block);
        if (mesh != null && mesh.VerticesCount > 0)
        {
            return mesh;
        }

        capi.Tesselator.TesselateBlock(block, out MeshData tesselated);
        if (tesselated != null && tesselated.VerticesCount > 0)
        {
            return tesselated;
        }

        return null;
    }
}
