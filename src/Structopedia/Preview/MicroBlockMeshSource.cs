using System;
using System.Collections.Generic;
using System.IO;
using Structopedia.Schematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Structopedia.Preview;

/// <summary>
/// Rebuilds the mesh of a chiselled block from the block entity data a schematic carries for it.
/// <para>
/// A microblock keeps nothing in its block type: the block json points at a plain cube with no
/// texture, which is what draws the placeholder cube. Its shape and its materials live in the block
/// entity, so a preview that never places anything has to decode that itself and hand the pieces to
/// the same mesh builder the game uses.
/// </para>
/// </summary>
/// <remarks>
/// Mirrors what the game does when it loads one: <c>BlockEntityMicroBlock.FromTreeAttributes</c>
/// reads the voxel cuboids, the materials, the rotation and the decor out of the tree,
/// <c>OnLoadCollectibleMappings</c> then moves the ids over from the world the schematic was saved
/// in, and <c>GenMesh</c> rotates the materials and calls <c>CreateMesh</c>.
/// </remarks>
internal static class MicroBlockMeshSource
{
    /// <summary>Tree key holding the voxel materials, as ids of the world the schematic came from.</summary>
    private const string MaterialsKey = "materials";

    /// <summary>Tree key holding the decor block per face, again as export world ids.</summary>
    private const string DecorIdsKey = "decorIds";

    /// <summary>Tree key holding the rotation of each decor face, packed three bits per face.</summary>
    private const string DecorRotationsKey = "decorRot";

    /// <summary>Tree key holding the block rotation around Y, offset by 360 and shifted up ten bits.</summary>
    private const string RotationKey = "rotation";

    /// <summary>Tree key holding the voxel bounds the block had before it was chiselled.</summary>
    private const string OriginalCuboidsKey = "originalCuboids";

    /// <summary>Faces a decor array covers, one entry each.</summary>
    private const int DecorFaceCount = 6;

    /// <summary>
    /// Suffix the game tries when a material code no longer resolves, which is how the blocks that
    /// were split into a free standing variant keep working in old schematics.
    /// </summary>
    private const string FreeVariantSuffix = "-free";

