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
        var grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0) verticalVelocity = -2f;

        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        var move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (grounded && Input.GetButtonDown("Jump")) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}