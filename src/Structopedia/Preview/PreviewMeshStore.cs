using System;
using System.Globalization;
using Structopedia.Caching;

namespace Structopedia.Preview;

/// <summary>
/// Holds the structure previews that have been built, and releases the ones that fall out of the
/// cache.
/// <para>
/// A handful of entries is what makes stepping through the variants of a page, or back to the page
/// before, instant rather than a rebuild every time. How many is a setting, since every entry costs
/// graphics memory for as long as it is held.
/// </para>
/// <para>
/// Not thread safe by design: every call has to come from the main thread, which is the only one
/// allowed to touch a GPU resource.
/// </para>
/// </summary>
internal sealed class PreviewMeshStore : IDisposable
{
    private readonly LruCache<string, PreviewEntry?> entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewMeshStore"/> class, empty.
    /// </summary>
    /// <param name="capacity">How many previews are held at once, at least one.</param>
    internal PreviewMeshStore(int capacity)
    {
        entries = new LruCache<string, PreviewEntry?>(capacity, Release);
    }

    /// <inheritdoc/>
    public void Dispose() => Clear();

    /// <summary>
    /// Hands out the preview of one variant, building it on the first call. A build that fails is
    /// remembered as a failure, so a structure that cannot be meshed is not attempted again on every
    /// frame.
    /// </summary>
    /// <param name="pageCode">Page the variant belongs to.</param>
    /// <param name="variantIndex">Variant shown by that page.</param>
    /// <param name="build">Builds and uploads the preview. Only called on a miss.</param>
    /// <returns>The preview, or null when it could not be built.</returns>
    internal PreviewEntry? GetOrBuild(string pageCode, int variantIndex, Func<PreviewEntry?> build)
    {
        ArgumentNullException.ThrowIfNull(pageCode);
        ArgumentNullException.ThrowIfNull(build);

        string key = KeyOf(pageCode, variantIndex);
        if (entries.TryGet(key, out PreviewEntry? cached))
        {
            return cached;
        }

        PreviewEntry? built = build();
        entries.Set(key, built);
        return built;
    }

    /// <summary>
    /// Drops everything held, so the next request rebuilds. Used when the handbook reloads, since the
    /// assets behind a preview may have changed under it.
    /// </summary>
    internal void Clear() => entries.Clear();

    private static string KeyOf(string pageCode, int variantIndex)
        => pageCode + "#" + variantIndex.ToString(CultureInfo.InvariantCulture);

    private static void Release(PreviewEntry? entry) => entry?.Dispose();
}
