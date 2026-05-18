using UnityEngine;
using UnityEngine.UIElements;
using System;

public class ToolbarController : MonoBehaviour
{
    public event Action OnCloseClicked;
    public event Action OnScreenshotClicked;
    public event Action OnShareClicked;

    private Button _undoButton;
    private Button _closeButton;
    private Button _screenshotButton;
    private Button _shareButton;
    private Button _moreButton;

    public void Initialize(VisualElement root)
    {
        _undoButton = root.Q<Button>("UndoButton");
        _closeButton = root.Q<Button>("CloseButton");
        _screenshotButton = root.Q<Button>("ScreenshotButton");
        _shareButton = root.Q<Button>("ShareButton");
        _moreButton = root.Q<Button>("MoreButton");

        if (_undoButton != null)
            _undoButton.clicked += OnUndoClicked;

        if (_closeButton != null)
            _closeButton.clicked += () => OnCloseClicked?.Invoke();

        if (_screenshotButton != null)
            _screenshotButton.clicked += () => OnScreenshotClicked?.Invoke();

        if (_shareButton != null)
            _shareButton.clicked += () => OnShareClicked?.Invoke();

        if (UndoRedoManager.Instance != null)
            UndoRedoManager.Instance.OnHistoryChanged += RefreshUndoButton;

        RefreshUndoButton();
    }

    public void Cleanup()
    {
        if (_undoButton != null)
            _undoButton.clicked -= OnUndoClicked;

        if (UndoRedoManager.Instance != null)
            UndoRedoManager.Instance.OnHistoryChanged -= RefreshUndoButton;
    }

    private void OnUndoClicked()
    {
        UndoRedoManager.Instance?.Undo();
    }

    private void RefreshUndoButton()
    {
        if (_undoButton == null)
            return;

        bool canUndo = UndoRedoManager.Instance != null
                       && UndoRedoManager.Instance.CanUndo;

        _undoButton.SetEnabled(canUndo);
        _undoButton.style.opacity = canUndo ? 1f : 0.4f;
    }
}