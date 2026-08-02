using System;
using System.Collections.Generic;
using Structopedia.Schematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Structopedia.Preview;

/// <summary>
/// Turns a parsed <see cref="BlockSchematic"/> into one client-side mesh per layer, so the preview
/// can show the structure up to any height.
/// </summary>
internal static class SchematicMeshBuilder
{
    /// <summary>Vertices a fully tesselated cube contributes; used to pre-size a layer mesh.</summary>
    private const int VerticesPerBlockEstimate = 24;

    /// <summary>Indices a fully tesselated cube contributes; used to pre-size a layer mesh.</summary>
    private const int IndicesPerBlockEstimate = 36;

    /// <summary>Upper bound on the blocks one layer pre-allocates for, so a huge one cannot ask for a huge buffer.</summary>
    private const int PreallocationCeiling = 20_000;

    /// <summary>
    /// Block families whose shape lives in their block entity and which fall back to a textureless
    /// cube without it. Drawing that cube is what fills a preview with placeholder blocks, so they
    /// are left out instead.
    /// <para>
    /// <c>BlockShapeFromAttributes</c> is the base of the clutter family: clutter itself, its
    /// aquatic and devastation variants, banners, the cluttered bookshelves and loose rubble. Its
    /// sibling <c>BlockShapeMaterialFromAttributes</c> is deliberately not here: scroll racks, antler
    /// mounts and bookshelves name a real shape in their own block type, so their default mesh is
    /// the right shape in the wrong wood rather than a placeholder.
    /// </para>
    /// </summary>
    private static readonly Type[] ShapeFromBlockEntityTypes = [typeof(BlockShapeFromAttributes)];

    /// <summary>
    /// Builds the meshes of one structure, layer by layer from the ground up. Must run on the main
    /// thread: it touches the tesselator and the block atlas.
    /// </summary>
    /// <param name="capi">Client API.</param>
    /// <param name="schematic">Parsed schematic. It is never mutated.</param>
    /// <param name="maxVertices">
    /// Vertex budget of the whole preview. Layers are built from the bottom up until it runs out,
    /// and the build then reports itself truncated along with the layer it stopped on. That is what
    /// keeps a cathedral sized structure from freezing the game.
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
        LayerIndex layers = LayerIndex.Build(cells);

        if (layers.MinLayer is not int minLayer || layers.MaxLayer is not int maxLayer)
        {
            return new MeshBuildResult(schematic.SizeX, schematic.SizeY, schematic.SizeZ, 0, 0);
        }

        var result = new MeshBuildResult(schematic.SizeX, schematic.SizeY, schematic.SizeZ, minLayer, maxLayer);
        var budget = new LayerBudget(maxVertices);

        for (int y = minLayer; y <= maxLayer && !budget.Exhausted; y++)
        {
            IReadOnlyList<SchematicCell> layerCells = layers.CellsAt(y);
            if (layerCells.Count == 0)
            {
                continue;
            }

            MeshData layerMesh = NewLayerMesh(layerCells.Count);
            AddLayer(capi, schematic, layerCells, layerMesh, budget, result);

            if (layerMesh.VerticesCount > 0)
            {
                result.Layers.Add(new LayerMesh(y, layerMesh));
            }
        }

        result.VerticesCount = budget.Used;
        result.Truncated = budget.Exhausted;
        result.TruncatedAtLayer = budget.StoppedAtLayer;
        return result;
    }

    private static MeshData NewLayerMesh(int cellCount)
    {
        int preallocated = Math.Clamp(cellCount, 1, PreallocationCeiling);
        return new MeshData(
            preallocated * VerticesPerBlockEstimate,
            preallocated * IndicesPerBlockEstimate,
            withNormals: false,
            withUv: true,
            withRgba: true,
            withFlags: true);
    }

    /// <summary>
    /// Merges every block of one layer into that layer's mesh, at its own position inside the
    /// schematic, so the layers stack back into the structure when they are drawn together.
    /// </summary>
    private static void AddLayer(
        ICoreClientAPI capi,
        BlockSchematic schematic,
        IReadOnlyList<SchematicCell> cells,
        MeshData layerMesh,
        LayerBudget budget,
        MeshBuildResult result)
    {
        foreach (SchematicCell cell in cells)
        {
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

            if (ShapeLivesInBlockEntity(block))
            {
                result.ClutterSkippedCount++;
                continue;
            }

            MeshData? blockMesh;
            if (block is BlockMicroBlock)
            {
                // Built for this one block, so it is merged as is rather than cloned.
                blockMesh = MicroBlockMeshSource.TryBuild(
                    capi,
                    schematic,
                    SchematicCellReader.PackIndex(cell.X, cell.Y, cell.Z));

                if (blockMesh == null)
                {
                    result.ChiseledFallbackCount++;
                    continue;
                }

                result.ChiseledCount++;
            }
            else
            {
                MeshData? shared = GetBlockMesh(capi, block);
                if (shared == null)
                {
                    result.EmptyMeshCount++;
                    continue;
                }

                // The default block mesh belongs to the engine: never mutate or dispose it.
                blockMesh = shared.Clone();
            }

            if (!budget.TryAdd(cell.Y, blockMesh.VerticesCount))
            {
                return;
            }

            layerMesh.AddMeshData(blockMesh, cell.X, cell.Y, cell.Z);
            result.MergedCount++;
        }
    }

    /// <summary>
    /// Tells whether a block falls back to a placeholder cube without the block entity holding its
    /// shape.
    /// </summary>
    private static bool ShapeLivesInBlockEntity(Block block)
    {
        foreach (Type family in ShapeFromBlockEntityTypes)
        {
            if (family.IsInstanceOfType(block))
            {
                return true;
            }
        }

        return false;
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
