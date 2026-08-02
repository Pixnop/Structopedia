using System;
using System.Collections.Generic;

namespace Structopedia.Catalog;

/// <summary>
/// Orders names the way a reader expects, so <c>ruin-2</c> comes before <c>ruin-10</c> instead of
/// after it. Digit runs compare as numbers, everything else compares ordinally.
/// </summary>
internal sealed class NaturalSortComparer : IComparer<string>
{
    /// <summary>The shared instance; the comparer holds no state.</summary>
    internal static NaturalSortComparer Instance { get; } = new NaturalSortComparer();

    /// <inheritdoc/>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        int i = 0;
        int j = 0;

        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                int startX = i;
                int startY = j;
                while (i < x.Length && char.IsDigit(x[i]))
                {
                    i++;
                }

                while (j < y.Length && char.IsDigit(y[j]))
                {
                    j++;
                }

                int compared = CompareDigitRuns(x.AsSpan(startX, i - startX), y.AsSpan(startY, j - startY));
                if (compared != 0)
                {
                    return compared;
                }

                continue;
            }

            if (x[i] != y[j])
            {
                return x[i] < y[j] ? -1 : 1;
            }

            i++;
            j++;
        }

        if (i < x.Length)
        {
            return 1;
        }

        if (j < y.Length)
        {
            return -1;
        }

        // Every run matched, which still allows different spellings such as "gear-01" and "gear-1".
        // Falling back to an ordinal compare keeps the order total, so sorting stays stable.
        return Math.Sign(string.CompareOrdinal(x, y));
    }

    /// <summary>
    /// Compares two digit runs as numbers without parsing them, so a run longer than any integer
    /// type still orders correctly.
    /// </summary>
    private static int CompareDigitRuns(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        ReadOnlySpan<char> trimmedLeft = left.TrimStart('0');
        ReadOnlySpan<char> trimmedRight = right.TrimStart('0');

        if (trimmedLeft.Length != trimmedRight.Length)
        {
            return trimmedLeft.Length < trimmedRight.Length ? -1 : 1;
        }

        return Math.Sign(trimmedLeft.SequenceCompareTo(trimmedRight));
    }
}
