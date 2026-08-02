using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Structopedia.Catalog;

namespace Structopedia.Handbook;

/// <summary>
/// Names the handbook pages of the catalog. A page code is an address: the handbook indexes pages by
/// it and <c>handbook://structopedia-trader-cold</c> links straight to one, so it has to stay short,
/// readable and unique.
/// </summary>
internal static class PageCodes
{
    /// <summary>Prefix worn by every page Structopedia adds, so its pages never clash with another mod's.</summary>
    internal const string Prefix = "structopedia-";

    /// <summary>Name given to the group holding the files that sit at the root of the schematics folder.</summary>
    private const string RootSlug = "misc";

    /// <summary>Character joining the parts of a code, and standing in for anything unusable.</summary>
    private const char Separator = '-';

    /// <summary>
    /// Builds one code per group, in the order the groups come in, making sure no two are alike.
    /// </summary>
    /// <param name="groups">Catalog groups.</param>
    /// <returns>The page codes, aligned with <paramref name="groups"/>.</returns>
    internal static IReadOnlyList<string> Assign(IReadOnlyList<StructureGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var taken = new HashSet<string>(StringComparer.Ordinal);
        var codes = new List<string>(groups.Count);

        foreach (StructureGroup group in groups)
        {
            string baseCode = Build(group);
            string code = baseCode;

            // Two mods can be named alike once their names are slugged. Numbering keeps the codes
            // apart, which matters because the handbook indexes pages by code and would lose one.
            for (int attempt = 2; !taken.Add(code); attempt++)
            {
                code = baseCode + Separator + attempt.ToString(CultureInfo.InvariantCulture);
            }

            codes.Add(code);
        }

        return codes;
    }

    /// <summary>
    /// Builds the code of a single group, before any numbering.
    /// </summary>
    private static string Build(StructureGroup group)
    {
        string slug = Slugify(group.Key);
        if (slug.Length == 0)
        {
            slug = RootSlug;
        }

        if (group.Origin.Kind != StructureOriginKind.Game)
        {
            string originSlug = Slugify(group.Origin.DisplayName);
            if (originSlug.Length > 0)
            {
                slug = slug + Separator + originSlug;
            }
        }

        return Prefix + slug;
    }

    /// <summary>
    /// Keeps the letters and digits of a name and turns everything else into a separator, so a code
    /// survives being written in a link or a config file.
    /// </summary>
    private static string Slugify(string raw)
    {
        var builder = new StringBuilder(raw.Length + 4);
        bool pendingSeparator = false;

        foreach (char current in raw)
        {
            if (char.IsAsciiLetterOrDigit(current))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append(Separator);
                }

                pendingSeparator = false;
                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            // Held back rather than written, so runs collapse and a trailing one leaves nothing behind.
            pendingSeparator = true;
        }

        return builder.ToString();
    }
}
