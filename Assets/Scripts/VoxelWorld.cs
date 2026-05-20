using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public VoxelChunk chunkPrefab;
    public Transform player;
    public int renderDistance = 3;

    readonly Dictionary<Vector2Int, VoxelChunk> loadedChunks = new();

    void Update() => UpdateChunks();
    void OnApplicationQuit() => SaveDirtyChunks();
    void OnDisable() => SaveDirtyChunks();

    void UpdateChunks()
    {
        var playerChunk = GetChunkCoord(player.position);
        var neededChunks = new HashSet<Vector2Int>();

        for (var x = -renderDistance; x <= renderDistance; x++)
            for (var z = -renderDistance; z <= renderDistance; z++)
            {
                var coord = playerChunk + new Vector2Int(x, z);
                neededChunks.Add(coord);

                if (!loadedChunks.ContainsKey(coord)) LoadChunk(coord);
            }

        var chunksToUnload = new List<Vector2Int>();

        foreach (var coord in loadedChunks.Keys) if (!neededChunks.Contains(coord)) chunksToUnload.Add(coord);
        foreach (var coord in chunksToUnload) UnloadChunk(coord);
    }

    void LoadChunk(Vector2Int coord)
    {
        var chunk = Instantiate(chunkPrefab, GetChunkPosition(coord), Quaternion.identity, transform);
        var exists = ChunkStorage.Exists(coord);

        chunk.chunkCoord = coord;

        if (exists) chunk.SetBlocks(ChunkStorage.Load(coord));

        chunk.Initialize(!exists);
        loadedChunks.Add(coord, chunk);
    }

    void UnloadChunk(Vector2Int coord)
    {
        var chunk = loadedChunks[coord];
        if (chunk.IsDirty) ChunkStorage.Save(coord, chunk.CopyBlocks());

        loadedChunks.Remove(coord);
        Destroy(chunk.gameObject);
    }

    public void SetBlockWorld(Vector3Int worldPos, byte block)
    {
        var chunkCoord = GetChunkCoord(worldPos);

        if (!loadedChunks.TryGetValue(chunkCoord, out var chunk)) return;

        var localPos = new Vector3Int(Mod(worldPos.x), worldPos.y, Mod(worldPos.z));
        chunk.SetBlock(localPos.x, localPos.y, localPos.z, block);

        if (localPos.x == 0) RebuildChunk(chunkCoord + Vector2Int.left);
        if (localPos.x == VoxelChunk.Size - 1) RebuildChunk(chunkCoord + Vector2Int.right);
        if (localPos.z == 0) RebuildChunk(chunkCoord + Vector2Int.down);
        if (localPos.z == VoxelChunk.Size - 1) RebuildChunk(chunkCoord + Vector2Int.up);
    }

    void RebuildChunk(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out var chunk)) chunk.GenerateMesh();
    }

    void SaveDirtyChunks()
    {
        foreach (var (coord, chunk) in loadedChunks) if (chunk.IsDirty) ChunkStorage.Save(coord, chunk.CopyBlocks());
    }

    static int Mod(int value) => ((value % VoxelChunk.Size) + VoxelChunk.Size) % VoxelChunk.Size;

    static Vector2Int GetChunkCoord(Vector3 position) => new(Mathf.FloorToInt(position.x / VoxelChunk.Size), Mathf.FloorToInt(position.z / VoxelChunk.Size));
    static Vector3 GetChunkPosition(Vector2Int coord) => new(coord.x * VoxelChunk.Size, 0, coord.y * VoxelChunk.Size);
}