    /// <summary>
    /// Builds the mesh of one chiselled block.
    /// </summary>
    /// <param name="capi">Client API. Must be called on the main thread: this touches the atlas.</param>
    /// <param name="schematic">Schematic the block belongs to, read for its block entity data and its code table.</param>
    /// <param name="packedIndex">Packed position of the block, the key of both index lists.</param>
    /// <returns>
    /// The mesh, or null when the schematic holds no data for that position, when the data cannot be
    /// read, or when none of the materials exist in this install.
    /// </returns>
    internal static MeshData? TryBuild(ICoreClientAPI capi, BlockSchematic schematic, uint packedIndex)
    {
        ArgumentNullException.ThrowIfNull(capi);
        ArgumentNullException.ThrowIfNull(schematic);

        if (!schematic.BlockEntities.TryGetValue(packedIndex, out string? encoded) || string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        TreeAttribute? tree = TryDecode(schematic, encoded);
        if (tree == null)
        {
            return null;
        }

        uint[] cuboids = BlockEntityMicroBlock.GetVoxelCuboids(tree);
        if (cuboids == null || cuboids.Length == 0)
        {
            return null;
        }

        int[]? materials = ReadMaterials(capi, schematic, tree);
        if (materials == null)
        {
            return null;
        }

        int rotationY = ReadRotationY(tree);
        materials = RotateMaterials(capi, materials, rotationY);

        int[]? decor = ReadDecor(capi, schematic, tree, rotationY);
        uint[]? originalCuboids = (tree[OriginalCuboidsKey] as IntArrayAttribute)?.AsUint;

        // Deliberately no position: it only feeds the texture randomiser and the neighbour lookup,
        // and the neighbours of a preview block are whatever happens to stand at the world origin.
        MeshData mesh = BlockEntityMicroBlock.CreateMesh(
            capi,
            new List<uint>(cuboids),
            materials,
            decor,
            posForRnd: null,
            originalCuboids: originalCuboids,
            decorRotations: tree.GetInt(DecorRotationsKey));

        return mesh != null && mesh.VerticesCount > 0 ? mesh : null;
    }

    private static TreeAttribute? TryDecode(BlockSchematic schematic, string encoded)
    {
        try
        {
            return schematic.DecodeBlockEntityData(encoded);
        }
        catch (FormatException)
        {
            // Ascii85 payload that is not one. A single unreadable block is not worth a broken page.
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the voxel materials and moves them onto this install.
    /// </summary>
    /// <remarks>
    /// The game writes them as ints, which are the block ids of whichever install saved the
    /// schematic, so they mean nothing here until the code table has been walked. Every worldgen
    /// schematic the game ships is in that form. The string form is what old schematics carry, and
    /// there the codes already name blocks, so the vanilla reader resolves them as they are.
    /// </remarks>
    private static int[]? ReadMaterials(ICoreClientAPI capi, BlockSchematic schematic, TreeAttribute tree)
    {
        if (tree[MaterialsKey] is IntArrayAttribute exported)
        {
            return MicroBlockMaterials.Remap(
                exported.value,
                schematic.BlockCodes,
                code => ResolveBlockId(capi, code),
                substituteUnresolved: true);
        }

        if (tree[MaterialsKey] is StringArrayAttribute)
        {
            int[] resolved = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, capi.World);
            return resolved.Length > 0 ? resolved : null;
        }

        return null;
    }

    /// <summary>
    /// Reads the decor blocks and moves them onto this install, dropping the lot when one of them
    /// cannot be placed: the array is positional, so a face wearing the decor of another face is
    /// worse than a face wearing none.
    /// </summary>
    private static int[]? ReadDecor(ICoreClientAPI capi, BlockSchematic schematic, TreeAttribute tree, int rotationY)
    {
        if (tree[DecorIdsKey] is not IntArrayAttribute exported || exported.value.Length == 0)
        {
            return null;
        }

        int[]? decor = MicroBlockMaterials.Remap(
            exported.value,
            schematic.BlockCodes,
            code => ResolveBlockId(capi, code),
            substituteUnresolved: false);

        if (decor == null || rotationY == 0 || decor.Length < DecorFaceCount)
        {
            return decor;
        }

        // Same shuffle as GenRotatedMaterialIds: the four sides walk round, up and down stay put.
        var rotated = new int[decor.Length];
        for (int face = 0; face < 4; face++)
        {
            rotated[face] = decor[GameMath.Mod(face + (rotationY / 90), 4)];
        }

        for (int face = 4; face < decor.Length; face++)
        {
            rotated[face] = decor[face];
        }

        return rotated;
    }

    /// <summary>
    /// Swaps every material for the variant it takes when the block is turned, the way the game does
    /// before it meshes one. Only the materials that have rotated variants, logs and the like, move.
    /// </summary>
    private static int[] RotateMaterials(ICoreClientAPI capi, int[] materials, int rotationY)
    {
        if (rotationY == 0)
        {
            return materials;
        }

        var rotated = new int[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            rotated[i] = materials[i];

            Block? block = capi.World.GetBlock(materials[i]);
            AssetLocation? rotatedCode = block?.GetRotatedBlockCode(rotationY);
            if (rotatedCode == null)
            {
                continue;
            }

            Block? rotatedBlock = capi.World.GetBlock(rotatedCode);
            if (rotatedBlock != null)
            {
                rotated[i] = rotatedBlock.Id;
            }
        }

        return rotated;
    }

    /// <summary>Reads the Y rotation out of the packed field the game writes it in.</summary>
    private static int ReadRotationY(TreeAttribute tree) => ((tree.GetInt(RotationKey) >> 10) & 0x3FF) - 360;

    /// <summary>
    /// Names a block of this install, trying the free standing variant the way the game does when the
    /// plain code has gone.
    /// </summary>
    private static int? ResolveBlockId(ICoreClientAPI capi, AssetLocation code)
    {
        Block? block = capi.World.GetBlock(code)
            ?? capi.World.GetBlock(code.WithPathAppendixOnce(FreeVariantSuffix));

        return block?.Id;
    }
}
