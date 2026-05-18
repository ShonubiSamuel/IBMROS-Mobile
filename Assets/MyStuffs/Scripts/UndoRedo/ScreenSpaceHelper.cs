using UnityEngine;

public static class ScreenSpaceHelper
{
    public struct PanelDimensions
    {
        public readonly float HalfWidth;
        public readonly float HalfHeight;
        public PanelDimensions(Rect rect)
        {
            HalfWidth = rect.width * 0.5f;
            HalfHeight = rect.height * 0.5f;
        }
    }

    public struct ObjectScreenBounds
    {
        public readonly float TopY;
        public readonly float BottomY;
        public readonly float CenterX;
        public ObjectScreenBounds(float topY, float bottomY, float centerX)
        {
            TopY = topY;
            BottomY = bottomY;
            CenterX = centerX;
        }
    }

    // Cache the 8 corners to avoid garbage collection
    private static Vector3[] _corners = new Vector3[8];

    public static bool TryGetScreenSpaceBounds(Bounds bounds, Camera camera, out ObjectScreenBounds screenBounds)
    {
        // 1. Get the 8 corners of the World Space AABB
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents;

        _corners[0] = center + new Vector3(ext.x, ext.y, ext.z);
        _corners[1] = center + new Vector3(ext.x, ext.y, -ext.z);
        _corners[2] = center + new Vector3(ext.x, -ext.y, ext.z);
        _corners[3] = center + new Vector3(ext.x, -ext.y, -ext.z);
        _corners[4] = center + new Vector3(-ext.x, ext.y, ext.z);
        _corners[5] = center + new Vector3(-ext.x, ext.y, -ext.z);
        _corners[6] = center + new Vector3(-ext.x, -ext.y, ext.z);
        _corners[7] = center + new Vector3(-ext.x, -ext.y, -ext.z);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        bool anyPointOnScreen = false;

        // 2. Project all 8 corners to Screen Space
        for (int i = 0; i < 8; i++)
        {
            Vector3 screenPos = camera.WorldToScreenPoint(_corners[i]);

            // Handle points behind the camera
            if (screenPos.z < 0)
            {
                // If a point is behind us, it "flips".
                // Dealing with partial clipping is complex, but for UI panels, 
                // we typically just want to know if the *bulk* is visible.
                // We'll mark it invalid for Min/Max calculation to avoid flipping issues.
                continue; 
            }

            anyPointOnScreen = true;
            if (screenPos.x < minX) minX = screenPos.x;
            if (screenPos.x > maxX) maxX = screenPos.x;
            if (screenPos.y < minY) minY = screenPos.y;
            if (screenPos.y > maxY) maxY = screenPos.y;
        }

        if (!anyPointOnScreen)
        {
            screenBounds = default;
            return false;
        }

        // 3. Create the bounds from the Extremes
        float centerX = (minX + maxX) / 2f;
        screenBounds = new ObjectScreenBounds(maxY, minY, centerX);
        return true;
    }

    public static void CalculatePanelPositions(
        ObjectScreenBounds screenBounds,
        Rect safeArea, 
        float paddingTop,      
        float paddingBottom,   
        float minVerticalSpacing,
        PanelDimensions panelAbove,
        PanelDimensions panelBelow,
        out Vector2 finalPosAbove,
        out Vector2 finalPosBelow)
    {
        // 1. Horizontal (X)
        float maxHalfWidth = Mathf.Max(panelAbove.HalfWidth, panelBelow.HalfWidth);
        float clampedX = Mathf.Clamp(screenBounds.CenterX, 
                                    safeArea.xMin + maxHalfWidth, 
                                    safeArea.xMax - maxHalfWidth);

        // 2. Top Panel Y (Anchor)
        // Use the calculated Highest Y pixel + padding
        float targetYAbove = screenBounds.TopY + paddingTop;
        
        targetYAbove = Mathf.Clamp(targetYAbove, 
                                   safeArea.yMin + panelAbove.HalfHeight, 
                                   safeArea.yMax - panelAbove.HalfHeight);
                                   
        // 3. Bottom Panel Y (Follower)
        // Use the calculated Lowest Y pixel - padding
        float idealYBelow = screenBounds.BottomY - paddingBottom;
        
        // Constraints
        float floorY = safeArea.yMin + panelBelow.HalfHeight;
        float topPanelBottomEdge = targetYAbove - panelAbove.HalfHeight;
        float ceilingY = topPanelBottomEdge - minVerticalSpacing - panelBelow.HalfHeight;

        float targetYBelow = idealYBelow;
        
        // Collision check
        targetYBelow = Mathf.Min(targetYBelow, ceilingY);
        
        // Floor check
        targetYBelow = Mathf.Max(targetYBelow, floorY);
        
        // 4. Output
        finalPosAbove = new Vector2(clampedX, targetYAbove);
        finalPosBelow = new Vector2(clampedX, targetYBelow);
    }
}