using System;
using System.Collections.Generic;
using System.IO;
using Structopedia.Catalog;
using Structopedia.Config;
using Structopedia.Handbook;
using Structopedia.Preview;
using Structopedia.Scanning;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Structopedia;

/// <summary>Client-only entry point for the Structopedia mod.</summary>
public sealed class StructopediaModSystem : ModSystem
{
    /// <summary>Name of the settings file, written below the ModConfig folder.</summary>
    private const string ConfigFileName = "structopedia.json";

    private ICoreClientAPI? capi;
    private ModSystemSurvivalHandbook? handbook;

    /// <summary>
    /// Owns the built previews. Created with the catalog, since how many it holds is a setting and
    /// the settings are only read once the client is up.
    /// </summary>
    private PreviewMeshStore? previews;

    /// <summary>
    /// The catalog pages, built once and handed to the handbook again on every reload. See
    /// <see cref="OnInitCustomPages"/> for why they are kept rather than rebuilt.
    /// </summary>
    private List<StructureGroupPage>? pages;

    /// <inheritdoc/>
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    /// <inheritdoc/>
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        capi = api;

        handbook = api.ModLoader.GetModSystem<ModSystemSurvivalHandbook>();
        if (handbook == null)
        {
            Mod.Logger.Warning("Survival handbook mod system not found, no page will be registered.");
        }
        else
        {
            handbook.OnInitCustomPages += OnInitCustomPages;
        }

        Mod.Logger.Notification("{0} {1} loaded.", Mod.Info.Name, Mod.Info.Version);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (handbook != null)
        {
            handbook.OnInitCustomPages -= OnInitCustomPages;
            handbook = null;
        }

        previews?.Dispose();
        previews = null;

        if (pages != null)
        {
            foreach (StructureGroupPage page in pages)
            {
                page.Dispose();
            }

            pages = null;
        }

        capi = null;

        base.Dispose();
    }

    /// <summary>
    /// Hands the catalog pages to the handbook. Replayed on every handbook reload, so it has to stay
    /// idempotent.
    /// </summary>
    /// <remarks>
    /// The same page instances are handed over every time. The handbook empties its page list on a
    /// reload without disposing what was in it, so building a new generation would leak the list
    /// entry texture of the old one, and the browse history can still point at a page that is no
    /// longer listed. What a reload can invalidate is held elsewhere and dropped here: the GPU mesh
    /// lives in the store, the parsed schematics live in the pages and both are let go of before the
    /// pages are listed again.
    /// </remarks>
    private void OnInitCustomPages(List<GuiHandbookPage> handbookPages)
    {
        if (capi == null)
        {
            return;
        }

        pages ??= BuildPages(capi);
        previews?.Clear();

        foreach (StructureGroupPage page in pages)
        {
            page.ResetCaches();
            handbookPages.Add(page);
        }
    }

    /// <summary>
    /// Scans the asset origins and turns what it finds into one page per structure group.
    /// </summary>
    private List<StructureGroupPage> BuildPages(ICoreClientAPI api)
    {
        StructopediaConfig config = LoadConfig(api);
        previews = new PreviewMeshStore(config.PreviewCacheSize);

        IReadOnlyList<SchematicScanEntry> entries = SchematicScanner.Scan(api, Mod.Logger);

        var sourcesByVariant = new Dictionary<(StructureOrigin Origin, string RelativePath), SchematicSource>();
        var scanned = new List<ScannedSchematic>(entries.Count);
        var origins = new HashSet<StructureOrigin>();

        foreach (SchematicScanEntry entry in entries)
        {
            scanned.Add(entry.Schematic);
            sourcesByVariant[(entry.Schematic.Origin, entry.Schematic.RelativePath)] = entry.Source;
            origins.Add(entry.Schematic.Origin);
        }

        IReadOnlyList<StructureGroup> groups = CatalogBuilder.Build(scanned);

        var listed = new List<StructureGroup>(groups.Count);
        foreach (StructureGroup group in groups)
        {
            // Story content spoils the story line, so it stays out unless the player asks for it.
            if (config.ShowStoryStructures || !group.IsStory)
            {
                listed.Add(group);
            }
        }

        IReadOnlyList<string> codes = PageCodes.Assign(listed);
        var built = new List<StructureGroupPage>(listed.Count);

        for (int i = 0; i < listed.Count; i++)
        {
            StructureGroup group = listed[i];
            var sources = new List<SchematicSource>(group.Variants.Count);

            foreach (StructureVariant variant in group.Variants)
            {
                if (sourcesByVariant.TryGetValue((group.Origin, variant.RelativePath), out SchematicSource? source))
                {
                    sources.Add(source);
                }
            }

            if (sources.Count != group.Variants.Count)
            {
                // Every variant came out of the scan, so this cannot happen; a page whose variants and
                // sources disagree would show the wrong structure, which is worse than showing none.
                Mod.Logger.Warning("Structure group '{0}' lost track of its files and was left out.", group.Key);
                continue;
            }

            built.Add(new StructureGroupPage(
                api,
                group,
                sources,
                previews,
                codes[i],
                config.MaxPreviewVertices,
                Mod.Logger));
        }

        Mod.Logger.Notification(
            "Found {0} schematics across {1} origins, listing {2} of {3} structure groups.",
            entries.Count,
            origins.Count,
            built.Count,
            groups.Count);

        return built;
    }

    /// <summary>
    /// Reads the settings file, writing it with the defaults when it is not there yet. A file that
    /// cannot be read is reported and ignored rather than taking the mod down with it.
    /// </summary>
    private StructopediaConfig LoadConfig(ICoreClientAPI api)
    {
        StructopediaConfig? stored = null;

        try
        {
            stored = api.LoadModConfig<StructopediaConfig>(ConfigFileName);
        }
        catch (Exception exception)
        {
            // The json reader raises its own exception types, which live in a library the mod does
            // not reference, so there is nothing narrower to catch here.
            Mod.Logger.Warning("Could not read '{0}', falling back to the defaults: {1}", ConfigFileName, exception.Message);
        }

        if (stored == null)
        {
            stored = new StructopediaConfig();

            try
            {
                api.StoreModConfig(stored, ConfigFileName);
            }
            catch (IOException exception)
            {
                Mod.Logger.Warning("Could not write '{0}': {1}", ConfigFileName, exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Mod.Logger.Warning("Could not write '{0}': {1}", ConfigFileName, exception.Message);
            }
        }

        return stored.Sanitized();
    }
}
