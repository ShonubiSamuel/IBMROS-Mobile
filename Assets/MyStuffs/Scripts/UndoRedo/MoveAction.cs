using UnityEngine;

public class MoveAction : IUndoableAction
{
    public string Description => $"Move {_target.name}";

    private readonly Transform _target;
    private readonly Vector3 _previousPosition;
    private readonly Vector3 _nextPosition;

    public MoveAction(Transform target, Vector3 previousPosition, Vector3 nextPosition)
    {
        _target = target;
        _previousPosition = previousPosition;
        _nextPosition = nextPosition;
    }

    public void Execute()
    {
        if (_target != null)
            _target.position = _nextPosition;
    }

    public void Undo()
    {
        if (_target != null)
            _target.position = _previousPosition;
    }

    public void Redo()
    {
        Execute();
    }
}