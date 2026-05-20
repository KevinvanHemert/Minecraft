using System.IO;
using UnityEngine;

public static class ChunkStorage
{
    static string Folder => Path.Combine(Application.persistentDataPath, "Worlds", "MyWorld", "Chunks");

    public static void Save(Vector2Int coord, byte[] blocks)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(GetPath(coord), blocks);
    }

    public static byte[] Load(Vector2Int coord) => File.ReadAllBytes(GetPath(coord));

    public static bool Exists(Vector2Int coord) => File.Exists(GetPath(coord));

    static string GetPath(Vector2Int coord) => Path.Combine(Folder, $"{coord.x}_{coord.y}.chunk");
}