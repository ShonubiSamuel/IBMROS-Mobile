using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Settings")]
    public float handleRange = 100f; // Pixels the knob can move
    public float deadZone = 0.1f;    // Minimum input to trigger movement

    [Header("References")]
    public RectTransform background;
    public RectTransform handle;
    
    // --- OUTPUT ---
    // Read this from your CameraController! 
    // X = Horizontal, Y = Vertical. Range: -1 to 1.
    public Vector2 InputVector { get; private set; }

    private Vector2 _initialPos;

    void Start()
    {
        // Force handle to center regardless of where it was placed in Editor
        handle.anchoredPosition = Vector2.zero;
        _initialPos = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position))
        {
            // Clamp to circle boundary
            Vector2 direction = Vector2.ClampMagnitude(position, handleRange);

            InputVector = direction / handleRange;

            if (InputVector.magnitude < deadZone)
                InputVector = Vector2.zero;

            // Move handle relative to background center
            handle.anchoredPosition = direction;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

}