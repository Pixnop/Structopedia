namespace Structopedia.Config;

/// <summary>
/// Settings the player can edit, loaded from the mod config file. Everything is optional: a file
/// missing a key, or holding a value that makes no sense, falls back to the default.
/// </summary>
internal sealed class StructopediaConfig
{
    /// <summary>Number of structure previews kept in memory when nothing else is configured.</summary>
    internal const int DefaultPreviewCacheSize = 4;

    /// <summary>Vertex budget of a single preview when nothing else is configured.</summary>
    internal const int DefaultMaxPreviewVertices = 3_000_000;

    /// <summary>Whether the catalog lists the story line structures, which spoil the story.</summary>
    public bool ShowStoryStructures { get; set; }

    /// <summary>How many built previews stay in the cache before the oldest is released.</summary>
    public int PreviewCacheSize { get; set; } = DefaultPreviewCacheSize;

    /// <summary>Vertex ceiling above which a structure is too heavy to preview.</summary>
    public int MaxPreviewVertices { get; set; } = DefaultMaxPreviewVertices;

    /// <summary>
    /// Returns a copy with every out of range value replaced by its default, leaving this instance
    /// untouched so the file the player wrote can still be reported as is.
    /// </summary>
    /// <returns>A usable config.</returns>
    internal StructopediaConfig Sanitized() => new()
    {
        ShowStoryStructures = ShowStoryStructures,
        PreviewCacheSize = PreviewCacheSize > 0 ? PreviewCacheSize : DefaultPreviewCacheSize,
        MaxPreviewVertices = MaxPreviewVertices > 0 ? MaxPreviewVertices : DefaultMaxPreviewVertices
    };
}
