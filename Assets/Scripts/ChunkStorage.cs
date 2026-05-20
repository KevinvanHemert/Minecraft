using System;
using System.IO;
using UnityEngine;

public static class ChunkStorage
{
    static string Folder => Path.Combine(Application.persistentDataPath, "Worlds", "MyWorld", "Chunks");

    public static bool Exists(Vector2Int coord) => File.Exists(GetPath(coord));

    public static void Save(Vector2Int coord, byte[,,] blocks)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(GetPath(coord), Flatten(blocks));
    }

    public static byte[,,] Load(Vector2Int coord)
    {
        return Unflatten(File.ReadAllBytes(GetPath(coord)));
    }

    static byte[] Flatten(byte[,,] blocks)
    {
        var data = new byte[blocks.Length];
        Buffer.BlockCopy(blocks, 0, data, 0, data.Length);
        return data;
    }

    static byte[,,] Unflatten(byte[] data)
    {
        var blocks = new byte[VoxelChunk.Size, VoxelChunk.Size, VoxelChunk.Size];
        Buffer.BlockCopy(data, 0, blocks, 0, data.Length);
        return blocks;
    }

    static string GetPath(Vector2Int coord) => Path.Combine(Folder, $"{coord.x}_{coord.y}.chunk");

}