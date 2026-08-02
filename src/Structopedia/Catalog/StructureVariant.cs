namespace Structopedia.Catalog;

/// <summary>One schematic file inside a catalog group, ready to be listed.</summary>
/// <param name="RelativePath">Path below <c>worldgen/schematics/</c>, used to load the file again.</param>
/// <param name="Title">Readable name derived from the file name.</param>
internal sealed record StructureVariant(string RelativePath, string Title);
