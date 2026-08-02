namespace Structopedia.Catalog;

/// <summary>Where a schematic was found, which decides how the catalog labels and orders it.</summary>
internal enum StructureOriginKind
{
    /// <summary>Shipped by the base game.</summary>
    Game,

    /// <summary>Shipped by another mod.</summary>
    Mod,

    /// <summary>Declared by Structopedia itself, for a hand-picked selection.</summary>
    Curated
}
