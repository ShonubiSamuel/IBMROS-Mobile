using UnityEngine;
using System;

public class ObjectDragHandler : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Config")]
    [SerializeField] private LayerMask floorLayer;

    public event Action OnDragStart;
    public event Action OnDragEnd;

    private Transform _selectedObject;
    private Camera _mainCamera;
    private bool _isDragging = false;
    private bool _blocked = false;
    private Vector3 _dragStartPosition;
    private Vector3 _grabOffset;

    public bool IsDragging => _isDragging;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected += HandleObjectSelected;
            selectionManager.onObjectDeselected += HandleObjectDeselected;
        }
    }

    void OnDisable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected -= HandleObjectSelected;
            selectionManager.onObjectDeselected -= HandleObjectDeselected;
        }
    }

    public void SetBlocked(bool blocked)
    {
        _blocked = blocked;

        if (blocked && _isDragging)
            CancelDrag();
    }

    // Called by ObjectManipulator in priority order
    public bool TryBeginDrag(Vector2 screenPosition)
    {
        if (_blocked || _selectedObject == null)
            return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return false;

        bool hitSelected = hit.transform == _selectedObject
                           || hit.transform.IsChildOf(_selectedObject);

        if (!hitSelected)
            return false;

        _isDragging = true;
        _dragStartPosition = _selectedObject.position;

        // Find where the floor would be hit under the same touch
        if (Physics.Raycast(ray, out RaycastHit floorHit, 100f, floorLayer))
            _grabOffset = _selectedObject.position - floorHit.point;
        else
            _grabOffset = Vector3.zero;

        OnDragStart?.Invoke();
        return true;
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (!_isDragging || _selectedObject == null)
            return;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
        {
            Vector3 targetPosition = hit.point + _grabOffset;

            Renderer renderer = _selectedObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                float pivotToBase = _selectedObject.position.y - renderer.bounds.min.y;
                targetPosition.y = hit.point.y + pivotToBase;
            }

            _selectedObject.position = targetPosition;
        }
    }

    public void EndDrag()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        OnDragEnd?.Invoke();

        UndoRedoManager.Instance?.Record(
            new MoveAction(_selectedObject, _dragStartPosition, _selectedObject.position)
        );
    }

    private void CancelDrag()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        OnDragEnd?.Invoke();
    }

    private void HandleObjectSelected(Transform target)
    {
        _selectedObject = target;
    }

    private void HandleObjectDeselected()
    {
        _selectedObject = null;
        CancelDrag();
    }
}