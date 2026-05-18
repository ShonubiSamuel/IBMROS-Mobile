using UnityEngine;

public class RotateAction : IUndoableAction
{
    public string Description => $"Rotate {_target.name}";

    private readonly Transform _target;
    private readonly Quaternion _previousRotation;
    private readonly Quaternion _nextRotation;

    public RotateAction(Transform target, Quaternion previousRotation, Quaternion nextRotation)
    {
        _target = target;
        _previousRotation = previousRotation;
        _nextRotation = nextRotation;
    }

    public void Execute()
    {
        if (_target != null)
            _target.rotation = _nextRotation;
    }

    public void Undo()
    {
        if (_target != null)
            _target.rotation = _previousRotation;
    }

    public void Redo()
    {
        Execute();
    }
}