using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Structopedia.Spike;

/// <summary>
/// Hardcoded handbook page used by the render spike: one schematic, drawn in 3D inside the text flow.
/// </summary>
internal sealed class SpikeStructurePage : GuiHandbookPage
{
    /// <summary>Category code of the page, which becomes a handbook tab of its own.</summary>
    internal const string CategoryCodeValue = "structures";

    /// <summary>Page code, also usable as <c>handbook://structopedia-spike</c>.</summary>
    internal const string PageCodeValue = "structopedia-spike";

    private const string TitleKey = "structopedia:spike-vug-title";
    private const string IntroKey = "structopedia:spike-vug-intro";
    private const string ControlsKey = "structopedia:spike-vug-controls";

    private readonly ICoreClientAPI capi;
    private readonly StructopediaModSystem modSystem;
    private readonly string title;
    private readonly string searchTitle;
    private readonly string searchText;

    private LoadedTexture? listEntryTexture;

    internal SpikeStructurePage(ICoreClientAPI capi, StructopediaModSystem modSystem)
    {
        this.capi = capi;
        this.modSystem = modSystem;
        title = Lang.Get(TitleKey);
        searchTitle = StringUtil.ToSearchFriendly(title);
        searchText = Lang.Get(IntroKey);
    }

    /// <inheritdoc/>
    public override string PageCode => PageCodeValue;

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
    public override void ComposePage(GuiComposer detailViewGui, ElementBounds textBounds, ItemStack[] allstacks, ActionConsumable<string> openDetailPageFor)
    {
        var components = new List<RichTextComponentBase>();
        components.AddRange(VtmlUtil.Richtextify(capi, "<strong>" + title + "</strong>\n", CairoFont.WhiteSmallishText()));
        components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(IntroKey) + "\n", CairoFont.WhiteSmallText()));
        components.Add(new ClearFloatTextComponent(capi, 8f));

        // ComposePage runs again on every navigation to this page and the previous composer is
        // disposed, so a fresh component is created each time. The GPU mesh it draws is shared and
        // owned by the mod system; the composer owns and disposes the component.
        components.Add(new SpikePreviewComponent(capi, modSystem));

        components.Add(new ClearFloatTextComponent(capi, 8f));
        components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(ControlsKey), CairoFont.WhiteDetailText()));

        // Exactly one richtext element named "richtext": the detail page scroll is wired on it.
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

    private void Recompose(ICoreClientAPI api)
    {
        ArgumentNullException.ThrowIfNull(api);

        listEntryTexture?.Dispose();
        listEntryTexture = new TextTextureUtil(api).GenTextTexture(title, CairoFont.WhiteSmallText());
    }
}
