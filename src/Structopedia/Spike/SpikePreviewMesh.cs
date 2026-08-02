using System;
using Vintagestory.API.Client;

namespace Structopedia.Spike;

/// <summary>
/// GPU-side handle for the spike schematic: the uploaded mesh plus the extents needed to centre it.
/// Owned by <see cref="StructopediaModSystem"/>, shared by every page instance.
/// </summary>
internal sealed class SpikePreviewMesh : IDisposable
{
    internal SpikePreviewMesh(MultiTextureMeshRef meshRef, int sizeX, int sizeY, int sizeZ)
    {
        MeshRef = meshRef;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    internal MultiTextureMeshRef MeshRef { get; }

    internal int SizeX { get; }

    internal int SizeY { get; }

    internal int SizeZ { get; }

    /// <summary>Length of the schematic diagonal, used to derive a zoom that fits the frame.</summary>
    internal float Diagonal => MathF.Sqrt((SizeX * SizeX) + (SizeY * SizeY) + (SizeZ * SizeZ));

    public void Dispose() => MeshRef.Dispose();
}
