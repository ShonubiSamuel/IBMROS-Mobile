using UnityEngine;

public class DeleteAction : IUndoableAction
{
    public string Description => $"Delete {_objectName}";

    private readonly GameObject _gameObject;
    private readonly string _objectName;
    private readonly Vector3 _position;
    private readonly Quaternion _rotation;
    private readonly Vector3 _scale;
    private readonly Transform _parent;
    private GameObject _restoredObject;

    public DeleteAction(GameObject gameObject)
    {
        _gameObject = gameObject;
        _objectName = gameObject.name;
        _position = gameObject.transform.position;
        _rotation = gameObject.transform.rotation;
        _scale = gameObject.transform.localScale;
        _parent = gameObject.transform.parent;
    }

    public void Execute()
    {
        if (_gameObject != null)
            _gameObject.SetActive(false);
    }

    public void Undo()
    {
        if (_gameObject != null)
        {
            _gameObject.SetActive(true);
            _gameObject.transform.position = _position;
            _gameObject.transform.rotation = _rotation;
            _gameObject.transform.localScale = _scale;
            _gameObject.transform.SetParent(_parent);
        }
    }

    public void Redo()
    {
        if (_gameObject != null)
            _gameObject.SetActive(false);
    }
}