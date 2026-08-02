using Vintagestory.API.Client;

namespace Structopedia.Preview;

/// <summary>One layer of a structure, as geometry sitting on the graphics card.</summary>
/// <param name="Y">Height of the layer inside the schematic.</param>
/// <param name="MeshRef">Uploaded mesh of every block sitting on it.</param>
internal readonly record struct PreviewLayer(int Y, MultiTextureMeshRef MeshRef);
