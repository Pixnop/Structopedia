using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>
/// GPU-side handle for one structure preview: the uploaded mesh of every layer, the extents needed
/// to centre it and what the build had to leave out.
/// Owned by <see cref="PreviewMeshStore"/>, only borrowed by the component that draws it.
/// </summary>
internal sealed class PreviewEntry : IDisposable
{
    private readonly IReadOnlyList<PreviewLayer> layers;

    internal PreviewEntry(IReadOnlyList<PreviewLayer> layers, MeshBuildResult build)
    {
        this.layers = layers;
        SizeX = build.SizeX;
        SizeY = build.SizeY;
        SizeZ = build.SizeZ;
        MinLayer = build.MinLayer;
        MaxLayer = build.MaxLayer;
        Truncated = build.Truncated;
        TruncatedAtLayer = build.TruncatedAtLayer;
        ChiseledCount = build.ChiseledCount;
        ChiseledFallbackCount = build.ChiseledFallbackCount;
        ClutterSkippedCount = build.ClutterSkippedCount;
    }

    /// <summary>Layers from the lowest up, one per layer that ended up with geometry.</summary>
    internal IReadOnlyList<PreviewLayer> Layers => layers;

    internal int SizeX { get; }

    internal int SizeY { get; }

    internal int SizeZ { get; }

    /// <summary>Lowest layer of the structure, the bottom of the slider range.</summary>
    internal int MinLayer { get; }

    /// <summary>Highest layer of the structure, the top of the slider range.</summary>
    internal int MaxLayer { get; }

    /// <summary>
    /// True when the build stopped on the vertex budget, so the structure has no layers above
    /// <see cref="TruncatedAtLayer"/>.
    /// </summary>
    internal bool Truncated { get; }

    /// <summary>Layer the budget ran out on, or null when the whole structure was built.</summary>
    internal int? TruncatedAtLayer { get; }

    /// <summary>Chiselled blocks drawn from their block entity data.</summary>
    internal int ChiseledCount { get; }

    /// <summary>Chiselled blocks left out because their block entity data could not be used.</summary>
    internal int ChiseledFallbackCount { get; }

    /// <summary>Blocks left out because their shape only exists in a block entity.</summary>
    internal int ClutterSkippedCount { get; }

    /// <summary>Length of the structure diagonal, used to derive a zoom that fits the frame.</summary>
    internal float Diagonal => MathF.Sqrt((SizeX * SizeX) + (SizeY * SizeY) + (SizeZ * SizeZ));

    /// <summary>True while the layers can still be handed to the renderer.</summary>
    internal bool IsUsable
    {
        get
        {
            foreach (PreviewLayer layer in layers)
            {
                if (layer.MeshRef.Disposed || !layer.MeshRef.Initialized)
                {
                    return false;
                }
            }

            return layers.Count > 0;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (PreviewLayer layer in layers)
        {
            layer.MeshRef.Dispose();
        }
    }
}
