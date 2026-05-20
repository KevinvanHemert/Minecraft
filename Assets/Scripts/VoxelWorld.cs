using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public VoxelChunk chunkPrefab;
    public Transform player;

    public int renderDistance = 3;
    public int chunksLoadedPerFrame = 1;

    readonly Queue<Vector2Int> chunkQueue = new();
    readonly HashSet<Vector2Int> queuedChunks = new();
    readonly Dictionary<Vector2Int, VoxelChunk> loadedChunks = new();

    void OnApplicationQuit() => SaveDirtyChunks();
    void OnDisable() => SaveDirtyChunks();

    void Start()
    {
        LoadInitialChunks();
    }

    void Update()
    {
        UpdateChunks();
        ProcessChunkQueue();
    }

    void LoadInitialChunks()
    {
        var playerChunk = GetChunkCoord(player.position);

        for (var x = -renderDistance; x <= renderDistance; x++)
            for (var z = -renderDistance; z <= renderDistance; z++)
            {
                var coord = playerChunk + new Vector2Int(x, z);

                if (!loadedChunks.ContainsKey(coord))
                    LoadChunk(coord);
            }
    }

    void UpdateChunks()
    {
        var playerChunk = GetChunkCoord(player.position);
        var neededChunks = new HashSet<Vector2Int>();

        for (var x = -renderDistance; x <= renderDistance; x++)
            for (var z = -renderDistance; z <= renderDistance; z++)
            {
                var coord = playerChunk + new Vector2Int(x, z);
                neededChunks.Add(coord);

                if (!loadedChunks.ContainsKey(coord) && queuedChunks.Add(coord))
                    chunkQueue.Enqueue(coord);
            }

        var chunksToUnload = new List<Vector2Int>();

        foreach (var coord in loadedChunks.Keys)
            if (!neededChunks.Contains(coord))
                chunksToUnload.Add(coord);

        foreach (var coord in chunksToUnload)
            UnloadChunk(coord);
    }

    void ProcessChunkQueue()
    {
        for (var i = 0; i < chunksLoadedPerFrame && chunkQueue.Count > 0; i++)
        {
            var coord = chunkQueue.Dequeue();
            queuedChunks.Remove(coord);

            if (!loadedChunks.ContainsKey(coord))
                LoadChunk(coord);
        }
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

        if (block == (byte)BlockType.Air) TryFillWithWater(worldPos);

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

    void TryFillWithWater(Vector3Int worldPos)
    {
        if (GetBlockWorld(worldPos) != (byte)BlockType.Air) return;

        if (GetBlockWorld(worldPos + Vector3Int.left) == (byte)BlockType.Water ||
            GetBlockWorld(worldPos + Vector3Int.right) == (byte)BlockType.Water ||
            GetBlockWorld(worldPos + Vector3Int.forward) == (byte)BlockType.Water ||
            GetBlockWorld(worldPos + Vector3Int.back) == (byte)BlockType.Water ||
            GetBlockWorld(worldPos + Vector3Int.up) == (byte)BlockType.Water)
        {
            SetBlockWorld(worldPos, (byte)BlockType.Water);
        }
    }

    public byte GetBlockWorld(Vector3Int worldPos)
    {
        var chunkCoord = GetChunkCoord(worldPos);

        if (!loadedChunks.TryGetValue(chunkCoord, out var chunk))
            return 0;

        return chunk.GetBlock(Mod(worldPos.x), worldPos.y, Mod(worldPos.z));
    }

    static int Mod(int value) => ((value % VoxelChunk.Size) + VoxelChunk.Size) % VoxelChunk.Size;

    static Vector2Int GetChunkCoord(Vector3 position) => new(Mathf.FloorToInt(position.x / VoxelChunk.Size), Mathf.FloorToInt(position.z / VoxelChunk.Size));
    static Vector3 GetChunkPosition(Vector2Int coord) => new(coord.x * VoxelChunk.Size, 0, coord.y * VoxelChunk.Size);


}