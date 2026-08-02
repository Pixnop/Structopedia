using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>One layer of a structure, as geometry that has not reached the graphics card yet.</summary>
/// <param name="Y">Height of the layer inside the schematic.</param>
/// <param name="Mesh">Merged mesh of every block sitting on it.</param>
internal readonly record struct LayerMesh(int Y, MeshData Mesh);
