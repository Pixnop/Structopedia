using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Xunit;
using Xunit.Abstractions;

namespace Structopedia.Pure.Tests.Integration;

/// <summary>
/// Runs the schematic domain against the real worldgen assets, which is the only way to know the
/// decoding rules hold on all 700 odd files rather than on the handful the unit tests describe.
/// <para>
/// Skipped, silently and green, when VINTAGE_STORY does not point at a game install. CI sets it, so
/// these run there; a contributor without the game still gets a usable test run.
/// </para>
/// </summary>
public sealed class VanillaSchematicsTests
{
    /// <summary>Where the worldgen schematics sit below a game or server install.</summary>
    private const string SchematicsFolder = "assets/survival/worldgen/schematics";

    /// <summary>
    /// The 1.22.6 assets hold 701 files. A floor slightly below that catches an install that
    /// resolved to the wrong place without breaking on the next content patch.
    /// </summary>
    private const int MinimumFileCount = 700;

    /// <summary>How many offending files a failure message lists before summarising the rest.</summary>
    private const int MaxReportedFailures = 20;

    /// <summary>
    /// 540 of the 1.22.6 schematics hold a chiselled block, which is what makes drawing them from
    /// their block entity data worth the trouble. Floors, not exact counts, so a content patch does
    /// not break the build.
    /// </summary>
    private const int MinimumChiselledFileCount = 400;

    /// <summary>Those 540 files hold 75209 chiselled blocks between them.</summary>
    private const int MinimumChiselledCellCount = 50_000;

    private readonly ITestOutputHelper _output;

    public VanillaSchematicsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Every_Vanilla_Schematic_Parses_And_Decodes_Inside_Its_Own_Bounds()
    {
        if (!TryResolveSchematicsFolder(out string folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
        var parseFailures = new List<string>();
        var outOfBounds = new List<string>();
        var strayDuplicates = new List<string>();
        long totalCells = 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(folder, file);
            string error = string.Empty;

            // Read and parsed once: the three checks below all run off this single pass.
            BlockSchematic? schematic = BlockSchematic.LoadFromString(File.ReadAllText(file), ref error);
            if (schematic == null)
            {
                parseFailures.Add($"{relativePath}: {error}");
                continue;
            }

            IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);
            totalCells += cells.Count;

            foreach (SchematicCell cell in cells)
            {
                if (cell.X < 0 || cell.X >= schematic.SizeX
                    || cell.Y < 0 || cell.Y >= schematic.SizeY
                    || cell.Z < 0 || cell.Z >= schematic.SizeZ)
                {
                    outOfBounds.Add(
                        $"{relativePath}: ({cell.X},{cell.Y},{cell.Z}) outside " +
                        $"{schematic.SizeX}x{schematic.SizeY}x{schematic.SizeZ}");
                    break;
                }
            }

            string? stray = FindNonConsecutiveDuplicate(schematic.Indices);
            if (stray != null)
            {
                strayDuplicates.Add($"{relativePath}: {stray}");
            }
        }

        stopwatch.Stop();
        _output.WriteLine(
            $"Parsed {files.Length} vanilla schematics from {folder} " +
            $"({totalCells} cells) in {stopwatch.ElapsedMilliseconds} ms.");

        Assert.True(
            files.Length >= MinimumFileCount,
            $"Expected at least {MinimumFileCount} schematics under {folder}, found {files.Length}.");
        Assert.True(parseFailures.Count == 0, Describe("failed to parse", parseFailures));
        Assert.True(outOfBounds.Count == 0, Describe("decoded a cell outside its bounds", outOfBounds));
        Assert.True(
            strayDuplicates.Count == 0,
            Describe("repeated an index without the two entries being consecutive", strayDuplicates));
    }

    [Fact]
    public void The_Smoky_Quartz_Vug_Tallies_Its_Blocks_Without_Its_Markers()
    {
        if (!TryResolveSchematicsFolder(out string folder))
        {
            return;
        }

        string path = Path.Combine(folder, "vug", "smokyquartz", "vug-medium1.json");
        string error = string.Empty;
        BlockSchematic? schematic = BlockSchematic.LoadFromString(File.ReadAllText(path), ref error);

        Assert.NotNull(schematic);
        Assert.Equal(7, schematic.SizeX);
        Assert.Equal(7, schematic.SizeY);
        Assert.Equal(8, schematic.SizeZ);

        TallyResult tally = BlockTally.Count(SchematicCellReader.ReadCells(schematic));

        _output.WriteLine(
            $"vug-medium1: {tally.Blocks.Count} distinct blocks, " +
            $"{tally.MetaCount} meta markers, {tally.UnknownCount} unresolved ids.");

        Assert.NotEmpty(tally.Blocks);
        Assert.True(tally.MetaCount > 0, "The vug is expected to carry worldgen markers.");
        Assert.DoesNotContain(tally.Blocks, row => row.Code.Path.StartsWith("meta-", StringComparison.Ordinal));
    }

