using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoRedoManager : MonoBehaviour
{
    public static UndoRedoManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxHistorySize = 50;

    // Events consumed by ToolbarController
    public event Action OnHistoryChanged;

    private Stack<IUndoableAction> _undoStack = new Stack<IUndoableAction>();
    private Stack<IUndoableAction> _redoStack = new Stack<IUndoableAction>();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Record(IUndoableAction action)
    {
        if (action == null)
            return;

        // Clear redo history whenever a new action is recorded
        _redoStack.Clear();

        _undoStack.Push(action);

        // Trim history if it exceeds the limit
        if (_undoStack.Count > maxHistorySize)
            TrimUndoStack();

        Debug.Log($"[UndoRedoManager] Recorded: {action.Description}. " +
                  $"Undo stack: {_undoStack.Count}");

        OnHistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            Debug.Log("[UndoRedoManager] Nothing to undo.");
            return;
        }

        IUndoableAction action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        Debug.Log($"[UndoRedoManager] Undid: {action.Description}. " +
                  $"Undo stack: {_undoStack.Count}");

        OnHistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            Debug.Log("[UndoRedoManager] Nothing to redo.");
            return;
        }

        IUndoableAction action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);

        Debug.Log($"[UndoRedoManager] Redid: {action.Description}. " +
                  $"Undo stack: {_undoStack.Count}");

        OnHistoryChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnHistoryChanged?.Invoke();
        Debug.Log("[UndoRedoManager] History cleared.");
    }

    private void TrimUndoStack()
    {
        var temp = new Stack<IUndoableAction>();
        int count = 0;

        foreach (var action in _undoStack)
        {
            if (count >= maxHistorySize)
                break;

            temp.Push(action);
            count++;
        }

        _undoStack.Clear();

        foreach (var action in temp)
            _undoStack.Push(action);
    }
}