namespace Structopedia.Schematics;

/// <summary>What a block code stands for inside a schematic.</summary>
internal enum BlockRole
{
    /// <summary>A real block: it has geometry and belongs in the block list.</summary>
    Visible,

    /// <summary>A worldgen marker (<c>meta-*</c>) that only the generator reads.</summary>
    MetaMarker,

    /// <summary>A placeholder standing in for part of a multiblock structure.</summary>
    MultiblockGhost,

    /// <summary>The schematic referenced a block id its own code table does not define.</summary>
    UnknownCode
}
