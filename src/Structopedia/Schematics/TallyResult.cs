using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Structopedia.Schematics;

/// <summary>
/// The block list of one schematic: every visible block with how many times it occurs, plus how many
/// entries were dropped because they were bookkeeping blocks rather than real geometry.
/// </summary>
/// <param name="Blocks">Visible blocks, most numerous first, ties broken by code.</param>
/// <param name="MetaCount">Number of worldgen marker entries that were left out.</param>
/// <param name="MultiblockCount">Number of multiblock placeholder entries that were left out.</param>
/// <param name="RandomizerCount">Number of worldgen randomizer entries that were left out.</param>
/// <param name="UnknownCount">Number of entries whose block id the schematic did not define.</param>
internal sealed record TallyResult(
    IReadOnlyList<(AssetLocation Code, int Count)> Blocks,
    int MetaCount,
    int MultiblockCount,
    int RandomizerCount,
    int UnknownCount);
