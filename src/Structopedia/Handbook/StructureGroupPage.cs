using System;
using System.Collections.Generic;
using System.Text;
using Structopedia.Catalog;
using Structopedia.Preview;
using Structopedia.Scanning;
using Structopedia.Schematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Structopedia.Handbook;

/// <summary>
/// Handbook page of one structure group: every schematic of a single folder, from a single origin,
/// with a 3D preview and the block list of whichever variant is being shown.
/// </summary>
internal sealed class StructureGroupPage : GuiHandbookPage
{
    /// <summary>Category code of the page, which becomes a handbook tab of its own.</summary>
    internal const string CategoryCodeValue = "structures";

    /// <summary>How many block entries the list shows before it stops and counts the rest.</summary>
    private const int MaxBlockRows = 40;

    /// <summary>Unscaled size of a block icon in the block list.</summary>
    private const double BlockIconSize = 40.0;

    private readonly ICoreClientAPI capi;
    private readonly StructureGroup group;
    private readonly IReadOnlyList<SchematicSource> sources;
    private readonly PreviewMeshStore previews;
    private readonly ILogger logger;
    private readonly int maxPreviewVertices;
    private readonly string pageCode;
    private readonly string listTitle;
    private readonly string searchTitle;
    private readonly string searchText;

    /// <summary>Parsed schematics, one slot per variant, filled the first time a variant is opened.</summary>
    private readonly BlockSchematic?[] parsed;

    /// <summary>Tells apart a variant never opened from one whose file could not be read.</summary>
    private readonly bool[] parseAttempted;

    private LoadedTexture? listEntryTexture;
    private int variantIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureGroupPage"/> class.
    /// </summary>
    /// <param name="capi">Client API.</param>
    /// <param name="group">Catalog group this page stands for.</param>
    /// <param name="sources">Where each variant can be read from, aligned with the group variants.</param>
    /// <param name="previews">Store owning the GPU mesh of the preview.</param>
    /// <param name="pageCode">Unique page code, assigned by <see cref="PageCodes"/>.</param>
    /// <param name="maxPreviewVertices">Vertex budget of a single preview.</param>
    /// <param name="logger">Logger used to report a file that could not be read.</param>
    internal StructureGroupPage(
        ICoreClientAPI capi,
        StructureGroup group,
        IReadOnlyList<SchematicSource> sources,
        PreviewMeshStore previews,
        string pageCode,
        int maxPreviewVertices,
        ILogger logger)
    {
        this.capi = capi;
        this.group = group;
        this.sources = sources;
        this.previews = previews;
        this.pageCode = pageCode;
        this.maxPreviewVertices = maxPreviewVertices;
        this.logger = logger;

        parsed = new BlockSchematic?[group.Variants.Count];
        parseAttempted = new bool[group.Variants.Count];

        listTitle = group.Origin.Kind == StructureOriginKind.Game
            ? group.Title
            : group.Title + " (" + group.Origin.DisplayName + ")";

        searchTitle = StringUtil.ToSearchFriendly(listTitle);
        searchText = StringUtil.ToSearchFriendly(BuildSearchText());
    }

    /// <inheritdoc/>
    public override string PageCode => pageCode;

    /// <inheritdoc/>
    public override string CategoryCode => CategoryCodeValue;

    /// <inheritdoc/>
    public override bool IsDuplicate => false;

    /// <inheritdoc/>
    public override float SearchWeightOffset => 0f;

    /// <inheritdoc/>
    public override PageText GetPageText() => new PageText
    {
        Title = searchTitle,
        Text = searchText
    };

    /// <inheritdoc/>
    public override void ComposePage(
        GuiComposer detailViewGui,
        ElementBounds textBounds,
        ItemStack[] allstacks,
        ActionConsumable<string> openDetailPageFor)
    {
        var components = new List<RichTextComponentBase>();

        components.AddRange(VtmlUtil.Richtextify(
            capi,
            "<strong>" + listTitle + "</strong>\n",
            CairoFont.WhiteSmallishText()));
        AddLine(components, OriginLine(), CairoFont.WhiteDetailText());

        AddVariantNavigation(components, openDetailPageFor);

        BlockSchematic? schematic = LoadCurrentVariant();
        if (schematic == null)
        {
            AddLine(components, Lang.Get("structopedia:load-failed"), CairoFont.WhiteSmallText());
        }
        else
        {
            AddLine(
                components,
                Lang.Get("structopedia:dimensions", schematic.SizeX, schematic.SizeY, schematic.SizeZ),
                CairoFont.WhiteSmallText());
            AddPreview(components, schematic);
            AddBlockList(components, schematic, openDetailPageFor);
        }

        // Exactly one richtext element named "richtext", added last: the detail page wires its scroll
        // bar on that name and sizes it from the last element it composed.
        detailViewGui.AddRichtext(components.ToArray(), textBounds, "richtext");
    }

