using UnityEngine;

public class BlockInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public VoxelWorld world;
    public float range = 6f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) Interact(-1, 0);
        if (Input.GetMouseButtonDown(1)) Interact(1, 1);
    }

    void Interact(float direction, byte block)
    {
        var cameraTransform = playerCamera.transform;

        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, range)) return;
        if (!hit.collider.GetComponent<VoxelChunk>()) return;

        var pos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.01f * direction);
        world.SetBlockWorld(pos, block);
    }
}