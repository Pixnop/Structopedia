using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Structopedia.Schematics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Xunit;
using Xunit.Abstractions;

namespace Structopedia.Pure.Tests.Integration;

/// <summary>
/// Guards the build guides Structopedia ships. They are not decoration: a player follows them block
/// by block, so a wrong offset or a code the game does not know is a bug that only shows up in a
/// half built furnace. Every rule replayed here comes from the 1.22.6 game code, never from a wiki.
/// </summary>
public sealed class CuratedBuildSchematicsTests
{
    /// <summary>Folder the guides sit in, below the mod assets copied next to the test assembly.</summary>
    private const string BuildsFolder = "assets/structopedia/worldgen/schematics/builds";

    /// <summary>Block type declaring the cementation furnace, below a game install.</summary>
    private const string StoneCoffinBlockType =
        "assets/survival/blocktypes/stone/cementationfurnace/stonecoffin.json";

    /// <summary>Attribute key of the variants that carry the structure, north and east.</summary>
    private const string ControllerVariantKey = "@(.*)-(north|east)";

    /// <summary>Every guide, so a file that silently stops being shipped fails the run.</summary>
    private static readonly string[] Expected =
    [
        "bloomery/with-chimney.json",
        "cementation-furnace/minimum.json",
        "cementation-furnace/with-grated-chimney.json",
        "charcoal-pit/sealed-mound.json",
        "charcoal-pit/smallest-pit.json"
    ];

    private readonly ITestOutputHelper _output;