    /// <inheritdoc/>
    public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
    {
        // Lazy texture, same pattern as GuiHandbookTextPage: the flat list only renders visible cells.
        float rowHeight = (float)GuiElement.scaled(25.0);
        float padLeft = (float)GuiElement.scaled(10.0);

        if (listEntryTexture == null)
        {
            Recompose(capi);
        }

        LoadedTexture? texture = listEntryTexture;
        if (texture == null)
        {
            return;
        }

        capi.Render.Render2DTexturePremultipliedAlpha(
            texture.TextureId,
            x + padLeft,
            y + (rowHeight / 4f) - GuiElement.scaled(3.0),
            texture.Width,
            texture.Height,
            50f);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        listEntryTexture?.Dispose();
        listEntryTexture = null;
    }

    /// <summary>
    /// Lets go of every parsed schematic. Called when the handbook reloads, since the files behind
    /// them may have been edited in the meantime.
    /// </summary>
    internal void ResetCaches()
    {
        Array.Clear(parsed);
        Array.Clear(parseAttempted);
        variantIndex = 0;
    }

    private void AddLine(List<RichTextComponentBase> components, string text, CairoFont font)
        => components.Add(new RichTextComponent(capi, text + "\n", font));

    private string BuildSearchText()
    {
        var builder = new StringBuilder();
        foreach (StructureVariant variant in group.Variants)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(variant.Title);
        }

