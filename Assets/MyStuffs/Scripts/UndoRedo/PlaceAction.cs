using UnityEngine;

public class PlaceAction : IUndoableAction
{
    public string Description => $"Place {_placedObject?.name}";

    private readonly GameObject      _placedObject;
    private readonly SelectionManager _selectionManager;
    private Vector3    _savedPosition;
    private Quaternion _savedRotation;

    public PlaceAction(GameObject placedObject, SelectionManager selectionManager)
    {
        _placedObject     = placedObject;
        _selectionManager = selectionManager;

        if (_placedObject != null)
        {
            _savedPosition = _placedObject.transform.position;
            _savedRotation = _placedObject.transform.rotation;
        }
    }

    // Called if action system re-executes directly
    public void Execute()
    {
        if (_placedObject == null) return;
        _placedObject.SetActive(true);
        _placedObject.transform.position = _savedPosition;
        _placedObject.transform.rotation = _savedRotation;
    }

    // Undo hides the placed object and deselects it
    public void Undo()
    {
        if (_placedObject == null) return;
        _selectionManager?.DeselectObject();
        _placedObject.SetActive(false);
    }

    // Redo restores it at the same position and reselects it
    public void Redo()
    {
        if (_placedObject == null) return;
        _placedObject.SetActive(true);
        _placedObject.transform.position = _savedPosition;
        _placedObject.transform.rotation = _savedRotation;
        _selectionManager?.SelectObject(_placedObject.transform);
    }
}