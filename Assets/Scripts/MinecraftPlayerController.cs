using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MinecraftPlayerController : MonoBehaviour
{
    public CharacterController controller;
    public Camera playerCamera;

    public float moveSpeed = 6f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    public VoxelWorld world;

    public float swimSpeed = 2.5f;
    public float waterGravity = -1.5f;
    public float swimUpSpeed = 4f;
    public float maxWaterFallSpeed = -1.5f;

    const byte Air = 0;
    const byte Grass = 1;
    const byte Dirt = 2;
    const byte Stone = 3;
    const byte Water = 4;

    float cameraPitch;
    float verticalVelocity;

    void Awake()
    {
        LockCursor();
    }

    void Update()
    {
        LookAround();
        Move();
    }

    void LookAround()
    {
        var mouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
        transform.Rotate(Vector3.up * mouse.x);

        cameraPitch = Mathf.Clamp(cameraPitch - mouse.y, -90f, 90f);
        playerCamera.transform.localEulerAngles = Vector3.right * cameraPitch;
    }

    void Move()
    {
        var inWater = IsInWater();
        var speed = inWater ? swimSpeed : moveSpeed;
        var currentGravity = inWater ? waterGravity : gravity;

        var grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0) verticalVelocity = -2f;

        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        var move = transform.right * input.x + transform.forward * input.y;

        controller.Move(move * speed * Time.deltaTime);

        if (inWater)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, maxWaterFallSpeed);

            if (Input.GetButton("Jump"))
                verticalVelocity = CanJumpOutOfWater() ? 6f : swimUpSpeed;
        }
        else if (grounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += currentGravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    bool IsInWater()
    {
        var pos = Vector3Int.FloorToInt(transform.position);
        return world.GetBlockWorld(pos) == Water;
    }

    bool CanJumpOutOfWater()
    {
        var feet = Vector3Int.FloorToInt(transform.position);
        var head = Vector3Int.FloorToInt(transform.position + Vector3.up * 1.5f);

        return world.GetBlockWorld(feet) == Water &&
               world.GetBlockWorld(head) == Air;
    }
}