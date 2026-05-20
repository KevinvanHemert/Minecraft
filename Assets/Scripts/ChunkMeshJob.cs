using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct ChunkMeshJob : IJob
{
    public const int Size = 16;
    const int AtlasSize = 4;
    const float UvPadding = 0.001f;

    [ReadOnly] public NativeArray<byte> blocks;

    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector2> uvs;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
            for (var y = 0; y < Size; y++)
                for (var z = 0; z < Size; z++)
                {
                    var block = GetBlock(x, y, z);
                    if (block == 0) continue;

                    AddVisibleFaces(x, y, z, block);
                }
    }

    void AddVisibleFaces(int x, int y, int z, byte block)
    {
        var pos = new float3(x, y, z);

        if (IsAir(x, y + 1, z)) AddFace(pos, 0, block);
        if (IsAir(x, y - 1, z)) AddFace(pos, 1, block);
        if (IsAir(x + 1, y, z)) AddFace(pos, 2, block);
        if (IsAir(x - 1, y, z)) AddFace(pos, 3, block);
        if (IsAir(x, y, z + 1)) AddFace(pos, 4, block);
        if (IsAir(x, y, z - 1)) AddFace(pos, 5, block);
    }

    void AddFace(float3 pos, int direction, byte block)
    {
        var start = vertices.Length;

        for (var i = 0; i < 4; i++)
            vertices.Add(pos + GetFaceVertex(direction, i));

        var tile = GetTile(block, direction);
        AddUVs(tile.x, tile.y);

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    void AddUVs(int tileX, int tileY)
    {
        var tileSize = 1f / AtlasSize;
        tileY = AtlasSize - 1 - tileY;

        var xMin = tileX * tileSize + UvPadding;
        var yMin = tileY * tileSize + UvPadding;
        var xMax = xMin + tileSize - UvPadding * 2;
        var yMax = yMin + tileSize - UvPadding * 2;

        uvs.Add(new Vector2(xMin, yMin));
        uvs.Add(new Vector2(xMin, yMax));
        uvs.Add(new Vector2(xMax, yMax));
        uvs.Add(new Vector2(xMax, yMin));
    }

    int2 GetTile(byte block, int direction)
    {
        if (block == 1 && direction == 0) return new int2(0, 0);
        if (block == 1 && direction == 1) return new int2(1, 0);
        if (block == 1) return new int2(2, 0);
        if (block == 2) return new int2(1, 0);
        if (block == 3) return new int2(3, 0);

        return int2.zero;
    }

    bool IsAir(int x, int y, int z) => !InBounds(x, y, z) || GetBlock(x, y, z) == 0;

    byte GetBlock(int x, int y, int z) => blocks[Index(x, y, z)];

    static bool InBounds(int x, int y, int z) =>
        x >= 0 && y >= 0 && z >= 0 &&
        x < Size && y < Size && z < Size;

    static int Index(int x, int y, int z) => x + Size * (y + Size * z);

    static float3 GetFaceVertex(int direction, int index)
    {
        return direction switch
        {
            0 => index switch // up
            {
                0 => new float3(0, 1, 1),
                1 => new float3(0, 1, 0),
                2 => new float3(1, 1, 0),
                _ => new float3(1, 1, 1)
            },
            1 => index switch // down
            {
                0 => new float3(0, 0, 0),
                1 => new float3(0, 0, 1),
                2 => new float3(1, 0, 1),
                _ => new float3(1, 0, 0)
            },
            2 => index switch // right
            {
                0 => new float3(1, 0, 1),
                1 => new float3(1, 1, 1),
                2 => new float3(1, 1, 0),
                _ => new float3(1, 0, 0)
            },
            3 => index switch // left
            {
                0 => new float3(0, 0, 0),
                1 => new float3(0, 1, 0),
                2 => new float3(0, 1, 1),
                _ => new float3(0, 0, 1)
            },
            4 => index switch // forward
            {
                0 => new float3(0, 0, 1),
                1 => new float3(0, 1, 1),
                2 => new float3(1, 1, 1),
                _ => new float3(1, 0, 1)
            },
            _ => index switch // back
            {
                0 => new float3(1, 0, 0),
                1 => new float3(1, 1, 0),
                2 => new float3(0, 1, 0),
                _ => new float3(0, 0, 0)
            }
        };
    }
}