using UnityEngine;
using UnityEngine.UI;

public class UnderwaterEffect : MonoBehaviour
{
    public VoxelWorld world;
    public Image overlay;

    const byte Water = 4;

    void Update()
    {
        var pos = Vector3Int.FloorToInt(transform.position);
        overlay.enabled = world.GetBlockWorld(pos) == Water;
    }
}