using System;
using System.IO;
using Vintagestory.API.Common;

namespace Structopedia.Scanning;

/// <summary>
/// Where one schematic file can be read from. The scan keeps the handle rather than the contents, so
/// a file is only read when a player actually opens the page showing it.
/// </summary>
/// <param name="Origin">Asset origin the file belongs to, which is what loads it on demand.</param>
/// <param name="Asset">
/// Asset handle produced by the scan, still empty. Loading fills it in place, so the same instance is
/// reused every time.
/// </param>
internal sealed record SchematicSource(IAssetOrigin Origin, IAsset Asset)
{
    /// <summary>Location of the file, useful when reporting a failure.</summary>
    internal AssetLocation Location => Asset.Location;

    /// <summary>
    /// Reads the file from disk and hands back its text.
    /// </summary>
    /// <returns>The file contents, or null when the file could not be read.</returns>
    internal string? TryReadText()
    {
        try
        {
            if (!Origin.TryLoadAsset(Asset))
            {
                return null;
            }

            return Asset.ToText();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            // The text is all we needed: holding the bytes would keep a copy of every schematic the
            // player ever opened alive for the rest of the session.
            Asset.Data = null;
        }
    }
}
