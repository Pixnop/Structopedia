using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Structopedia.Spike;

/// <summary>
/// Turns a packed <see cref="BlockSchematic"/> shipped as a worldgen asset into a single client-side mesh.
/// </summary>
internal static class SpikeMeshBuilder
{
    /// <summary>Asset path suffix of the schematic the spike renders (7x7x8, ~2.8 KB).</summary>
    internal const string SchematicPathSuffix = "vug/smokyquartz/vug-medium1.json";

    /// <summary>Vertices a fully tesselated cube contributes; used to pre-size the target mesh.</summary>
    private const int VerticesPerBlockEstimate = 24;

    /// <summary>Indices a fully tesselated cube contributes; used to pre-size the target mesh.</summary>
    private const int IndicesPerBlockEstimate = 36;

    /// <summary>
    /// Loads the spike schematic and merges the default mesh of every block it contains.
    /// Must run on the main thread: it touches the tesselator and the block atlas.
    /// </summary>
    /// <param name="capi">Client API.</param>
    /// <param name="logger">Logger used to report a missing or unreadable asset.</param>
    /// <returns>The build result, or null when the schematic could not be found or parsed.</returns>
    internal static SpikeMeshBuildResult? Build(ICoreClientAPI capi, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(capi);
        ArgumentNullException.ThrowIfNull(logger);

        IAsset? asset = FindSchematicAsset(capi);
        if (asset == null)
        {
            logger.Warning("Spike schematic '{0}' not found in any asset origin.", SchematicPathSuffix);
            return null;
        }

        string error = string.Empty;
        BlockSchematic? schematic = BlockSchematic.LoadFromString(asset.ToText(), ref error);
        if (schematic == null)
        {
            logger.Warning("Spike schematic '{0}' failed to parse: {1}", asset.Location, error);
            return null;
        }

        // No Init() and no Remap() on purpose: both rely on statics that only the server populates
        // (BlockRemaps / ItemRemaps, the filler and pathway block ids), so calling them from a
        // client connected to a remote server throws.
        int blockCount = Math.Min(schematic.Indices.Count, schematic.BlockIds.Count);
        var target = new MeshData(
            Math.Max(1, blockCount * VerticesPerBlockEstimate),
            Math.Max(1, blockCount * IndicesPerBlockEstimate),
            withNormals: false,
            withUv: true,
            withRgba: true,
            withFlags: true);

        var result = new SpikeMeshBuildResult(target, schematic.SizeX, schematic.SizeY, schematic.SizeZ);

        for (int i = 0; i < blockCount; i++)
        {
            uint index = schematic.Indices[i];
            int dx = (int)(index & 0x3FFu);
            int dy = (int)((index >> 20) & 0x3FFu);
            int dz = (int)((index >> 10) & 0x3FFu);

            if (!schematic.BlockCodes.TryGetValue(schematic.BlockIds[i], out AssetLocation? code) || code == null)
            {
                result.UnknownCount++;
                continue;
            }

            if (IsFiltered(code))
            {
                result.FilteredCount++;
                continue;
            }

            Block? block = capi.World.GetBlock(code);
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
            target.AddMeshData(blockMesh.Clone(), dx, dy, dz);
            result.MergedCount++;
        }

        return result;
    }

    /// <summary>
    /// Meta and multiblock placeholders carry no renderable geometry in a preview.
    /// </summary>
    private static bool IsFiltered(AssetLocation code)
    {
        string path = code.Path;
        return path.StartsWith("meta-", StringComparison.Ordinal)
            || path.StartsWith("multiblock", StringComparison.Ordinal);
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

    /// <summary>
    /// Worldgen assets are server-side, so they are not in the client asset manager: the origins are
    /// scanned directly and the matching file is loaded on demand.
    /// </summary>
    private static IAsset? FindSchematicAsset(ICoreClientAPI capi)
    {
        foreach (IAssetOrigin origin in capi.Assets.Origins)
        {
            List<IAsset> assets = origin.GetAssets(AssetCategory.worldgen, shouldLoad: false);
            foreach (IAsset asset in assets)
            {
                if (!asset.Location.Path.EndsWith(SchematicPathSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (origin.TryLoadAsset(asset))
                {
                    return asset;
                }
            }
        }

        return null;
    }
}
