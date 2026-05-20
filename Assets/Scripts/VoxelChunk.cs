using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public int worldSeed = 12345;


    public bool IsDirty { get; private set; }

    public const int Size = 16;
    const int AtlasSize = 4;

    private byte[,,] blocks = new byte[Size, Size, Size];

    readonly List<Vector3> vertices = new();
    readonly List<int> triangles = new();
    readonly List<Vector2> uvs = new();

    public int terrainHeight = 8;
    public float terrainScale = 0.08f;
    public int dirtDepth = 3;

    public Vector2Int chunkCoord;
    public int worldXOffset => chunkCoord.x * Size;
    public int worldZOffset => chunkCoord.y * Size;


    private bool hasExternalBlocks;

    public void Initialize(bool generateTerrain)
    {
        if (generateTerrain)
            GenerateBlocks();

        GenerateMesh();
    }

    void GenerateBlocks()
    {
        for (int x = 0; x < Size; x++)
            for (int z = 0; z < Size; z++)
            {
                float worldX = worldXOffset + x + worldSeed;
                float worldZ = worldZOffset + z + worldSeed;
                float noise = Mathf.PerlinNoise(worldX * terrainScale, worldZ * terrainScale);

                int height = Mathf.FloorToInt(noise * terrainHeight) + 4;

                for (int y = 0; y < Size; y++)
                {
                    if (y > height)
                    {
                        blocks[x, y, z] = 0; // air
                    }
                    else if (y == height)
                    {
                        blocks[x, y, z] = 1; // grass
                    }
                    else if (y >= height - dirtDepth)
                    {
                        blocks[x, y, z] = 2; // dirt
                    }
                    else
                    {
                        blocks[x, y, z] = 3; // stone
                    }
                }
            }
    }

    public void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                {
                    if (blocks[x, y, z] == 0)
                        continue;

                    AddVisibleFaces(x, y, z);
                }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        if (TryGetComponent(out MeshCollider collider))
        {
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
        }
    }

    void AddVisibleFaces(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z);
        byte block = blocks[x, y, z];

        if (IsAir(x, y + 1, z)) AddFace(pos, Vector3.up, block);
        if (IsAir(x, y - 1, z)) AddFace(pos, Vector3.down, block);
        if (IsAir(x + 1, y, z)) AddFace(pos, Vector3.right, block);
        if (IsAir(x - 1, y, z)) AddFace(pos, Vector3.left, block);
        if (IsAir(x, y, z + 1)) AddFace(pos, Vector3.forward, block);
        if (IsAir(x, y, z - 1)) AddFace(pos, Vector3.back, block);
    }

    bool IsAir(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size)
            return true;

        return blocks[x, y, z] == 0;
    }

    void AddFace(Vector3 pos, Vector3 direction, byte block)
    {
        int start = vertices.Count;

        Vector3[] face = GetFaceVertices(direction);

        for (int i = 0; i < 4; i++)
            vertices.Add(pos + face[i]);

        Vector2Int tile = GetTile(block, direction);
        AddUVs(tile.x, tile.y);

        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start + 0);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }

    Vector3[] GetFaceVertices(Vector3 direction)
    {
        // Vertices are ordered as:
        // bottom-left, top-left, top-right, bottom-right
        // as seen from OUTSIDE the block.

        if (direction == Vector3.forward)
            return new[]
            {
            new Vector3(0,0,1),
            new Vector3(0,1,1),
            new Vector3(1,1,1),
            new Vector3(1,0,1)
        };

        if (direction == Vector3.back)
            return new[]
            {
            new Vector3(1,0,0),
            new Vector3(1,1,0),
            new Vector3(0,1,0),
            new Vector3(0,0,0)
        };

        if (direction == Vector3.right)
            return new[]
            {
            new Vector3(1,0,1),
            new Vector3(1,1,1),
            new Vector3(1,1,0),
            new Vector3(1,0,0)
        };

        if (direction == Vector3.left)
            return new[]
            {
            new Vector3(0,0,0),
            new Vector3(0,1,0),
            new Vector3(0,1,1),
            new Vector3(0,0,1)
        };

        if (direction == Vector3.up)
            return new[]
            {
            new Vector3(0,1,1),
            new Vector3(0,1,0),
            new Vector3(1,1,0),
            new Vector3(1,1,1)
        };

        // down
        return new[]
        {
        new Vector3(0,0,0),
        new Vector3(0,0,1),
        new Vector3(1,0,1),
        new Vector3(1,0,0)
    };
    }

    Vector2Int GetTile(byte block, Vector3 direction)
    {
        // tile positions in the atlas
        // bottom-left is 0,0

        if (block == 1) // grass
        {
            if (direction == Vector3.up)
                return new Vector2Int(0, 0); // grass top

            if (direction == Vector3.down)
                return new Vector2Int(1, 0); // dirt

            return new Vector2Int(2, 0); // grass side
        }

        if (block == 2)
            return new Vector2Int(1, 0); // dirt

        if (block == 3)
            return new Vector2Int(3, 0); // stone

        return new Vector2Int(0, 0);
    }

    void AddUVs(int tileX, int tileY)
    {
        float tileSize = 1f / AtlasSize;
        float padding = 0.001f;

        tileY = AtlasSize - 1 - tileY;

        float xMin = tileX * tileSize + padding;
        float yMin = tileY * tileSize + padding;
        float xMax = xMin + tileSize - padding * 2;
        float yMax = yMin + tileSize - padding * 2;

        uvs.Add(new Vector2(xMin, yMin)); // bottom-left
        uvs.Add(new Vector2(xMin, yMax)); // top-left
        uvs.Add(new Vector2(xMax, yMax)); // top-right
        uvs.Add(new Vector2(xMax, yMin)); // bottom-right
    }

    public void SetBlock(int x, int y, int z, byte block)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size)
            return;

        if (blocks[x, y, z] == block)
            return;

        blocks[x, y, z] = block;
        IsDirty = true;

        Debug.Log($"Chunk {chunkCoord} changed. Dirty = {IsDirty}");

        GenerateMesh();
    }

    public byte[,,] CopyBlocks()
    {
        return (byte[,,])blocks.Clone();
    }

    public void SetBlocks(byte[,,] newBlocks)
    {
        blocks = (byte[,,])newBlocks.Clone();
        IsDirty = false;
    }
}