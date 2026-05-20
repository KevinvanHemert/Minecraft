using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    public VoxelChunk chunkPrefab;
    public Transform player;
    public int renderDistance = 3;

    private readonly Dictionary<Vector2Int, VoxelChunk> loadedChunks = new();
    private readonly Dictionary<Vector2Int, byte[,,]> savedChunkData = new();

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        Vector2Int playerChunk = GetChunkCoord(player.position);

        HashSet<Vector2Int> neededChunks = new();

        for (int x = -renderDistance; x <= renderDistance; x++)
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int coord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                neededChunks.Add(coord);

                if (!loadedChunks.ContainsKey(coord))
                    LoadChunk(coord);
            }

        List<Vector2Int> chunksToUnload = new();

        foreach (var pair in loadedChunks)
        {
            if (!neededChunks.Contains(pair.Key))
                chunksToUnload.Add(pair.Key);
        }

        foreach (Vector2Int coord in chunksToUnload)
            UnloadChunk(coord);
    }

    void LoadChunk(Vector2Int coord)
    {
        Vector3 position = new Vector3(
            coord.x * VoxelChunk.Size,
            0,
            coord.y * VoxelChunk.Size
        );

        VoxelChunk chunk = Instantiate(
            chunkPrefab,
            position,
            Quaternion.identity,
            transform
        );

        chunk.chunkCoord = coord;

        if (ChunkStorage.Exists(coord))
        {
            byte[,,] blocks = ChunkStorage.Load(coord);
            chunk.SetBlocks(blocks);

            Debug.Log($"Loaded chunk from disk: {coord}");
        }
        else
        {
            Debug.Log($"Generated new chunk: {coord}");
        }

        chunk.Initialize(!ChunkStorage.Exists(coord));

        loadedChunks.Add(coord, chunk);
    }

    void UnloadChunk(Vector2Int coord)
    {
        VoxelChunk chunk = loadedChunks[coord];

         if (chunk.IsDirty)
            ChunkStorage.Save(coord, chunk.CopyBlocks());

        loadedChunks.Remove(coord);
        Destroy(chunk.gameObject);
    }

    Vector2Int GetChunkCoord(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / VoxelChunk.Size),
            Mathf.FloorToInt(position.z / VoxelChunk.Size)
        );
    }

    public void SetBlockWorld(Vector3Int worldPos, byte block)
    {
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt((float)worldPos.x / VoxelChunk.Size),
            Mathf.FloorToInt((float)worldPos.z / VoxelChunk.Size)
        );

        if (!loadedChunks.TryGetValue(chunkCoord, out VoxelChunk chunk))
            return;

        int localX = Mod(worldPos.x, VoxelChunk.Size);
        int localY = worldPos.y;
        int localZ = Mod(worldPos.z, VoxelChunk.Size);

        chunk.SetBlock(localX, localY, localZ, block);

        RebuildNeighborIfNeeded(chunkCoord, localX, localZ);
    }

    int Mod(int value, int size)
    {
        return ((value % size) + size) % size;
    }

    void RebuildNeighborIfNeeded(Vector2Int coord, int x, int z)
    {
        if (x == 0)
            RebuildChunk(coord + Vector2Int.left);

        if (x == VoxelChunk.Size - 1)
            RebuildChunk(coord + Vector2Int.right);

        if (z == 0)
            RebuildChunk(coord + Vector2Int.down);

        if (z == VoxelChunk.Size - 1)
            RebuildChunk(coord + Vector2Int.up);
    }

    void RebuildChunk(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out VoxelChunk chunk))
            chunk.GenerateMesh();
    }

    void SaveDirtyChunks()
    {
        foreach (var pair in loadedChunks)
        {
            VoxelChunk chunk = pair.Value;

            if (chunk.IsDirty)
                ChunkStorage.Save(pair.Key, chunk.CopyBlocks());
        }
    }

    void OnApplicationQuit()
    {
        SaveDirtyChunks();
    }

    void OnDisable()
    {
        SaveDirtyChunks();
    }
}