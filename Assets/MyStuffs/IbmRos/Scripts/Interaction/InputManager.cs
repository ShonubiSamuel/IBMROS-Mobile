using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    // World interaction events (UI touches are filtered out)
    public event Action<Vector2> OnPointerDown;
    public event Action<Vector2> OnPointerUp;
    public event Action<Vector2> OnPointerMove;
    public event Action<Vector2> OnPointerClick;

    // Pinch events for two-finger zoom
    public event Action<float> OnPinchStart;
    public event Action<float> OnPinchDelta;
    public event Action OnPinchEnd;

    [Header("Settings")]
    [SerializeField] private float clickMoveThreshold = 10f;

    // Touch state
    private bool _trackingTouch = false;
    private bool _touchHasMoved = false;
    private Vector2 _pointerDownPosition;
    private bool _pinchActive = false;
    private float _lastPinchDistance = 0f;

    // Editor mouse state
#if UNITY_EDITOR
    private bool _mouseDown = false;
    private bool _mouseHasMoved = false;
    private Vector2 _mouseDownPosition;
#endif

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    // TOUCH INPUT

    private void HandleTouchInput()
    {
        var activeTouches = Touch.activeTouches;

        // TWO FINGER PINCH
        if (activeTouches.Count == 2)
        {
            HandlePinch(activeTouches[0].screenPosition, activeTouches[1].screenPosition);
            CancelSingleTouch();
            return;
        }

        // END PINCH
        if (_pinchActive && activeTouches.Count < 2)
        {
            _pinchActive = false;
            OnPinchEnd?.Invoke();
        }

        if (activeTouches.Count == 0)
        {
            _trackingTouch = false;
            return;
        }

        // SINGLE TOUCH
        var touch = activeTouches[0];

        if (IsPointerOverUI(touch.screenPosition))
        {
            if (touch.phase == TouchPhase.Began)
            {
                _trackingTouch = false;
                return;
            }
        }

        if (touch.phase == TouchPhase.Began)
        {
            _trackingTouch = true;
            _touchHasMoved = false;
            _pointerDownPosition = touch.screenPosition;
            OnPointerDown?.Invoke(touch.screenPosition);
        }

        if (!_trackingTouch)
            return;

        if (touch.phase == TouchPhase.Moved)
        {
            float distance = Vector2.Distance(touch.screenPosition, _pointerDownPosition);
            if (distance > clickMoveThreshold)
                _touchHasMoved = true;

            OnPointerMove?.Invoke(touch.screenPosition);
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (!_touchHasMoved)
                OnPointerClick?.Invoke(touch.screenPosition);

            OnPointerUp?.Invoke(touch.screenPosition);
            _trackingTouch = false;
        }
    }

    private void HandlePinch(Vector2 pos1, Vector2 pos2)
    {
        float currentDistance = Vector2.Distance(pos1, pos2);

        if (!_pinchActive)
        {
            _pinchActive = true;
            _lastPinchDistance = currentDistance;
            OnPinchStart?.Invoke(currentDistance);
            return;
        }

        float delta = currentDistance - _lastPinchDistance;
        _lastPinchDistance = currentDistance;
        OnPinchDelta?.Invoke(delta);
    }

    private void CancelSingleTouch()
    {
        if (_trackingTouch)
        {
            _trackingTouch = false;
            _touchHasMoved = false;
        }
    }

    // EDITOR MOUSE INPUT

#if UNITY_EDITOR
    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mousePosition = mouse.position.ReadValue();
        
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Raycast to see what UI element is under the pointer
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            var pointerData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };
            EventSystem.current.RaycastAll(pointerData, results);
        }

        // MOUSE DOWN
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI(mousePosition))
                return;

            _mouseDown = true;
            _mouseHasMoved = false;
            _mouseDownPosition = mousePosition;
            OnPointerDown?.Invoke(mousePosition);
        }

        if (!_mouseDown)
            return;

        // MOUSE MOVE
        if (mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.01f)
            {
                float distance = Vector2.Distance(mousePosition, _mouseDownPosition);
                if (distance > clickMoveThreshold)
                    _mouseHasMoved = true;

                OnPointerMove?.Invoke(mousePosition);
            }
        }

        // MOUSE UP
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (!_mouseHasMoved)
                OnPointerClick?.Invoke(mousePosition);

            OnPointerUp?.Invoke(mousePosition);
            _mouseDown = false;
        }

        // SCROLL WHEEL as pinch simulation in Editor
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            OnPinchDelta?.Invoke(scroll * 10f);
    }
#endif

    // UI CHECK

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

#if UNITY_EDITOR
        return EventSystem.current.IsPointerOverGameObject(-1);
#else
    var activeTouches = Touch.activeTouches;
    foreach (var touch in activeTouches)
    {
        if (EventSystem.current.IsPointerOverGameObject(touch.touchId))
            return true;
    }
    return false;
#endif
    }
    
}