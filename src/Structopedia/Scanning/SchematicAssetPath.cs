using System;

namespace Structopedia.Scanning;

/// <summary>
/// Recognises the asset paths that hold a worldgen schematic and turns them into the relative path
/// the catalog groups on.
/// </summary>
internal static class SchematicAssetPath
{
    /// <summary>Folder every worldgen schematic lives under, whichever domain ships it.</summary>
    internal const string Prefix = "worldgen/schematics/";

    /// <summary>Extension a schematic file wears.</summary>
    private const string Extension = ".json";

    /// <summary>
    /// Reads the path of an asset and keeps the part below <see cref="Prefix"/>.
    /// </summary>
    /// <param name="assetPath">Path of the asset inside its domain, without the domain prefix.</param>
    /// <param name="relativePath">
    /// Path below <c>worldgen/schematics/</c>, extension included; empty when the asset is not a
    /// schematic.
    /// </param>
    /// <returns>True when the asset is a schematic file.</returns>
    internal static bool TryGetRelativePath(string assetPath, out string relativePath)
    {
        ArgumentNullException.ThrowIfNull(assetPath);

        relativePath = string.Empty;

        if (!assetPath.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            || !assetPath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // A scan run on Windows can hand back backslashes; the catalog speaks in forward slashes only.
        string candidate = assetPath[Prefix.Length..].Replace('\\', '/');

        // The name itself has to carry something: the loader already skips dot files, and a bare
        // extension would end up as a group with no title.
        int lastSlash = candidate.LastIndexOf('/');
        if (candidate.Length - lastSlash - 1 <= Extension.Length)
        {
            return false;
        }

        relativePath = candidate;
        return true;
    }
}
