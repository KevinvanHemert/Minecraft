using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public const int Size = 16;

    const int AtlasSize = 4;
    const float UvPadding = 0.001f;

    public int worldSeed = 12345;
    public int terrainHeight = 8;
    public float terrainScale = 0.08f;
    public int dirtDepth = 3;

    public Vector2Int chunkCoord;

    public bool IsDirty { get; private set; }

    byte[,,] blocks = new byte[Size, Size, Size];

    readonly List<Vector3> vertices = new();
    readonly List<int> triangles = new();
    readonly List<Vector2> uvs = new();

    int WorldX => chunkCoord.x * Size;
    int WorldZ => chunkCoord.y * Size;

    public void Initialize(bool generateTerrain)
    {
        if (generateTerrain) GenerateBlocks();
        GenerateMesh();
    }

    public void SetBlock(int x, int y, int z, byte block)
    {
        if (!InBounds(x, y, z) || blocks[x, y, z] == block) return;

        blocks[x, y, z] = block;
        IsDirty = true;

        GenerateMesh();
    }

    public byte[,,] CopyBlocks() => (byte[,,])blocks.Clone();

    public void SetBlocks(byte[,,] newBlocks)
    {
        blocks = (byte[,,])newBlocks.Clone();
        IsDirty = false;
    }

    public void GenerateMesh()
    {
        ClearMeshData();
        ForEachBlock((x, y, z) => { if (blocks[x, y, z] != 0) AddVisibleFaces(x, y, z); });
        ApplyMesh();
    }

    void GenerateBlocks()
    {
        ForEachColumn(GenerateColumn);
    }

    void GenerateColumn(int x, int z)
    {
        var noise = Mathf.PerlinNoise((WorldX + x + worldSeed) * terrainScale, (WorldZ + z + worldSeed) * terrainScale);
        var height = Mathf.FloorToInt(noise * terrainHeight) + 4;

        for (var y = 0; y < Size; y++) blocks[x, y, z] = GetTerrainBlock(y, height);
    }

    byte GetTerrainBlock(int y, int height)
    {
        if (y > height) return 0;
        if (y == height) return 1;
        if (y >= height - dirtDepth) return 2;
        return 3;
    }

    void AddVisibleFaces(int x, int y, int z)
    {
        var pos = new Vector3(x, y, z);
        var block = blocks[x, y, z];

        if (IsAir(x, y + 1, z)) AddFace(pos, Vector3.up, block);
        if (IsAir(x, y - 1, z)) AddFace(pos, Vector3.down, block);
        if (IsAir(x + 1, y, z)) AddFace(pos, Vector3.right, block);
        if (IsAir(x - 1, y, z)) AddFace(pos, Vector3.left, block);
        if (IsAir(x, y, z + 1)) AddFace(pos, Vector3.forward, block);
        if (IsAir(x, y, z - 1)) AddFace(pos, Vector3.back, block);
    }

    void AddFace(Vector3 pos, Vector3 direction, byte block)
    {
        var start = vertices.Count;
        var face = GetFaceVertices(direction);

        for (var i = 0; i < 4; i++) vertices.Add(pos + face[i]);

        var tile = GetTile(block, direction);
        AddUVs(tile.x, tile.y);
        AddTriangles(start);
    }

    void AddTriangles(int start)
    {
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

    void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    void ApplyMesh()
    {
        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        if (!TryGetComponent(out MeshCollider collider)) return;

        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
    }

    Vector2Int GetTile(byte block, Vector3 direction)
    {
        if (block == 1 && direction == Vector3.up) return new Vector2Int(0, 0);
        if (block == 1 && direction == Vector3.down) return new Vector2Int(1, 0);
        if (block == 1) return new Vector2Int(2, 0);
        if (block == 2) return new Vector2Int(1, 0);
        if (block == 3) return new Vector2Int(3, 0);
        return Vector2Int.zero;
    }

    static readonly Vector3[] ForwardFace = { new(0, 0, 1), new(0, 1, 1), new(1, 1, 1), new(1, 0, 1) };
    static readonly Vector3[] BackFace = { new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), new(0, 0, 0) };
    static readonly Vector3[] RightFace = { new(1, 0, 1), new(1, 1, 1), new(1, 1, 0), new(1, 0, 0) };
    static readonly Vector3[] LeftFace = { new(0, 0, 0), new(0, 1, 0), new(0, 1, 1), new(0, 0, 1) };
    static readonly Vector3[] UpFace = { new(0, 1, 1), new(0, 1, 0), new(1, 1, 0), new(1, 1, 1) };
    static readonly Vector3[] DownFace = { new(0, 0, 0), new(0, 0, 1), new(1, 0, 1), new(1, 0, 0) };

    static Vector3[] GetFaceVertices(Vector3 direction)
    {
        if (direction == Vector3.forward) return ForwardFace;
        if (direction == Vector3.back) return BackFace;
        if (direction == Vector3.right) return RightFace;
        if (direction == Vector3.left) return LeftFace;
        if (direction == Vector3.up) return UpFace;
        return DownFace;
    }

    void ForEachBlock(System.Action<int, int, int> action)
    {
        for (var x = 0; x < Size; x++)
            for (var y = 0; y < Size; y++)
                for (var z = 0; z < Size; z++)
                    action(x, y, z);
    }

    void ForEachColumn(System.Action<int, int> action)
    {
        for (var x = 0; x < Size; x++)
            for (var z = 0; z < Size; z++)
                action(x, z);
    }

    bool IsAir(int x, int y, int z) => !InBounds(x, y, z) || blocks[x, y, z] == 0;

    bool InBounds(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < Size && y < Size && z < Size;
}