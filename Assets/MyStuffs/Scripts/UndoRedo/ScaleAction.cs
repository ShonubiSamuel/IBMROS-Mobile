using UnityEngine;

public class ScaleAction : IUndoableAction
{
    public string Description => $"Scale {_target.name}";

    private readonly Transform _target;
    private readonly Vector3 _previousScale;
    private readonly Vector3 _nextScale;

    public ScaleAction(Transform target, Vector3 previousScale, Vector3 nextScale)
    {
        _target = target;
        _previousScale = previousScale;
        _nextScale = nextScale;
    }

    public void Execute()
    {
        if (_target != null)
            _target.localScale = _nextScale;
    }

    public void Undo()
    {
        if (_target != null)
            _target.localScale = _previousScale;
    }

    public void Redo()
    {
        Execute();
    }
}