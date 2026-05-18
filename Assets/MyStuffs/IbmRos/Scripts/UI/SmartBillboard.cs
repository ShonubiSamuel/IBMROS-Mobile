using UnityEngine;

public class SmartBillboard : MonoBehaviour
{
    // The ObjectScaler will tell us: "I want to align with THIS direction"
    // e.g. The Forward vector or Right vector
    public Vector3 targetAlignDirection = Vector3.zero;

    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera == null) return;

        // 1. Face the camera (Standard Billboard)
        // This ensures the sprite is flat against the screen
        transform.rotation = _mainCamera.transform.rotation;

        // 2. Spin to match the line (Smart Alignment)
        if (targetAlignDirection != Vector3.zero)
        {
            AlignSpriteToScreenLine();
        }
    }

    void AlignSpriteToScreenLine()
    {
        // A. Get two points along the 3D line we want to mimic
        // Point 1: The handle's center
        Vector3 point1 = transform.position;
        // Point 2: A little bit along the target direction
        Vector3 point2 = transform.position + targetAlignDirection;

        // B. Project both points to 2D Screen Space
        Vector3 screen1 = _mainCamera.WorldToScreenPoint(point1);
        Vector3 screen2 = _mainCamera.WorldToScreenPoint(point2);

        // Safety: If points are behind camera, stop to avoid flipping
        if (screen1.z < 0 || screen2.z < 0) return;

        // C. Calculate the angle between these two dots on your screen
        Vector2 screenDir = (screen2 - screen1).normalized;
        float angle = Mathf.Atan2(screenDir.y, screenDir.x) * Mathf.Rad2Deg;

        // D. Apply that angle as a local "Roll" (Z-Rotation)
        // We add this to the camera-facing rotation we set earlier
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}