    /// <summary>
    /// The preview draws a chiselled block from its block entity data, which means finding that data
    /// by the packed position of the block and moving the material ids inside it onto the running
    /// install. Both steps are assumptions about a format nobody documented, so they are replayed
    /// here against every chiselled block the game ships.
    /// </summary>
    [Fact]
    public void Every_Chiselled_Block_Of_The_Vanilla_Schematics_Can_Be_Rebuilt()
    {
        if (!TryResolveSchematicsFolder(out string folder))
        {
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
        var withoutData = new List<string>();
        var unreadable = new List<string>();
        var unmappable = new List<string>();
        int filesWithChiselled = 0;
        int chiselledCells = 0;

        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(folder, file);
            string error = string.Empty;

            BlockSchematic? schematic = BlockSchematic.LoadFromString(File.ReadAllText(file), ref error);
            if (schematic == null)
            {
                continue;
            }

            // The mod itself asks the block registry whether a block is a BlockMicroBlock. There is no
            // registry here, so the code stands in for it, which is what names those blocks anyway.
            var chiselledIds = new HashSet<int>();
            foreach (KeyValuePair<int, AssetLocation> pair in schematic.BlockCodes)
            {
                if (pair.Value.Path.StartsWith("microblock", StringComparison.Ordinal)
                    || pair.Value.Path.StartsWith("chiseledblock", StringComparison.Ordinal))
                {
                    chiselledIds.Add(pair.Key);
                }
            }

            if (chiselledIds.Count == 0)
            {
                continue;
            }

            filesWithChiselled++;
            IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

            for (int i = 0; i < cells.Count; i++)
            {
                if (!chiselledIds.Contains(schematic.BlockIds[i]))
                {
                    continue;
                }

                chiselledCells++;
                SchematicCell cell = cells[i];
                uint packed = SchematicCellReader.PackIndex(cell.X, cell.Y, cell.Z);

                if (!schematic.BlockEntities.TryGetValue(packed, out string? encoded))
                {
                    withoutData.Add($"{relativePath}: nothing keyed at ({cell.X},{cell.Y},{cell.Z})");
                    continue;
                }

                if (schematic.DecodeBlockEntityData(encoded)["materials"] is not IntArrayAttribute materials)
                {
                    unreadable.Add($"{relativePath}: ({cell.X},{cell.Y},{cell.Z}) has no int materials");
                    continue;
                }

                int[]? remapped = MicroBlockMaterials.Remap(
                    materials.value,
                    schematic.BlockCodes,
                    AnythingNamedExists,
                    substituteUnresolved: true);

                if (remapped == null || remapped.Length != materials.value.Length)
                {
                    unmappable.Add($"{relativePath}: ({cell.X},{cell.Y},{cell.Z}) lost its materials");
                }
            }
        }

        _output.WriteLine(
            $"Rebuilt the materials of {chiselledCells} chiselled blocks " +
            $"across {filesWithChiselled} of {files.Length} vanilla schematics.");

        Assert.True(filesWithChiselled >= MinimumChiselledFileCount, $"Only {filesWithChiselled} schematics held a chiselled block.");
        Assert.True(chiselledCells >= MinimumChiselledCellCount, $"Only {chiselledCells} chiselled blocks were found.");
        Assert.True(withoutData.Count == 0, Describe("keyed a chiselled block to no data", withoutData));
        Assert.True(unreadable.Count == 0, Describe("stored materials in a form the preview cannot read", unreadable));
        Assert.True(unmappable.Count == 0, Describe("named a material its own code table does not hold", unmappable));
    }

    /// <summary>
    /// Stands in for a block registry holding every block a schematic can possibly name, so a failure
    /// can only come from the code table of the schematic and not from what this install happens to
    /// have.
    /// </summary>
    private static int? AnythingNamedExists(AssetLocation code) => code.GetHashCode() | 1;

    /// <summary>
    /// Reports the first index that comes back after something else was written in between. A
    /// schematic is allowed to repeat an index twice in a row, that is how a waterlogged cell stores
    /// its fluid; anything else would mean the position no longer identifies a cell.
    /// </summary>
    private static string? FindNonConsecutiveDuplicate(IReadOnlyList<uint> indices)
    {
        var seen = new HashSet<uint>(indices.Count);
        for (int i = 0; i < indices.Count; i++)
        {
            if (i > 0 && indices[i] == indices[i - 1])
            {
                continue;
            }

            if (!seen.Add(indices[i]))
            {
                return $"index {indices[i]} reappears at position {i}";
            }
        }

        return null;
    }

    private static string Describe(string what, List<string> failures)
    {
        IEnumerable<string> shown = failures.Take(MaxReportedFailures);
        string suffix = failures.Count > MaxReportedFailures
            ? $"{Environment.NewLine}... and {failures.Count - MaxReportedFailures} more"
            : string.Empty;

        return $"{failures.Count} schematic(s) {what}:{Environment.NewLine}"
            + string.Join(Environment.NewLine, shown) + suffix;
    }

    /// <summary>
    /// Locates the vanilla schematics through the VINTAGE_STORY variable the build already relies on.
    /// </summary>
    private bool TryResolveSchematicsFolder(out string folder)
    {
        folder = string.Empty;

        string? gameFolder = Environment.GetEnvironmentVariable("VINTAGE_STORY");
        if (string.IsNullOrWhiteSpace(gameFolder))
        {
            _output.WriteLine("Skipped: VINTAGE_STORY is not set.");
            return false;
        }

        string candidate = Path.Combine(gameFolder, SchematicsFolder);
        if (!Directory.Exists(candidate))
        {
            _output.WriteLine($"Skipped: no schematics folder at {candidate}.");
            return false;
        }

        folder = candidate;
        return true;
    }
}
