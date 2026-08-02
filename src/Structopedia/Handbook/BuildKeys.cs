using System;

namespace Structopedia.Handbook;

/// <summary>
/// Names the translations a build guide can carry. A guide is one folder holding the variants of a
/// single machine, so the folder name is what a title and a description hang off: adding
/// <c>build-title-quern</c> and <c>build-desc-quern</c> to the lang files is all a new
/// <c>builds/quern</c> folder needs to read as more than a humanised path.
/// </summary>
internal static class BuildKeys
{
    /// <summary>Key naming the machine a guide builds, replacing the humanised folder name.</summary>
    private const string TitlePrefix = "structopedia:build-title-";

    /// <summary>Key holding the rules the guide follows, shown under the origin line.</summary>
    private const string DescriptionPrefix = "structopedia:build-desc-";

    /// <summary>Builds the title key of a group, or an empty string when it has no folder to name.</summary>
    /// <param name="groupKey">Folder path of the catalog group, for example <c>builds/bloomery</c>.</param>
    /// <returns>The lang key, empty when none applies.</returns>
    internal static string Title(string groupKey) => Key(TitlePrefix, groupKey);

    /// <summary>Builds the description key of a group, or an empty string when it has none.</summary>
    /// <param name="groupKey">Folder path of the catalog group, for example <c>builds/bloomery</c>.</param>
    /// <returns>The lang key, empty when none applies.</returns>
    internal static string Description(string groupKey) => Key(DescriptionPrefix, groupKey);

    private static string Key(string prefix, string groupKey)
    {
        ArgumentNullException.ThrowIfNull(groupKey);

        string segment = LastSegment(groupKey);
        return segment.Length == 0 ? string.Empty : prefix + segment;
    }

    /// <summary>
    /// Names the deepest folder of a path. A scan run on Windows hands back backslashes, and a path
    /// can end on a separator, so neither shape may decide what the folder is called.
    /// </summary>
    private static string LastSegment(string groupKey)
    {
        string path = groupKey.Replace('\\', '/').TrimEnd('/');
        int lastSlash = path.LastIndexOf('/');
        return lastSlash < 0 ? path : path[(lastSlash + 1)..];
    }
}
