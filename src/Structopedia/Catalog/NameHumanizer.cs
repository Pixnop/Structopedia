using System;
using System.Text;

namespace Structopedia.Catalog;

/// <summary>
/// Turns an asset name into something readable: <c>vug-medium1</c> becomes <c>Vug medium 1</c>.
/// Deterministic and culture independent, so the same file always yields the same label.
/// </summary>
internal static class NameHumanizer
{
    /// <summary>Extension worn by every schematic file, dropped before the name is rewritten.</summary>
    private const string JsonExtension = ".json";

    /// <summary>Rewrites one asset name.</summary>
    /// <param name="raw">File or folder name, with or without a <c>.json</c> extension.</param>
    /// <returns>The readable form, empty when the name carries no word at all.</returns>
    internal static string Humanize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        string name = raw.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase)
            ? raw[..^JsonExtension.Length]
            : raw;

        var builder = new StringBuilder(name.Length + 8);
        bool pendingSpace = false;
        char previous = '\0';

        foreach (char current in name)
        {
            if (IsSeparator(current))
            {
                // Held back rather than written, so runs of separators collapse and a trailing one
                // never leaves a dangling space.
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (builder.Length > 0 && (pendingSpace || IsWordBoundary(previous, current)))
            {
                builder.Append(' ');
            }

            pendingSpace = false;
            builder.Append(current);
            previous = current;
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        builder[0] = char.ToUpperInvariant(builder[0]);
        return builder.ToString();
    }

    private static bool IsSeparator(char value)
        => value is '-' or '_' or '.' || char.IsWhiteSpace(value);

    /// <summary>
    /// Names glue a number straight onto a word (<c>medium1</c>, <c>h10</c>), so a letter meeting a
    /// digit reads as a word break.
    /// </summary>
    private static bool IsWordBoundary(char previous, char current)
        => (char.IsLetter(previous) && char.IsDigit(current))
            || (char.IsDigit(previous) && char.IsLetter(current));
}
