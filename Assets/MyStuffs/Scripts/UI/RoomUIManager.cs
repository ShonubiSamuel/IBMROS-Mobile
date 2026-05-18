using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class RoomUIManager : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Controllers")]
    [SerializeField] private ToolbarController toolbarController;
    [SerializeField] private BottomBarController bottomBarController;

    private VisualElement _root;

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[RoomUIManager] UIDocument not assigned.");
            return;
        }

        _root = uiDocument.rootVisualElement;

        toolbarController?.Initialize(_root);
        bottomBarController?.Initialize(_root);

        if (toolbarController != null)
        {
            toolbarController.OnCloseClicked += OnCloseClicked;
            toolbarController.OnScreenshotClicked += OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked += OnAddFurnitureClicked;
    }

    void OnDisable()
    {
        toolbarController?.Cleanup();

        if (toolbarController != null)
        {
            toolbarController.OnCloseClicked -= OnCloseClicked;
            toolbarController.OnScreenshotClicked -= OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked -= OnAddFurnitureClicked;
    }

    private void OnCloseClicked()
    {
        UndoRedoManager.Instance?.Clear();
        SceneTransition.SetSkipSplash(true);
        SceneManager.LoadScene("Main");
    }

    private void OnScreenshotClicked()
    {
        Debug.Log("[RoomUIManager] Screenshot tapped.");
        ScreenCapture.CaptureScreenshot("Screenshot.png");
    }

    private void OnAddFurnitureClicked()
    {
        Debug.Log("[RoomUIManager] Add furniture tapped.");
    }
}