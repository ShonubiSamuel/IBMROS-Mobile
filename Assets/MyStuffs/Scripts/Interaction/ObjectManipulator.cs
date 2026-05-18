using UnityEngine;
using System;

public class ObjectManipulator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ObjectDragHandler dragHandler;
    [SerializeField] private ObjectRotationHandler rotationHandler;
    [SerializeField] private ObjectScaleHandler scaleHandler;
    [SerializeField] private CameraController cameraController;

    public event Action OnManipulationStart;
    public event Action OnManipulationEnd;
    public event Action OnCameraRotationStart;
    public event Action OnCameraRotationEnd;

    private bool _isRotatingCamera = false;
    private Vector2 _lastScreenPos;
    private Transform _selectedObject;

    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnPointerDown += HandlePointerDown;
            inputManager.OnPointerMove += HandlePointerMove;
            inputManager.OnPointerUp += HandlePointerUp;
        }

        if (selectionManager != null)
        {
            selectionManager.onObjectSelected += HandleObjectSelected;
            selectionManager.onObjectDeselected += HandleObjectDeselected;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnPointerDown -= HandlePointerDown;
            inputManager.OnPointerMove -= HandlePointerMove;
            inputManager.OnPointerUp -= HandlePointerUp;
        }

        if (selectionManager != null)
        {
            selectionManager.onObjectSelected -= HandleObjectSelected;
            selectionManager.onObjectDeselected -= HandleObjectDeselected;
        }
    }

    private void HandleObjectSelected(Transform target)
    {
        _selectedObject = target;
    }

    private void HandleObjectDeselected()
    {
        _selectedObject = null;
        CancelCameraRotation();
    }

    private void HandlePointerDown(Vector2 screenPosition)
    {
        // Priority 1: Scale handle tap
        if (scaleHandler.TryBeginScale(screenPosition))
        {
            OnManipulationStart?.Invoke();
            return;
        }

        // Priority 2: Drag selected object
        if (dragHandler.TryBeginDrag(screenPosition))
        {
            OnManipulationStart?.Invoke();
            return;
        }

        // Priority 3: Camera rotation
        _isRotatingCamera = true;
        _lastScreenPos = screenPosition;
        dragHandler.SetBlocked(true);
        scaleHandler.SetBlocked(true);
        rotationHandler.SetBlocked(true);
        OnCameraRotationStart?.Invoke();
    }

    private void HandlePointerMove(Vector2 screenPosition)
    {
        if (scaleHandler.IsScaling)
        {
            scaleHandler.UpdateScale(screenPosition);
            return;
        }

        if (dragHandler.IsDragging)
        {
            dragHandler.UpdateDrag(screenPosition);
            return;
        }

        if (_isRotatingCamera)
        {
            Vector2 delta = screenPosition - _lastScreenPos;
            _lastScreenPos = screenPosition;
            cameraController?.RotateCamera(delta);
        }
    }

    private void HandlePointerUp(Vector2 screenPosition)
    {
        if (scaleHandler.IsScaling)
        {
            scaleHandler.EndScale();
            OnManipulationEnd?.Invoke();
            return;
        }

        if (dragHandler.IsDragging)
        {
            dragHandler.EndDrag();
            OnManipulationEnd?.Invoke();
            return;
        }

        if (_isRotatingCamera)
            CancelCameraRotation();
    }

    private void CancelCameraRotation()
    {
        if (!_isRotatingCamera)
            return;

        _isRotatingCamera = false;
        dragHandler.SetBlocked(false);
        scaleHandler.SetBlocked(false);
        rotationHandler.SetBlocked(false);
        OnCameraRotationEnd?.Invoke();
    }

    public void DeleteSelectedObject()
    {
        if (_selectedObject == null)
            return;

        GameObject go = _selectedObject.gameObject;
        selectionManager.DeselectObject();

        UndoRedoManager.Instance?.Record(new DeleteAction(go));

        // SetActive false instead of Destroy so Undo can restore it
        go.SetActive(false);
    }

    public void DuplicateSelectedObject()
    {
        if (_selectedObject == null)
            return;

        Vector3 offset = new Vector3(0.5f, 0, 0.5f);
        GameObject clone = Instantiate(
            _selectedObject.gameObject,
            _selectedObject.position + offset,
            _selectedObject.rotation
        );

        clone.name = _selectedObject.name.Replace("(Clone)", "").Trim();

        UndoRedoManager.Instance?.Record(
            new DuplicateAction(clone, _selectedObject.name, selectionManager)
        );

        selectionManager.SelectObject(clone.transform);
    }
}