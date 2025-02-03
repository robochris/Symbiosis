using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The target (usually the player) to follow.")]
    public Transform target;
    [Tooltip("Default offset from the target.")]
    public Vector3 offset = new Vector3(0f, 5f, -7f);
    [Tooltip("How quickly the camera moves.")]
    public float smoothSpeed = 0.125f;
    [Tooltip("Rotation speed based on mouse input.")]
    public float rotationSpeed = 3f;

    private Vector3 currentOffset;

    void Start()
    {
        if (target != null)
        {
            currentOffset = offset;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Rotate the offset around the target using mouse horizontal input.
        float horizontalInput = Input.GetAxis("Mouse X") * rotationSpeed;
        currentOffset = Quaternion.AngleAxis(horizontalInput, Vector3.up) * currentOffset;

        // (Optional) Add vertical rotation if needed (with clamping to prevent flipping)

        // Calculate desired position and smooth the movement.
        Vector3 desiredPosition = target.position + currentOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Always look at the target.
        transform.LookAt(target);
    }
}
