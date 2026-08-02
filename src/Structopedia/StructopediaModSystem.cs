using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Structopedia;

/// <summary>Client-only entry point for the Structopedia mod.</summary>
public sealed class StructopediaModSystem : ModSystem
{
    /// <inheritdoc/>
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    /// <inheritdoc/>
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        Mod.Logger.Notification("{0} {1} loaded.", Mod.Info.Name, Mod.Info.Version);
    }
}
