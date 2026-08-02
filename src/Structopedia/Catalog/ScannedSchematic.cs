namespace Structopedia.Catalog;

/// <summary>
/// One schematic file found while scanning the asset origins, before it is grouped into the catalog.
/// </summary>
/// <param name="RelativePath">
/// Path below <c>worldgen/schematics/</c>, extension included, for example <c>trader/cold/tent1.json</c>.
/// </param>
/// <param name="Origin">Who shipped the file.</param>
internal sealed record ScannedSchematic(string RelativePath, StructureOrigin Origin);
