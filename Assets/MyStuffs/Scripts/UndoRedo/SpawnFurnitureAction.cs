using UnityEngine;

public class SpawnFurnitureAction : IUndoableAction
{
    public string Description => $"Spawn {_itemKey}";

    private readonly string     _itemKey;
    private readonly GameObject _spawnedObject;

    public SpawnFurnitureAction(string itemKey, GameObject spawnedObject)
    {
        _itemKey       = itemKey;
        _spawnedObject = spawnedObject;
    }

    // Called if the action system ever re-executes it directly
    public void Execute()
    {
        if (_spawnedObject != null)
            _spawnedObject.SetActive(true);
    }

    public void Undo()
    {
        if (_spawnedObject != null)
            _spawnedObject.SetActive(false);
    }

    public void Redo()
    {
        if (_spawnedObject != null)
            _spawnedObject.SetActive(true);
    }
}