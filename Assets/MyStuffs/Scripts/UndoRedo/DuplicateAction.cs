using UnityEngine;

public class DuplicateAction : IUndoableAction
{
    public string Description => $"Duplicate {_sourceName}";

    private readonly string _sourceName;
    private GameObject _clone;
    private readonly SelectionManager _selectionManager;

    public DuplicateAction(GameObject clone, string sourceName, SelectionManager selectionManager)
    {
        _clone = clone;
        _sourceName = sourceName;
        _selectionManager = selectionManager;
    }

    public void Execute()
    {
        if (_clone != null)
            _clone.SetActive(true);
    }

    public void Undo()
    {
        if (_clone != null)
        {
            if (_selectionManager != null)
                _selectionManager.DeselectObject();

            _clone.SetActive(false);
        }
    }

    public void Redo()
    {
        Execute();

        if (_clone != null && _selectionManager != null)
            _selectionManager.SelectObject(_clone.transform);
    }
}