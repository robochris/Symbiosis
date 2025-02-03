using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 10f;
    [SerializeField] private Camera playerCamera; // The camera that orbits the player

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (!playerCamera)
        {
            playerCamera = Camera.main; // Fallback if not assigned
        }
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Flatten the camera's forward/right so we don't tilt up/down
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Movement direction relative to the camera
        Vector3 moveDir = (camForward * vertical) + (camRight * horizontal);

        // Rotate the player only if moving
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Move the player
        controller.Move(moveDir * speed * Time.deltaTime);
    }
}
