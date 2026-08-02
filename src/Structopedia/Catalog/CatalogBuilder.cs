using System;
using System.Collections.Generic;
using System.Text;

namespace Structopedia.Catalog;

/// <summary>
/// Folds a flat list of scanned schematic files into the grouped, ordered catalog the handbook shows.
/// </summary>
internal static class CatalogBuilder
{
    /// <summary>Title given to the group holding files that sit at the root of the schematics folder.</summary>
    internal const string MiscellaneousTitle = "Miscellaneous";

    /// <summary>First path segment marking the story line content.</summary>
    private const string StorySegment = "story";

    /// <summary>Separator placed between folder segments in a group title.</summary>
    private const string TitleSeparator = " / ";

    /// <summary>
    /// Groups files by folder and origin, then orders the result: ordinary structures first, story
    /// content last, each block sorted by title. Two origins sharing a folder name stay apart, since
    /// a mod adding its own <c>trader/cold</c> is a different set of structures.
    /// </summary>
    /// <param name="schematics">Files found while scanning the asset origins.</param>
    /// <returns>The catalog groups, ready to be listed.</returns>
    internal static IReadOnlyList<StructureGroup> Build(IEnumerable<ScannedSchematic> schematics)
    {
        ArgumentNullException.ThrowIfNull(schematics);

        var buckets = new Dictionary<(string Key, StructureOrigin Origin), List<StructureVariant>>();

        foreach (ScannedSchematic scanned in schematics)
        {
            // A scan run on Windows hands back backslashes; the catalog speaks in forward slashes only.
            string path = scanned.RelativePath.Replace('\\', '/');
            int lastSlash = path.LastIndexOf('/');
            string key = lastSlash < 0 ? string.Empty : path[..lastSlash];
            string fileName = lastSlash < 0 ? path : path[(lastSlash + 1)..];

            (string Key, StructureOrigin Origin) bucketKey = (key, scanned.Origin);
            if (!buckets.TryGetValue(bucketKey, out List<StructureVariant>? variants))
            {
                variants = [];
                buckets[bucketKey] = variants;
            }

            variants.Add(new StructureVariant(path, NameHumanizer.Humanize(fileName)));
        }

        var groups = new List<StructureGroup>(buckets.Count);
        foreach (KeyValuePair<(string Key, StructureOrigin Origin), List<StructureVariant>> bucket in buckets)
        {
            List<StructureVariant> variants = bucket.Value;
            variants.Sort(static (left, right) =>
                NaturalSortComparer.Instance.Compare(left.RelativePath, right.RelativePath));

            groups.Add(new StructureGroup(
                bucket.Key.Key,
                BuildTitle(bucket.Key.Key),
                bucket.Key.Origin,
                IsStory(bucket.Key.Key),
                variants));
        }

        groups.Sort(Compare);
        return groups;
    }

    /// <summary>
    /// Orders two groups. The build guides come first, since they answer a question a player is
    /// asking right now rather than showing what the generator might drop somewhere; story content
    /// sinks to the bottom; then titles decide. The remaining comparisons only exist so groups that
    /// look alike still come out in a fixed order.
    /// </summary>
    private static int Compare(StructureGroup left, StructureGroup right)
    {
        bool leftCurated = left.Origin.Kind == StructureOriginKind.Curated;
        bool rightCurated = right.Origin.Kind == StructureOriginKind.Curated;
        if (leftCurated != rightCurated)
        {
            return leftCurated ? -1 : 1;
        }

        if (left.IsStory != right.IsStory)
        {
            return left.IsStory ? 1 : -1;
        }

        int byTitle = NaturalSortComparer.Instance.Compare(left.Title, right.Title);
        if (byTitle != 0)
        {
            return byTitle;
        }

        int byKey = string.CompareOrdinal(left.Key, right.Key);
        if (byKey != 0)
        {
            return Math.Sign(byKey);
        }

        int byOriginName = string.CompareOrdinal(left.Origin.DisplayName, right.Origin.DisplayName);
        return byOriginName != 0 ? Math.Sign(byOriginName) : left.Origin.Kind.CompareTo(right.Origin.Kind);
    }

    /// <summary>Humanizes every folder segment and joins them, or names the root group.</summary>
    private static string BuildTitle(string key)
    {
        if (key.Length == 0)
        {
            return MiscellaneousTitle;
        }

        var builder = new StringBuilder(key.Length + 8);
        foreach (string segment in key.Split('/'))
        {
            string humanized = NameHumanizer.Humanize(segment);
            if (humanized.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(TitleSeparator);
            }

            builder.Append(humanized);
        }

        return builder.Length == 0 ? MiscellaneousTitle : builder.ToString();
    }

    /// <summary>
    /// Story content is recognised by its top folder only, so a <c>surface/story</c> folder from
    /// some mod is not mistaken for the story line.
    /// </summary>
    private static bool IsStory(string key)
    {
        int firstSlash = key.IndexOf('/', StringComparison.Ordinal);
        ReadOnlySpan<char> head = firstSlash < 0 ? key : key.AsSpan(0, firstSlash);
        return head.Equals(StorySegment, StringComparison.Ordinal);
    }
}
