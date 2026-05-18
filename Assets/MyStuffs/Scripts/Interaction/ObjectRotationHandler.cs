using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ObjectRotationHandler : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private UIDragHandle rotationHandle;

    [Header("Config")]
    [SerializeField] private float rotationSpeed = 0.5f;

    public event Action OnRotationStart;
    public event Action OnRotationEnd;

    private Transform _selectedObject;
    private bool _isRotating = false;
    private bool _blocked = false;
    private Quaternion _rotationStart;

    public bool IsRotating => _isRotating;

    void OnEnable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected += HandleObjectSelected;
            selectionManager.onObjectDeselected += HandleObjectDeselected;
        }

        if (rotationHandle != null)
        {
            rotationHandle.onDragStart += HandleDragStart;
            rotationHandle.onDrag += HandleDrag;
            rotationHandle.onDragEnd += HandleDragEnd;
        }
    }

    void OnDisable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected -= HandleObjectSelected;
            selectionManager.onObjectDeselected -= HandleObjectDeselected;
        }

        if (rotationHandle != null)
        {
            rotationHandle.onDragStart -= HandleDragStart;
            rotationHandle.onDrag -= HandleDrag;
            rotationHandle.onDragEnd -= HandleDragEnd;
        }
    }

    public void SetBlocked(bool blocked)
    {
        _blocked = blocked;

        if (blocked && _isRotating)
            CancelRotation();
    }

    private void HandleObjectSelected(Transform target)
    {
        _selectedObject = target;
    }

    private void HandleObjectDeselected()
    {
        _selectedObject = null;
        CancelRotation();
    }

    private void HandleDragStart(PointerEventData data)
    {
        if (_blocked || _selectedObject == null)
            return;

        _rotationStart = _selectedObject.rotation;
        _isRotating = true;
        OnRotationStart?.Invoke();
    }

    private void HandleDrag(PointerEventData data)
    {
        if (!_isRotating || _selectedObject == null)
            return;

        _selectedObject.Rotate(
            Vector3.up,
            -data.delta.x * rotationSpeed,
            Space.World
        );
    }

    private void HandleDragEnd(PointerEventData data)
    {
        if (!_isRotating)
            return;

        _isRotating = false;

        UndoRedoManager.Instance?.Record(
            new RotateAction(_selectedObject, _rotationStart, _selectedObject.rotation)
        );

        OnRotationEnd?.Invoke();
    }

    private void CancelRotation()
    {
        if (!_isRotating)
            return;

        _isRotating = false;
        OnRotationEnd?.Invoke();
    }
}