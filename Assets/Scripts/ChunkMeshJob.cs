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
    public NativeList<int> solidTriangles;
    public NativeList<int> waterTriangles;
    public NativeList<Vector2> uvs;

    const float WaterSurfaceHeight = 0.9f;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
            for (var y = 0; y < Size; y++)
                for (var z = 0; z < Size; z++)
                {
                    var block = GetBlock(x, y, z);
                    if (block == (byte)BlockType.Air) continue;

                    AddVisibleFaces(x, y, z, block);
                }
    }

    void AddVisibleFaces(int x, int y, int z, byte block)
    {
        var pos = new float3(x, y, z);

        if (ShouldRenderFace(block, x, y + 1, z)) AddFace(pos, 0, block);
        if (ShouldRenderFace(block, x, y - 1, z)) AddFace(pos, 1, block);
        if (ShouldRenderFace(block, x + 1, y, z)) AddFace(pos, 2, block);
        if (ShouldRenderFace(block, x - 1, y, z)) AddFace(pos, 3, block);
        if (ShouldRenderFace(block, x, y, z + 1)) AddFace(pos, 4, block);
        if (ShouldRenderFace(block, x, y, z - 1)) AddFace(pos, 5, block);
    }

    bool ShouldRenderFace(byte block, int x, int y, int z)
    {
        if (!InBounds(x, y, z))
            return block != (byte)BlockType.Water;

        var neighbor = GetBlock(x, y, z);

        if (neighbor == (byte)BlockType.Air) return true;
        if (block != (byte)BlockType.Water && neighbor == (byte)BlockType.Water) return true;

        return false;
    }

    void AddFace(float3 pos, int direction, byte block)
    {
        var start = vertices.Length;

        for (var i = 0; i < 4; i++)
            vertices.Add(pos + GetFaceVertex(direction, i, block));

        var tile = GetTile(block, direction);
        AddUVs(tile.x, tile.y);
        AddTriangles(start, block == (byte)BlockType.Water ? waterTriangles : solidTriangles);
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

    void AddTriangles(int start, NativeList<int> target)
    {
        target.Add(start);
        target.Add(start + 2);
        target.Add(start + 1);

        target.Add(start);
        target.Add(start + 3);
        target.Add(start + 2);
    }

    int2 GetTile(byte block, int direction)
    {
        if (block == (byte)BlockType.Grass && direction == 0) return new int2(0, 0);
        if (block == (byte)BlockType.Grass && direction == 1) return new int2(1, 0);
        if (block == (byte)BlockType.Grass) return new int2(2, 0);
        if (block == (byte)BlockType.Dirt) return new int2(1, 0);
        if (block == (byte)BlockType.Stone) return new int2(3, 0);
        if (block == (byte)BlockType.Water) return new int2(1, 2);

        return int2.zero;
    }

    byte GetBlock(int x, int y, int z) => blocks[Index(x, y, z)];

    static bool InBounds(int x, int y, int z) =>
        x >= 0 && y >= 0 && z >= 0 &&
        x < Size && y < Size && z < Size;

    static int Index(int x, int y, int z) => x + Size * (y + Size * z);

    static float3 GetFaceVertex(int direction, int index, byte block)
    {
        var top = block == (byte)BlockType.Water ? WaterSurfaceHeight : 1f;

        return direction switch
        {
            0 => index switch // up
            {
                0 => new float3(0, top, 1),
                1 => new float3(0, top, 0),
                2 => new float3(1, top, 0),
                _ => new float3(1, top, 1)
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
                1 => new float3(1, top, 1),
                2 => new float3(1, top, 0),
                _ => new float3(1, 0, 0)
            },
            3 => index switch // left
            {
                0 => new float3(0, 0, 0),
                1 => new float3(0, top, 0),
                2 => new float3(0, top, 1),
                _ => new float3(0, 0, 1)
            },
            4 => index switch // forward
            {
                0 => new float3(0, 0, 1),
                1 => new float3(0, top, 1),
                2 => new float3(1, top, 1),
                _ => new float3(1, 0, 1)
            },
            _ => index switch // back
            {
                0 => new float3(1, 0, 0),
                1 => new float3(1, top, 0),
                2 => new float3(0, top, 0),
                _ => new float3(0, 0, 0)
            }
        };
    }
}