        return builder.ToString();
    }

    private string OriginLine()
        => group.Origin.Kind == StructureOriginKind.Game
            ? Lang.Get("structopedia:source-game")
            : Lang.Get("structopedia:source-mod", group.Origin.DisplayName);

    private void Recompose(ICoreClientAPI api)
    {
        listEntryTexture?.Dispose();
        listEntryTexture = new TextTextureUtil(api).GenTextTexture(listTitle, CairoFont.WhiteSmallText());
    }

    private void AddVariantNavigation(List<RichTextComponentBase> components, ActionConsumable<string> openDetailPageFor)
    {
        int count = group.Variants.Count;
        if (count <= 1)
        {
            if (count == 1)
            {
                AddLine(components, group.Variants[0].Title, CairoFont.WhiteSmallText());
            }

            return;
        }

        components.Add(new LinkTextComponent(
            capi,
            "< ",
            CairoFont.WhiteSmallText(),
            _ => ShowVariant(variantIndex - 1, openDetailPageFor)));
        components.Add(new RichTextComponent(
            capi,
            Lang.Get("structopedia:variant", variantIndex + 1, count, group.Variants[variantIndex].Title) + " ",
            CairoFont.WhiteSmallText()));
        components.Add(new LinkTextComponent(
            capi,
            ">",
            CairoFont.WhiteSmallText(),
            _ => ShowVariant(variantIndex + 1, openDetailPageFor)));

        // Ends the line: a rich text component holding nothing but a newline is not a shape the text
        // flow expects, this is how the vanilla pages break out of one.
        components.Add(new ClearFloatTextComponent(capi, 4f));
    }

    /// <summary>
    /// Moves to another variant and redraws the page.
    /// </summary>
    /// <remarks>
    /// Opening the page by its own code does nothing: <c>GuiDialogHandbook.OpenDetailPageFor</c>
    /// returns early when the page asked for is already the one on top of the browse history. The
    /// dialog is asked to recompose instead, which keeps the scroll position and adds no history
    /// entry. Opening by code stays as the fallback for a dialog we cannot find.
    /// </remarks>
    private void ShowVariant(int index, ActionConsumable<string> openDetailPageFor)
    {
        int count = group.Variants.Count;
        variantIndex = ((index % count) + count) % count;

        foreach (object dialog in capi.OpenedGuis)
        {
            if (dialog is GuiDialogHandbook handbook)
            {
                handbook.ReloadPage();
                return;
            }
        }

        openDetailPageFor(pageCode);
    }

    /// <summary>
    /// Reads the schematic of the variant on screen, once per variant and per handbook reload.
    /// </summary>
    /// <returns>The parsed schematic, or null when the file could not be read.</returns>
    private BlockSchematic? LoadCurrentVariant()
    {
        if (variantIndex < 0 || variantIndex >= sources.Count)
        {
            return null;
        }

        if (parseAttempted[variantIndex])
        {
            return parsed[variantIndex];
        }

        parseAttempted[variantIndex] = true;

        SchematicSource source = sources[variantIndex];
        string? text = source.TryReadText();
        if (text == null)
        {
            logger.Warning("Schematic '{0}' could not be read from its origin.", source.Location);
            return null;
        }

        string error = string.Empty;
        BlockSchematic? schematic = BlockSchematic.LoadFromString(text, ref error);
        if (schematic == null)
        {
            logger.Warning("Schematic '{0}' failed to parse: {1}", source.Location, error);
            return null;
        }

        parsed[variantIndex] = schematic;
        return schematic;
    }

    private void AddPreview(List<RichTextComponentBase> components, BlockSchematic schematic)
    {
        int index = variantIndex;
        PreviewEntry? preview = previews.GetOrBuild(pageCode, index, () => BuildPreview(schematic));

        if (preview == null)
        {
            AddLine(components, Lang.Get("structopedia:preview-unavailable"), CairoFont.WhiteDetailText());
            return;
        }

        components.Add(new ClearFloatTextComponent(capi, 8f));

        // A fresh component on every compose, since the composer owns and disposes the previous one.
        // It asks the store on every frame rather than holding the preview, so one released while the
        // page is open is never drawn from.
        components.Add(new StructurePreviewComponent(
            capi,
            () => previews.GetOrBuild(pageCode, index, () => BuildPreview(schematic))));

        components.Add(new ClearFloatTextComponent(capi, 8f));
        AddLine(components, Lang.Get("structopedia:preview-controls"), CairoFont.WhiteDetailText());

        if (preview.Truncated)
        {
            // Same number the slider label shows at the top of its travel: the layers the budget
            // never reached are not part of its range either.
            int topLayer = preview.MaxLayer - preview.MinLayer + 1;

            AddLine(
                components,
                Lang.Get("structopedia:preview-truncated", maxPreviewVertices, topLayer),
                CairoFont.WhiteDetailText());
        }
    }

    /// <summary>
    /// Builds and uploads the layers of the variant on screen. Runs on the main thread, from the page
    /// compose, which is where the tesselator and the block atlas can be touched.
    /// </summary>
    private PreviewEntry? BuildPreview(BlockSchematic schematic)
    {
        MeshBuildResult build = SchematicMeshBuilder.Build(capi, schematic, maxPreviewVertices);

        logger.VerboseDebug(
            "{0}: {1} blocks meshed over {2} layers, {3} chiselled; skipped {4} filtered, {5} unknown, "
                + "{6} without geometry, {7} chiselled without usable data, {8} shaped by a block entity; "
                + "{9} vertices{10}.",
            pageCode,
            build.MergedCount,
            build.Layers.Count,
            build.ChiseledCount,
            build.FilteredCount,
            build.UnknownCount,
            build.EmptyMeshCount,
            build.ChiseledFallbackCount,
            build.ClutterSkippedCount,
            build.VerticesCount,
            build.Truncated ? ", truncated at layer " + build.TruncatedAtLayer : string.Empty);

        if (build.Layers.Count == 0)
        {
            return null;
        }

        var uploaded = new List<PreviewLayer>(build.Layers.Count);
        foreach (LayerMesh layer in build.Layers)
        {
            uploaded.Add(new PreviewLayer(layer.Y, capi.Render.UploadMultiTextureMesh(layer.Mesh)));
        }

        return new PreviewEntry(uploaded, build);
    }

    private void AddBlockList(
        List<RichTextComponentBase> components,
        BlockSchematic schematic,
        ActionConsumable<string> openDetailPageFor)
    {
        TallyResult tally = BlockTally.Count(SchematicCellReader.ReadCells(schematic));
        if (tally.Blocks.Count == 0)
        {
            return;
        }

        components.Add(new ClearFloatTextComponent(capi, 14f));
        components.AddRange(VtmlUtil.Richtextify(
            capi,
            "<strong>" + Lang.Get("structopedia:blocks-used") + "</strong>\n",
            CairoFont.WhiteSmallishText()));

        int shown = Math.Min(MaxBlockRows, tally.Blocks.Count);
        var missing = new List<(AssetLocation Code, int Count)>();

        for (int i = 0; i < shown; i++)
        {
            (AssetLocation code, int count) = tally.Blocks[i];

            Block? block = capi.World.GetBlock(code);
            if (block == null)
            {
                // The schematic names a block this install does not have, which happens as soon as a
                // mod ships structures made of its own blocks and is only half installed.
                missing.Add((code, count));
                continue;
            }

            components.Add(new ItemstackTextComponent(
                capi,
                new ItemStack(block),
                BlockIconSize,
                0.0,
                EnumFloat.Inline,
                stack => openDetailPageFor(PageCodeForStack(stack))));
            components.Add(new RichTextComponent(
                capi,
                Lang.Get("structopedia:block-count", count) + "  ",
                CairoFont.WhiteSmallText()));
        }

        if (missing.Count > 0)
        {
            components.Add(new ClearFloatTextComponent(capi, 8f));
            foreach ((AssetLocation code, int count) in missing)
            {
                AddLine(
                    components,
                    Lang.Get("structopedia:unknown-block", code.ToShortString(), count),
                    CairoFont.WhiteDetailText());
            }
        }

        int remaining = tally.Blocks.Count - shown;
        if (remaining > 0)
        {
            components.Add(new ClearFloatTextComponent(capi, 8f));
            AddLine(components, Lang.Get("structopedia:more-blocks", remaining), CairoFont.WhiteDetailText());
        }
    }

    /// <summary>
    /// Names the handbook page of a block, the same way the vanilla pages link to one another.
    /// </summary>
    private string PageCodeForStack(ItemStack stack)
        => stack.Collectible.GetCollectibleInterface<IHandBookPageCodeProvider>()
                ?.HandbookPageCodeForStack(capi.World, stack)
            ?? GuiHandbookItemStackPage.PageCodeForStack(stack);
}
