using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// One decoded entry of a packed schematic: a position local to the schematic bounds plus the block
/// code it holds. <paramref name="Code"/> is null when the schematic references a block id its own
/// code table does not define.
/// </summary>
/// <param name="X">Position along the X axis, relative to the schematic origin.</param>
/// <param name="Y">Position along the Y axis, relative to the schematic origin.</param>
/// <param name="Z">Position along the Z axis, relative to the schematic origin.</param>
/// <param name="Code">Block code, or null when the block id is unknown.</param>
/// <param name="IsFluidLayer">
/// True when this entry is the fluid half of a waterlogged cell, that is a second entry sharing the
/// position of the one right before it.
/// </param>
internal readonly record struct SchematicCell(int X, int Y, int Z, AssetLocation? Code, bool IsFluidLayer);
