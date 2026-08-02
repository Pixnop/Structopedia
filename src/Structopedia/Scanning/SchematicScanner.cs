using System;
using System.Collections.Generic;
using System.IO;
using Structopedia.Catalog;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Structopedia.Scanning;

/// <summary>
/// Walks every asset origin the client knows about and lists the worldgen schematics it holds.
/// </summary>
/// <remarks>
/// Worldgen is a server side asset category, so those files never reach the client asset manager.
/// The origins are read directly instead, without loading a single file: only the paths are needed
/// to build the catalog.
/// </remarks>
internal static class SchematicScanner
{
    /// <summary>
    /// Lists the schematics of every origin.
    /// </summary>
    /// <param name="capi">Client API.</param>
    /// <param name="logger">Logger used to report an origin that could not be read.</param>
    /// <returns>Every schematic found, in the order the origins were registered.</returns>
    internal static IReadOnlyList<SchematicScanEntry> Scan(ICoreClientAPI capi, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(capi);
        ArgumentNullException.ThrowIfNull(logger);

        IReadOnlyDictionary<string, string> modNames = ModFolderIndex.Build(capi.ModLoader.Mods);
        string gameAssetsPath = GamePaths.AssetsPath ?? string.Empty;

        var entries = new List<SchematicScanEntry>();
        var seen = new HashSet<(StructureOrigin Origin, string RelativePath)>();

        foreach (IAssetOrigin origin in capi.Assets.Origins)
        {
            StructureOrigin structureOrigin = OriginResolver.Resolve(
                origin.OriginPath ?? string.Empty,
                gameAssetsPath,
                modNames);

            foreach (IAsset asset in ListWorldgenAssets(origin, logger))
            {
                if (!SchematicAssetPath.TryGetRelativePath(asset.Location.Path, out string relativePath))
                {
                    continue;
                }

                // Two origins overriding one another would otherwise list the same file twice inside
                // a single group, which reads as a duplicate variant.
                if (!seen.Add((structureOrigin, relativePath)))
                {
                    continue;
                }

                entries.Add(new SchematicScanEntry(
                    new ScannedSchematic(relativePath, structureOrigin),
                    new SchematicSource(origin, asset)));
            }
        }

        return entries;
    }

    /// <summary>
    /// Lists the worldgen assets of one origin without reading any of them. A broken origin is
    /// reported and skipped rather than taking the whole scan down.
    /// </summary>
    private static List<IAsset> ListWorldgenAssets(IAssetOrigin origin, ILogger logger)
    {
        try
        {
            return origin.GetAssets(AssetCategory.worldgen, shouldLoad: false);
        }
        catch (IOException exception)
        {
            logger.Warning("Could not list the worldgen assets of '{0}': {1}", origin.OriginPath, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.Warning("Could not list the worldgen assets of '{0}': {1}", origin.OriginPath, exception.Message);
        }

        return [];
    }
}
