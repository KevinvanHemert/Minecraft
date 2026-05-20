using System.IO;
using UnityEngine;

public static class ChunkStorage
{
    private static string ChunkFolder =>
        Path.Combine(Application.persistentDataPath, "Worlds", "MyWorld", "Chunks");

    public static bool Exists(Vector2Int coord)
    {
        return File.Exists(GetPath(coord));
    }

    public static void Save(Vector2Int coord, byte[,,] blocks)
    {
        Directory.CreateDirectory(ChunkFolder);

        byte[] data = Flatten(blocks);
        File.WriteAllBytes(GetPath(coord), data);
    }

    public static byte[,,] Load(Vector2Int coord)
    {
        byte[] data = File.ReadAllBytes(GetPath(coord));
        return Unflatten(data);
    }

    private static string GetPath(Vector2Int coord)
    {
        return Path.Combine(ChunkFolder, $"{coord.x}_{coord.y}.chunk");
    }

    private static byte[] Flatten(byte[,,] blocks)
    {
        byte[] data = new byte[VoxelChunk.Size * VoxelChunk.Size * VoxelChunk.Size];

        int i = 0;

        for (int x = 0; x < VoxelChunk.Size; x++)
            for (int y = 0; y < VoxelChunk.Size; y++)
                for (int z = 0; z < VoxelChunk.Size; z++)
                {
                    data[i++] = blocks[x, y, z];
                }

        return data;
    }

    private static byte[,,] Unflatten(byte[] data)
    {
        byte[,,] blocks = new byte[VoxelChunk.Size, VoxelChunk.Size, VoxelChunk.Size];

        int i = 0;

        for (int x = 0; x < VoxelChunk.Size; x++)
            for (int y = 0; y < VoxelChunk.Size; y++)
                for (int z = 0; z < VoxelChunk.Size; z++)
                {
                    blocks[x, y, z] = data[i++];
                }

        return blocks;
    }
}