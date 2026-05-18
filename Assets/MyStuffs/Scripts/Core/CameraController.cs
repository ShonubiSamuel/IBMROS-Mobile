using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    public JoystickController movementJoystick; // Drag your Joystick here!

    [Header("Settings")]
    public float rotationSpeed = 2f;
    public float movementSpeed = 5f; // Walking speed
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 80f;
    public float fov = 75f;

    // State
    private float _currentX = 0f;
    private float _currentY = 45f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = fov;

        Vector3 angles = transform.eulerAngles;
        _currentX = angles.y;
        _currentY = angles.x;
    }

    public void RotateCamera(Vector2 delta)
    {
        _currentX -= delta.x * rotationSpeed * 0.1f;
        _currentY -= delta.y * rotationSpeed * 0.1f;
        _currentY = Mathf.Clamp(_currentY, minVerticalAngle, maxVerticalAngle);
    }

    void Update()
    {
        // 1. Handle Rotation (From logic calculated in RotateCamera)
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
        transform.rotation = rotation;

        // 2. Handle Movement (From Joystick)
        if (movementJoystick != null)
        {
            Vector2 input = movementJoystick.InputVector;

            if (input.sqrMagnitude > 0.01f)
            {
                // Calculate Forward and Right vectors relative to the camera's YAW (Horizontal rotation)
                // We ignore Pitch (X rotation) so we don't fly into the ground when looking down.
                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();

                // Calculate Move Direction
                Vector3 moveDir = (forward * input.y + right * input.x).normalized;

                // Apply Movement
                transform.position += moveDir * movementSpeed * Time.deltaTime;
            }
        }
    }
}