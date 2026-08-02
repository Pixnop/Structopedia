using System;
using System.Collections.Generic;

namespace Structopedia.Catalog;

/// <summary>
/// Tells apart the build guides Structopedia writes itself from everything else it happens to ship.
/// A guide is a claim about how a machine has to be assembled for the game to accept it, so it is
/// labelled and listed differently from the structures the world generator drops.
/// </summary>
internal static class CuratedOrigins
{
    /// <summary>Folder below <c>worldgen/schematics</c> holding the guides, and nothing else.</summary>
    internal const string BuildsFolder = "builds";

    /// <summary>
    /// Relabels the schematics that are ours and sit in the builds folder.
    /// </summary>
    /// <param name="schematics">Everything the scan found, from every origin.</param>
    /// <param name="ownModName">
    /// Name Structopedia is loaded under, which is what the scan reports as the origin of its own
    /// assets. Blank when it could not be read, in which case nothing is relabelled: mislabelling
    /// another mod's folder would be worse than showing our own guides as ordinary mod content.
    /// </param>
    /// <returns>The same schematics, in the same order, some of them relabelled.</returns>
    internal static IReadOnlyList<ScannedSchematic> Apply(
        IEnumerable<ScannedSchematic> schematics,
        string ownModName)
    {
        ArgumentNullException.ThrowIfNull(schematics);
        ArgumentNullException.ThrowIfNull(ownModName);

        // One instance for every guide: the catalog groups by origin and compares origins by value,
        // but sharing it also keeps the whole set behind a single reference.
        var curated = new StructureOrigin(StructureOriginKind.Curated, ownModName);
        bool named = !string.IsNullOrWhiteSpace(ownModName);

        var result = new List<ScannedSchematic>();
        foreach (ScannedSchematic scanned in schematics)
        {
            result.Add(named && IsOurs(scanned.Origin, ownModName) && IsInBuildsFolder(scanned.RelativePath)
                ? scanned with { Origin = curated }
                : scanned);
        }

        return result;
    }

    /// <summary>
    /// Tells whether an origin is this very mod. Case insensitive, because the name travels through
    /// a folder name on its way back from the mod loader.
    /// </summary>
    private static bool IsOurs(StructureOrigin origin, string ownModName)
        => origin.Kind == StructureOriginKind.Mod
            && string.Equals(origin.DisplayName, ownModName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Tells whether a path sits below the builds folder. The separator is part of the test, so a
    /// <c>buildsite</c> folder is not swallowed and a file named <c>builds.json</c> is not either.
    /// </summary>
    private static bool IsInBuildsFolder(string relativePath)
    {
        // A scan run on Windows hands back backslashes, exactly as the catalog already expects.
        string path = relativePath.Replace('\\', '/');
        return path.Length > BuildsFolder.Length
            && path.StartsWith(BuildsFolder, StringComparison.Ordinal)
            && path[BuildsFolder.Length] == '/';
    }
}
