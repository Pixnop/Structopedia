using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Structopedia.Scanning;

/// <summary>
/// Maps the folder of every loaded mod to the name that mod goes by, which is what turns an asset
/// origin path into a readable source line.
/// </summary>
internal static class ModFolderIndex
{
    /// <summary>
    /// Reads the loaded mod list.
    /// <para>
    /// The folder of a mod is only exposed by the concrete container the game loader builds, not by
    /// the <see cref="Mod"/> contract itself, so the cast is allowed to fail: the scan then falls
    /// back to naming a mod after its folder.
    /// </para>
    /// </summary>
    /// <param name="mods">Loaded mods, as reported by the mod loader.</param>
    /// <returns>Mod names keyed by the folder each mod was loaded from.</returns>
    internal static IReadOnlyDictionary<string, string> Build(IEnumerable<Mod> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);

        var byFolder = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Mod mod in mods)
        {
            if (mod is not ModContainer container || string.IsNullOrEmpty(container.FolderPath))
            {
                continue;
            }

            string? name = container.Info?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = container.Info?.ModID;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                byFolder[container.FolderPath] = name;
            }
        }

        return byFolder;
    }
}
