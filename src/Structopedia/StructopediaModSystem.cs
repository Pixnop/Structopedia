using System.Collections.Generic;
using Structopedia.Spike;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Structopedia;

/// <summary>Client-only entry point for the Structopedia mod.</summary>
public sealed class StructopediaModSystem : ModSystem
{
    private ICoreClientAPI? capi;
    private ModSystemSurvivalHandbook? handbook;
    private SpikePreviewMesh? spikeMesh;
    private bool spikeMeshBuildAttempted;

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

        spikeMesh?.Dispose();
        spikeMesh = null;
        spikeMeshBuildAttempted = false;
        capi = null;

        base.Dispose();
    }

    /// <summary>
    /// Builds and uploads the spike schematic mesh on first use, then hands out the shared handle.
    /// Must be called from the main thread, from inside a render frame.
    /// </summary>
    /// <returns>The shared mesh, or null when the schematic could not be built.</returns>
    internal SpikePreviewMesh? GetOrCreateSpikeMesh()
    {
        if (spikeMesh != null || spikeMeshBuildAttempted || capi == null)
        {
            return spikeMesh;
        }

        spikeMeshBuildAttempted = true;

        SpikeMeshBuildResult? build = SpikeMeshBuilder.Build(capi, Mod.Logger);
        if (build == null)
        {
            return null;
        }

        Mod.Logger.Notification(
            "Spike schematic {0}x{1}x{2}: {3} blocks meshed, {4} filtered, {5} unknown, {6} empty; {7} vertices, {8} indices.",
            build.SizeX,
            build.SizeY,
            build.SizeZ,
            build.MergedCount,
            build.FilteredCount,
            build.UnknownCount,
            build.EmptyMeshCount,
            build.Mesh.VerticesCount,
            build.Mesh.IndicesCount);

        if (build.Mesh.VerticesCount == 0)
        {
            Mod.Logger.Warning("Spike schematic produced an empty mesh, nothing will be drawn.");
            return null;
        }

        spikeMesh = new SpikePreviewMesh(
            capi.Render.UploadMultiTextureMesh(build.Mesh),
            build.SizeX,
            build.SizeY,
            build.SizeZ);

        return spikeMesh;
    }

    /// <summary>
    /// Adds the spike page to the handbook. Replayed on every handbook reload, so it must stay
    /// idempotent: a fresh page is created each time, the GPU mesh stays shared.
    /// </summary>
    private void OnInitCustomPages(List<GuiHandbookPage> pages)
    {
        if (capi == null)
        {
            return;
        }

        pages.Add(new SpikeStructurePage(capi, this));
    }
}