    public CuratedBuildSchematicsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Every_Guide_Is_Shipped_And_Parses_Inside_Its_Own_Bounds()
    {
        string folder = BuildsRoot();
        string[] found = Directory
            .GetFiles(folder, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(folder, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Expected, found);

        foreach (string relativePath in found)
        {
            BlockSchematic schematic = Load(folder, relativePath);
            IReadOnlyList<SchematicCell> cells = SchematicCellReader.ReadCells(schematic);

            Assert.True(cells.Count > 0, $"{relativePath} holds no block at all.");

            var occupied = new HashSet<(int X, int Y, int Z)>();
            foreach (SchematicCell cell in cells)
            {
                bool inBounds = cell.X >= 0 && cell.X < schematic.SizeX
                    && cell.Y >= 0 && cell.Y < schematic.SizeY
                    && cell.Z >= 0 && cell.Z < schematic.SizeZ;
                string where = $"{relativePath}: ({cell.X},{cell.Y},{cell.Z})";

                Assert.True(
                    inBounds,
                    $"{where} is outside {schematic.SizeX}x{schematic.SizeY}x{schematic.SizeZ}.");
                Assert.True(occupied.Add((cell.X, cell.Y, cell.Z)), $"{where} is written twice.");
            }

            // A schematic the game wrote is trimmed to its content, and so is a hand written one:
            // a size larger than the blocks inside it would leave an empty slice in the preview.
            Assert.Equal(schematic.SizeX - 1, cells.Max(cell => cell.X));
            Assert.Equal(schematic.SizeY - 1, cells.Max(cell => cell.Y));
            Assert.Equal(schematic.SizeZ - 1, cells.Max(cell => cell.Z));

            _output.WriteLine(
                $"{relativePath}: {cells.Count} blocks in "
                + $"{schematic.SizeX}x{schematic.SizeY}x{schematic.SizeZ}.");
        }
    }

    [Fact]
    public void Every_Guide_Names_Real_Blocks_Of_The_Base_Game()
    {
        string folder = BuildsRoot();

        foreach (string relativePath in Expected)
        {
            BlockSchematic schematic = Load(folder, relativePath);

            foreach (KeyValuePair<int, AssetLocation> entry in schematic.BlockCodes)
            {
                Assert.Equal(GlobalConstants.DefaultDomain, entry.Value.Domain);

                // A guide tells a player what to place. Worldgen markers are placed by nobody.
                Assert.NotEqual(BlockRole.MetaMarker, BlockClassifier.Classify(entry.Value));
                Assert.NotEqual(BlockRole.WorldgenRandomizer, BlockClassifier.Classify(entry.Value));
            }
        }
    }

    /// <summary>
    /// A guide whose folder has no title and no rules reads as a humanised path with nothing under
    /// it, which is the one thing a guide must not be. English is the reference, French follows it.
    /// </summary>
    [Fact]
    public void Every_Guide_Is_Named_And_Described_In_Both_Languages()
    {
        string langFolder = Path.Combine(AppContext.BaseDirectory, "assets/structopedia/lang");

        foreach (string language in new[] { "en", "fr" })
        {
            JObject entries = JObject.Parse(File.ReadAllText(Path.Combine(langFolder, language + ".json")));

            Assert.NotNull(entries["source-curated"]);

            foreach (string folder in Expected.Select(path => path.Split('/')[0]).Distinct())
            {
                Assert.True(entries[$"build-title-{folder}"] != null, $"{language}.json has no title for {folder}.");
                Assert.True(entries[$"build-desc-{folder}"] != null, $"{language}.json has no rules for {folder}.");
            }
        }
    }

    /// <summary>
    /// Replays <c>MultiblockStructure.InCompleteBlockCount</c> over the furnace guide, reading the
    /// offsets and the accepted codes from the block type the game ships rather than from a copy of
    /// them. Air is a required part of that structure, so a cell the guide leaves empty is checked
    /// against the pattern exactly like a cell it fills.
    /// </summary>
    [Fact]
    public void The_Cementation_Furnace_Satisfies_The_Structure_The_Game_Declares()
    {
        if (!TryResolveGameFolder(out string gameFolder))
        {
            return;
        }

        JObject blockType = JObject.Parse(File.ReadAllText(Path.Combine(gameFolder, StoneCoffinBlockType)));
        JToken structure = blockType["attributesByType"]![ControllerVariantKey]!["multiblockStructure"]!;

        var patterns = new Dictionary<int, AssetLocation>();
        foreach (JProperty number in ((JObject)structure["blockNumbers"]!).Properties())
        {
            patterns[(int)number.Value!] = new AssetLocation(number.Name);
        }

        var offsets = new List<Vec4i>();
        foreach (JToken offset in structure["offsets"]!)
        {
            offsets.Add(new Vec4i((int)offset["x"]!, (int)offset["y"]!, (int)offset["z"]!, (int)offset["w"]!));
        }

        Assert.NotEmpty(offsets);
        _output.WriteLine($"{offsets.Count} offsets read from {StoneCoffinBlockType}.");

        string folder = BuildsRoot();
        foreach (string relativePath in Expected.Where(path => path.StartsWith("cementation-furnace/", StringComparison.Ordinal)))
        {
            Dictionary<(int X, int Y, int Z), AssetLocation> world = Positions(Load(folder, relativePath));

            // The controller is the north facing section, which BlockEntityStoneCoffin builds the
            // structure around with no rotation at all, so the offsets apply as they are written.
            (int X, int Y, int Z) controller = Single(world, "stonecoffinsection-granite-north");
            var mismatches = new List<string>();

            foreach (Vec4i offset in offsets)
            {
                (int X, int Y, int Z) at = (controller.X + offset.X, controller.Y + offset.Y, controller.Z + offset.Z);
                AssetLocation code = world.TryGetValue(at, out AssetLocation? found) ? found : new AssetLocation("air");

                if (!WildcardUtil.Match(patterns[offset.W], code))
                {
                    mismatches.Add($"({offset.X:+#;-#;0},{offset.Y:+#;-#;0},{offset.Z:+#;-#;0}) "
                        + $"wants {patterns[offset.W]} but the guide has {code}");
                }
            }

            string report = $"{relativePath} does not satisfy the structure:{Environment.NewLine}"
                + string.Join(Environment.NewLine, mismatches);

            Assert.True(mismatches.Count == 0, report);

            // BlockEntityStoneCoffin.hasLid(): one stonecoffinlid above each of the two sections.
            foreach ((int X, int Y, int Z) section in new[] { controller, (controller.X, controller.Y, controller.Z + 1) })
            {
                Assert.True(
                    world.TryGetValue((section.X, section.Y + 1, section.Z), out AssetLocation? lid)
                    && lid.Path.StartsWith("stonecoffinlid", StringComparison.Ordinal),
                    $"{relativePath} has no coffin lid above {section}.");
            }

            // IsCompleteCoffin: the partner section faces the other way, one block along +Z.
            Assert.Equal(
                "game:stonecoffinsection-granite-south",
                world[(controller.X, controller.Y, controller.Z + 1)].ToString());
        }
    }

    /// <summary>
    /// Replays <c>BlockEntityCharcoalPit.FindHolesInPit</c>: the pit is the charcoal pit block plus
    /// every firewood pile reachable from it, and each face of that volume has to meet a block that
    /// is side solid and does not burn. <c>InCube</c> then caps the volume at 11 blocks per axis.
    /// </summary>
    [Fact]
    public void The_Charcoal_Pits_Are_Sealed_On_Every_Face_And_Fit_The_Walk()
    {
        const int MaxSize = 11;
        string folder = BuildsRoot();

        foreach (string relativePath in Expected.Where(path => path.StartsWith("charcoal-pit/", StringComparison.Ordinal)))
        {
            Dictionary<(int X, int Y, int Z), AssetLocation> world = Positions(Load(folder, relativePath));
            (int X, int Y, int Z) pit = Single(world, "charcoalpit");

            // BlockFirepit.TryConstruct only turns the firepit into a pit over a firewood pile.
            Assert.Equal("game:groundstorage", world[(pit.X, pit.Y - 1, pit.Z)].ToString());

            var volume = new HashSet<(int X, int Y, int Z)> { pit };
            var queue = new Queue<(int X, int Y, int Z)>();
            queue.Enqueue(pit);

            while (queue.Count > 0)
            {
                foreach ((int X, int Y, int Z) neighbour in Neighbours(queue.Dequeue()))
                {
                    if (world.TryGetValue(neighbour, out AssetLocation? code)
                        && code.Path == "groundstorage"
                        && volume.Add(neighbour))
                    {
                        queue.Enqueue(neighbour);
                    }
                }
            }

            int piles = world.Count(entry => entry.Value.Path == "groundstorage");
            Assert.Equal(piles + 1, volume.Count);

            foreach ((int X, int Y, int Z) cell in volume)
            {
                foreach ((int X, int Y, int Z) neighbour in Neighbours(cell))
                {
                    if (volume.Contains(neighbour))
                    {
                        continue;
                    }

                    Assert.True(
                        world.TryGetValue(neighbour, out AssetLocation? cover)
                        && cover.Path.StartsWith("soil-", StringComparison.Ordinal),
                        $"{relativePath}: {neighbour} is a hole in the pit.");
                }
            }

            int width = volume.Max(cell => cell.X) - volume.Min(cell => cell.X) + 1;
            int height = volume.Max(cell => cell.Y) - volume.Min(cell => cell.Y) + 1;
            int depth = volume.Max(cell => cell.Z) - volume.Min(cell => cell.Z) + 1;

            Assert.True(
                width <= MaxSize && height <= MaxSize && depth <= MaxSize,
                $"{relativePath}: the pit spans {width}x{height}x{depth}, over the {MaxSize} block limit.");

            _output.WriteLine($"{relativePath}: {piles} firewood piles, pit volume {width}x{height}x{depth}.");
        }
    }

    /// <summary>
    /// <c>BlockEntityBloomery.TryIgnite</c> refuses to light unless the block one above the base is a
    /// bloomery chimney, which makes the chimney part of the structure rather than an option.
    /// </summary>
    [Fact]
    public void The_Bloomery_Carries_The_Chimney_That_Lets_It_Light()
    {
        Dictionary<(int X, int Y, int Z), AssetLocation> world =
            Positions(Load(BuildsRoot(), "bloomery/with-chimney.json"));

        (int X, int Y, int Z) bloomery = Single(world, "bloomerybase-north");

        Assert.True(world.TryGetValue((bloomery.X, bloomery.Y + 1, bloomery.Z), out AssetLocation? above));
        Assert.Contains("bloomerychimney", above.Path, StringComparison.Ordinal);
        Assert.Equal(2, world.Count);
    }

    private static IEnumerable<(int X, int Y, int Z)> Neighbours((int X, int Y, int Z) cell)
    {
        yield return (cell.X + 1, cell.Y, cell.Z);
        yield return (cell.X - 1, cell.Y, cell.Z);
        yield return (cell.X, cell.Y + 1, cell.Z);
        yield return (cell.X, cell.Y - 1, cell.Z);
        yield return (cell.X, cell.Y, cell.Z + 1);
        yield return (cell.X, cell.Y, cell.Z - 1);
    }

    private static Dictionary<(int X, int Y, int Z), AssetLocation> Positions(BlockSchematic schematic)
    {
        var world = new Dictionary<(int X, int Y, int Z), AssetLocation>();
        foreach (SchematicCell cell in SchematicCellReader.ReadCells(schematic))
        {
            world[(cell.X, cell.Y, cell.Z)] = cell.Code!;
        }

        return world;
    }

    private static (int X, int Y, int Z) Single(
        Dictionary<(int X, int Y, int Z), AssetLocation> world,
        string path)
    {
        return world.Single(entry => entry.Value.Path == path).Key;
    }

    private static BlockSchematic Load(string folder, string relativePath)
    {
        string error = string.Empty;
        BlockSchematic? schematic = BlockSchematic.LoadFromString(
            File.ReadAllText(Path.Combine(folder, relativePath)),
            ref error);

        Assert.True(schematic != null, $"{relativePath} failed to parse: {error}");
        return schematic!;
    }

    /// <summary>
    /// Locates the guides in the mod assets copied next to the test assembly, which is where the
    /// build puts them and where the mod itself reads them from once it is packed.
    /// </summary>
    private static string BuildsRoot()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, BuildsFolder);
        Assert.True(Directory.Exists(folder), $"No build guides at {folder}.");
        return folder;
    }

    /// <summary>
    /// Finds the game install through the VINTAGE_STORY variable the build already relies on, so a
    /// contributor without the game still gets a green run on everything else.
    /// </summary>
    private bool TryResolveGameFolder(out string gameFolder)
    {
        gameFolder = Environment.GetEnvironmentVariable("VINTAGE_STORY") ?? string.Empty;

        if (gameFolder.Length == 0 || !File.Exists(Path.Combine(gameFolder, StoneCoffinBlockType)))
        {
            _output.WriteLine($"Skipped: no {StoneCoffinBlockType} below VINTAGE_STORY.");
            gameFolder = string.Empty;
            return false;
        }

        return true;
    }
}
