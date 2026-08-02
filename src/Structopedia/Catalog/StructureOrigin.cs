namespace Structopedia.Catalog;

/// <summary>
/// Who a schematic came from. Two schematics from different origins never share a catalog group,
/// even when they sit in identically named folders.
/// </summary>
/// <param name="Kind">Broad category of the provider.</param>
/// <param name="DisplayName">Name shown to the player, typically the mod name.</param>
internal sealed record StructureOrigin(StructureOriginKind Kind, string DisplayName);
