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
    [SerializeField] private FurnitureBrowserController furnitureBrowser;

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
            toolbarController.OnCloseClicked     += OnCloseClicked;
            toolbarController.OnScreenshotClicked += OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked += OnAddFurnitureClicked;

        if (furnitureBrowser != null)
        {
            furnitureBrowser.OnProductSelected += OnProductSelected;
            furnitureBrowser.OnClosed          += OnFurnitureBrowserClosed;
        }
    }

    void OnDisable()
    {
        toolbarController?.Cleanup();

        if (toolbarController != null)
        {
            toolbarController.OnCloseClicked      -= OnCloseClicked;
            toolbarController.OnScreenshotClicked -= OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked -= OnAddFurnitureClicked;

        if (furnitureBrowser != null)
        {
            furnitureBrowser.OnProductSelected -= OnProductSelected;
            furnitureBrowser.OnClosed          -= OnFurnitureBrowserClosed;
        }
    }

    // ---------------------------------------------------------------
    // TOOLBAR EVENTS
    // ---------------------------------------------------------------

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

    // ---------------------------------------------------------------
    // BOTTOM BAR EVENTS
    // ---------------------------------------------------------------

    private void OnAddFurnitureClicked()
    {
        Debug.Log("[RoomUIManager] Add furniture tapped.");

        if (furnitureBrowser == null)
            return;

        // Hide the bottom bar first
        bottomBarController?.SetVisible(false);

        // Try to open the browser — if it fails (not initialized yet),
        // immediately restore the bottom bar so the user isn't stuck
        bool opened = furnitureBrowser.Open();

        if (!opened)
        {
            Debug.LogWarning("[RoomUIManager] Furniture browser could not open — restoring bottom bar.");
            bottomBarController?.SetVisible(true);
        }
    }

    // ---------------------------------------------------------------
    // FURNITURE BROWSER EVENTS
    // ---------------------------------------------------------------

    private void OnFurnitureBrowserClosed()
    {
        // Restore the bottom bar when the browser fully closes
        bottomBarController?.SetVisible(true);
    }

    private void OnProductSelected(ProductModel product)
    {
        Debug.Log($"[RoomUIManager] Product selected: {product.Brand} {product.Name} — {product.ModelFileName}");
    }
}