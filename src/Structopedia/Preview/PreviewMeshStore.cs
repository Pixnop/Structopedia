using System;

namespace Structopedia.Preview;

/// <summary>
/// Holds the preview mesh currently on screen and releases the one before it.
/// <para>
/// One mesh at a time is all this pass needs: the handbook only ever draws the page a player is
/// looking at. Keeping several around, so stepping back to the previous variant is instant, is what
/// <see cref="Caching.LruCache{TKey, TValue}"/> is there for and is left to the optimisation pass.
/// </para>
/// <para>
/// Not thread safe by design: every call has to come from the main thread, which is the only one
/// allowed to touch a GPU resource.
/// </para>
/// </summary>
internal sealed class PreviewMeshStore : IDisposable
{
    private (string PageCode, int VariantIndex) _key;
    private PreviewMesh? _mesh;
    private bool _built;

    /// <inheritdoc/>
    public void Dispose() => Clear();

    /// <summary>
    /// Hands out the mesh of one variant, building it on the first call. A build that fails is
    /// remembered, so a structure that cannot be meshed is not attempted again on every frame.
    /// </summary>
    /// <param name="pageCode">Page the variant belongs to.</param>
    /// <param name="variantIndex">Variant shown by that page.</param>
    /// <param name="build">Builds and uploads the mesh. Only called on a miss.</param>
    /// <returns>The mesh, or null when it could not be built.</returns>
    internal PreviewMesh? GetOrBuild(string pageCode, int variantIndex, Func<PreviewMesh?> build)
    {
        ArgumentNullException.ThrowIfNull(pageCode);
        ArgumentNullException.ThrowIfNull(build);

        (string PageCode, int VariantIndex) requested = (pageCode, variantIndex);
        if (_built && _key == requested)
        {
            return _mesh;
        }

        Release();
        _key = requested;
        _built = true;
        _mesh = build();
        return _mesh;
    }

    /// <summary>
    /// Drops what is held, so the next request rebuilds. Used when the handbook reloads, since the
    /// assets behind a mesh may have changed under it.
    /// </summary>
    internal void Clear()
    {
        Release();
        _key = default;
        _built = false;
    }

    private void Release()
    {
        _mesh?.Dispose();
        _mesh = null;
    }
}
