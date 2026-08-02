using System.Collections.Generic;
using Structopedia.Preview;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Structopedia;

/// <summary>Client-only entry point for the Structopedia mod.</summary>
public sealed class StructopediaModSystem : ModSystem
{
    private readonly PreviewMeshStore previews = new PreviewMeshStore();

    private ICoreClientAPI? capi;
    private ModSystemSurvivalHandbook? handbook;

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

        previews.Dispose();
        capi = null;

        base.Dispose();
    }

    /// <summary>
    /// Adds the Structopedia pages to the handbook. Replayed on every handbook reload, so it has to
    /// stay idempotent.
    /// </summary>
    private void OnInitCustomPages(List<GuiHandbookPage> pages)
    {
        if (capi == null)
        {
            return;
        }

        // The catalog pages land in the next commit. The store already owns whatever GPU mesh they
        // will draw, and a reload has to let go of it: the assets behind it may have changed.
        previews.Clear();
    }
}
