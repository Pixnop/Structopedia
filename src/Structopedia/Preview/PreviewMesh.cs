using System;
using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>
/// GPU-side handle for one structure preview: the uploaded mesh plus the extents needed to centre it.
/// Owned by <see cref="PreviewMeshStore"/>, only borrowed by the component that draws it.
/// </summary>
internal sealed class PreviewMesh : IDisposable
{
    internal PreviewMesh(MultiTextureMeshRef meshRef, int sizeX, int sizeY, int sizeZ, bool truncated)
    {
        MeshRef = meshRef;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        Truncated = truncated;
    }

    internal MultiTextureMeshRef MeshRef { get; }

    internal int SizeX { get; }

    internal int SizeY { get; }

    internal int SizeZ { get; }

    /// <summary>
    /// True when the build stopped on the vertex budget, so the mesh only shows part of the structure.
    /// </summary>
    internal bool Truncated { get; }

    /// <summary>Length of the structure diagonal, used to derive a zoom that fits the frame.</summary>
    internal float Diagonal => MathF.Sqrt((SizeX * SizeX) + (SizeY * SizeY) + (SizeZ * SizeZ));

    /// <summary>True while the mesh can still be handed to the renderer.</summary>
    internal bool IsUsable => !MeshRef.Disposed && MeshRef.Initialized;

    public void Dispose() => MeshRef.Dispose();
}
