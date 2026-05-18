using UnityEngine;

public class PlaceAction : IUndoableAction
{
    public string Description => $"Place {_gameObject.name}";

    private readonly GameObject _gameObject;
    private readonly SelectionManager _selectionManager;

    public PlaceAction(GameObject gameObject, SelectionManager selectionManager)
    {
        _gameObject = gameObject;
        _selectionManager = selectionManager;
    }

    public void Execute()
    {
        if (_gameObject != null)
            _gameObject.SetActive(true);
    }

    public void Undo()
    {
        if (_gameObject == null)
            return;

        if (_selectionManager != null)
            _selectionManager.DeselectObject();

        FurnitureRegistry.Instance?.Unregister(
            _gameObject.GetComponent<FurnitureItem>()
        );

        _gameObject.SetActive(false);
    }

    public void Redo()
    {
        if (_gameObject == null)
            return;

        _gameObject.SetActive(true);

        FurnitureRegistry.Instance?.Register(
            _gameObject.GetComponent<FurnitureItem>()
        );

        if (_selectionManager != null)
            _selectionManager.SelectObject(_gameObject.transform);
    }
}