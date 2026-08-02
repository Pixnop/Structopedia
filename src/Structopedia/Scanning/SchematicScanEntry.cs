using Structopedia.Catalog;

namespace Structopedia.Scanning;

/// <summary>
/// One schematic file as the scan found it: what the catalog needs to place it, and what the page
/// needs to read it back later.
/// </summary>
/// <param name="Schematic">Catalog view of the file.</param>
/// <param name="Source">Handle the file can be loaded from.</param>
internal sealed record SchematicScanEntry(ScannedSchematic Schematic, SchematicSource Source);
