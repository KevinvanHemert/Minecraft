using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public const int Size = 16;

    public int worldSeed = 12345;
    public int terrainHeight = 8;
    public float terrainScale = 0.08f;
    public int dirtDepth = 3;
    public int waterLevel = 5;

    const byte Air = 0;
    const byte Grass = 1;
    const byte Dirt = 2;
    const byte Stone = 3;
    const byte Water = 4;

    public Vector2Int chunkCoord;

    public bool IsDirty { get; private set; }

    byte[] blocks = new byte[Size * Size * Size];

    int WorldX => chunkCoord.x * Size;
    int WorldZ => chunkCoord.y * Size;

    public void Initialize(bool generateTerrain)
    {
        if (generateTerrain) GenerateBlocks();
        GenerateMesh();
    }

    public void SetBlock(int x, int y, int z, byte block)
    {
        if (!InBounds(x, y, z) || GetBlock(x, y, z) == block) return;

        SetBlockLocal(x, y, z, block);
        IsDirty = true;

        GenerateMesh();
    }

    public byte[] CopyBlocks() => (byte[])blocks.Clone();

    public void SetBlocks(byte[] newBlocks)
    {
        blocks = (byte[])newBlocks.Clone();
        IsDirty = false;
    }

    public void GenerateMesh()
    {
        var nativeBlocks = new NativeArray<byte>(blocks, Allocator.TempJob);
        var nativeVertices = new NativeList<Vector3>(Allocator.TempJob);
        var nativeSolidTriangles = new NativeList<int>(Allocator.TempJob);
        var nativeWaterTriangles = new NativeList<int>(Allocator.TempJob);
        var nativeUvs = new NativeList<Vector2>(Allocator.TempJob);

        var job = new ChunkMeshJob
        {
            blocks = nativeBlocks,
            vertices = nativeVertices,
            solidTriangles = nativeSolidTriangles,
            waterTriangles = nativeWaterTriangles,
            uvs = nativeUvs
        };

        job.Schedule().Complete();

        var mesh = new Mesh();
        mesh.SetVertices(nativeVertices.AsArray());
        mesh.SetUVs(0, nativeUvs.AsArray());

        mesh.subMeshCount = 2;
        mesh.SetTriangles(nativeSolidTriangles.AsArray().ToArray(), 0);
        mesh.SetTriangles(nativeWaterTriangles.AsArray().ToArray(), 1);

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        if (TryGetComponent(out MeshCollider collider))
        {
            var colliderMesh = new Mesh();

            colliderMesh.SetVertices(nativeVertices.AsArray());
            colliderMesh.SetTriangles(nativeSolidTriangles.AsArray().ToArray(), 0);

            collider.sharedMesh = null;
            collider.sharedMesh = colliderMesh;
        }

        nativeBlocks.Dispose();
        nativeVertices.Dispose();
        nativeSolidTriangles.Dispose();
        nativeWaterTriangles.Dispose();
        nativeUvs.Dispose();
    }

    void GenerateBlocks()
    {
        ForEachColumn(GenerateColumn);
    }

    void GenerateColumn(int x, int z)
    {
        var noise = Mathf.PerlinNoise((WorldX + x + worldSeed) * terrainScale, (WorldZ + z + worldSeed) * terrainScale);
        var height = Mathf.FloorToInt(noise * terrainHeight) + 4;

        for (var y = 0; y < Size; y++) SetBlockLocal(x, y, z, GetTerrainBlock(y, height));
    }

    byte GetTerrainBlock(int y, int height)
    {
        if (y > height && y <= waterLevel) return Water;
        if (y > height) return Air;

        if (y == height)
            return height < waterLevel ? Dirt : Grass;

        if (y >= height - dirtDepth) return Dirt;
        return Stone;
    }

    void ForEachColumn(System.Action<int, int> action)
    {
        for (var x = 0; x < Size; x++)
            for (var z = 0; z < Size; z++)
                action(x, z);
    }

    bool InBounds(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < Size && y < Size && z < Size;

    static int Index(int x, int y, int z) => x + Size * (y + Size * z);

    public byte GetBlock(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) return 0;
        return blocks[Index(x, y, z)];
    }

    void SetBlockLocal(int x, int y, int z, byte block) => blocks[Index(x, y, z)] = block;
}