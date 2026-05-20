using UnityEngine;

public class BlockInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 6f;

    public VoxelWorld world;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BreakBlock();

        if (Input.GetMouseButtonDown(1))
            PlaceBlock();
    }

    void BreakBlock()
    {
        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, range))
            return;

        VoxelChunk chunk = hit.collider.GetComponent<VoxelChunk>();
        if (chunk == null)
            return;

        Vector3 worldBlockPos = hit.point - hit.normal * 0.01f;
        Vector3 localBlockPos = worldBlockPos - chunk.transform.position;

        Vector3Int pos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
        world.SetBlockWorld(pos, 0);
    }

    void PlaceBlock()
    {
        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, range))
            return;

        VoxelChunk chunk = hit.collider.GetComponent<VoxelChunk>();
        if (chunk == null)
            return;

        Vector3 worldBlockPos = hit.point + hit.normal * 0.01f;
        Vector3 localBlockPos = worldBlockPos - chunk.transform.position;

        Vector3Int pos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.01f);
        world.SetBlockWorld(pos, 1);
    }
}