using System;
using System.Collections.Generic;
using Structopedia.Catalog;

namespace Structopedia.Scanning;

/// <summary>
/// Tells who owns an asset origin by looking at where it sits on disk: inside the game install, or
/// inside the folder of a loaded mod.
/// </summary>
internal static class OriginResolver
{
    /// <summary>Name shown for everything the base game ships.</summary>
    internal const string GameDisplayName = "Vintage Story";

    /// <summary>Name of last resort, when nothing about the path says who provided it.</summary>
    internal const string UnknownDisplayName = "Unknown";

    /// <summary>Folder every mod keeps its assets in, right below its own folder.</summary>
    private const string AssetsFolderName = "assets";

    /// <summary>Extension a packaged mod wears, still visible in the name of its unpacked folder.</summary>
    private const string ArchiveExtension = ".zip";

    /// <summary>
    /// Shortest hexadecimal run accepted as the fingerprint the loader appends to an unpacked mod
    /// folder. Below that a suffix is far more likely to be part of the name itself.
    /// </summary>
    private const int MinimumHashLength = 6;

    private static readonly StructureOrigin GameOrigin =
        new StructureOrigin(StructureOriginKind.Game, GameDisplayName);

    /// <summary>
    /// Attributes one asset origin.
    /// </summary>
    /// <param name="originPath">Folder the origin reads its files from.</param>
    /// <param name="gameAssetsPath">Root of the assets shipped with the game install.</param>
    /// <param name="modNamesByFolderPath">
    /// Name of every loaded mod, keyed by the folder holding it. A mod loaded from an archive is
    /// listed under the folder it was unpacked into, which is where its assets are read from.
    /// </param>
    /// <returns>Who provided the files of that origin.</returns>
    internal static StructureOrigin Resolve(
        string originPath,
        string gameAssetsPath,
        IReadOnlyDictionary<string, string> modNamesByFolderPath)
    {
        ArgumentNullException.ThrowIfNull(originPath);
        ArgumentNullException.ThrowIfNull(gameAssetsPath);
        ArgumentNullException.ThrowIfNull(modNamesByFolderPath);

        string path = Normalize(originPath);
        string gameRoot = Normalize(gameAssetsPath);

        // The game keeps every domain it ships below a single assets folder, mods never live there.
        if (gameRoot.Length > 0 && IsAtOrBelow(path, gameRoot))
        {
            return GameOrigin;
        }

        foreach (KeyValuePair<string, string> mod in modNamesByFolderPath)
        {
            if (mod.Key == null || string.IsNullOrWhiteSpace(mod.Value))
            {
                continue;
            }

            string folder = Normalize(mod.Key);
            if (folder.Length == 0)
            {
                continue;
            }

            if (path.Equals(folder + "/" + AssetsFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return new StructureOrigin(StructureOriginKind.Mod, mod.Value);
            }
        }

        // Either the mod list could not be read, or the origin was registered by hand by some mod.
        // The folder name is still a better label than nothing.
        return new StructureOrigin(StructureOriginKind.Mod, OwningFolderName(path));
    }

    /// <summary>Rewrites a path so two spellings of the same folder compare equal.</summary>
    private static string Normalize(string path)
        => path.Replace('\\', '/').TrimEnd('/');

    /// <summary>
    /// Tells whether a path is the root itself or sits below it. The separator is part of the test so
    /// a sibling folder whose name merely starts with the root name is not swallowed.
    /// </summary>
    private static bool IsAtOrBelow(string path, string root)
        => path.Equals(root, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Names the folder an origin belongs to: the one holding the <c>assets</c> folder, or the origin
    /// folder itself when the origin was pointed somewhere else.
    /// </summary>
    private static string OwningFolderName(string path)
    {
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return UnknownDisplayName;
        }

        string last = segments[^1];
        string owner = segments.Length > 1 && last.Equals(AssetsFolderName, StringComparison.OrdinalIgnoreCase)
            ? segments[^2]
            : last;

        string cleaned = StripArchiveExtension(StripUnpackHash(owner));
        return cleaned.Length == 0 ? UnknownDisplayName : cleaned;
    }

    /// <summary>
    /// Drops the fingerprint the mod loader appends when it unpacks an archive, so
    /// <c>mymod_ab12cd34ef56</c> reads as <c>mymod</c>.
    /// </summary>
    private static string StripUnpackHash(string name)
    {
        int separator = name.LastIndexOf('_');
        if (separator <= 0)
        {
            return name;
        }

        ReadOnlySpan<char> suffix = name.AsSpan(separator + 1);
        if (suffix.Length < MinimumHashLength)
        {
            return name;
        }

        foreach (char character in suffix)
        {
            if (!Uri.IsHexDigit(character))
            {
                return name;
            }
        }

        return name[..separator];
    }

    /// <summary>Drops the archive extension the unpacked folder inherited from the file name.</summary>
    private static string StripArchiveExtension(string name)
        => name.EndsWith(ArchiveExtension, StringComparison.OrdinalIgnoreCase)
            ? name[..^ArchiveExtension.Length]
            : name;
}
