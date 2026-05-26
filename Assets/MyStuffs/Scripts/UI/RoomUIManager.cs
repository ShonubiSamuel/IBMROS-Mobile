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
    [SerializeField] private FurniturePanelController furniturePanelController;
    [SerializeField] private ItemDetailSheetController itemDetailSheetController;
    
    [Header("Spawning")]
    [SerializeField] private FurnitureSpawnManager furnitureSpawnManager;

    [Header("Scene Objects to Hide")]
    [SerializeField] private GameObject joystickObject;

    private VisualElement _root;
    private VisualElement _topBar;
    private VisualElement _bottomBar;
    private VisualElement _roomRoot;

    private bool _ignoreNextRootClick = false;
    private bool _addedToRoom        = false;

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[RoomUIManager] UIDocument not assigned.");
            return;
        }

        _root      = uiDocument.rootVisualElement;
        _topBar    = _root.Q<VisualElement>("TopBar");
        _bottomBar = _root.Q<VisualElement>("BottomBar");
        _roomRoot  = _root.Q<VisualElement>("Root");

        _roomRoot?.RegisterCallback<PointerDownEvent>(OnRootPointerDown);

        toolbarController?.Initialize(_root);
        bottomBarController?.Initialize(_root);
        furniturePanelController?.Initialize(_root);
        itemDetailSheetController?.Initialize(_root);

        if (toolbarController != null)
        {
            toolbarController.OnCloseClicked      += OnCloseClicked;
            toolbarController.OnScreenshotClicked += OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked += OnAddFurnitureClicked;

        if (furniturePanelController != null)
        {
            furniturePanelController.OnPanelClosed  += OnFurniturePanelClosed;
            furniturePanelController.OnItemSelected += OnItemSelected;
        }

        if (itemDetailSheetController != null)
        {
            itemDetailSheetController.OnSheetClosed     += OnItemDetailClosed;
            itemDetailSheetController.OnAddToRoomClicked += OnAddToRoomHandler;
        }
    }

    void OnDisable()
    {
        _roomRoot?.UnregisterCallback<PointerDownEvent>(OnRootPointerDown);

        toolbarController?.Cleanup();

        if (toolbarController != null)
        {
            toolbarController.OnCloseClicked      -= OnCloseClicked;
            toolbarController.OnScreenshotClicked -= OnScreenshotClicked;
        }

        if (bottomBarController != null)
            bottomBarController.OnAddFurnitureClicked -= OnAddFurnitureClicked;

        if (furniturePanelController != null)
        {
            furniturePanelController.OnPanelClosed  -= OnFurniturePanelClosed;
            furniturePanelController.OnItemSelected -= OnItemSelected;
        }

        if (itemDetailSheetController != null)
        {
            itemDetailSheetController.OnSheetClosed     -= OnItemDetailClosed;
            itemDetailSheetController.OnAddToRoomClicked -= OnAddToRoomHandler;
        }
    }

    // ---------------------------------------------------------------
    // ROOT POINTER DOWN
    // ---------------------------------------------------------------

    private void OnRootPointerDown(PointerDownEvent evt)
    {
        if (_ignoreNextRootClick)
        {
            _ignoreNextRootClick = false;
            return;
        }

        if (furniturePanelController == null || !furniturePanelController.IsOpen)
            return;

        if (itemDetailSheetController != null && itemDetailSheetController.IsOpen)
            return;

        var panel = _root.Q<VisualElement>("FurniturePanel");
        if (panel != null)
        {
            Vector2 localPos = panel.WorldToLocal(evt.position);
            if (panel.ContainsPoint(localPos))
                return;
        }

        Debug.Log("[RoomUIManager] Outside tap — closing panel.");
        furniturePanelController.Close();
    }

    // ---------------------------------------------------------------
    // TOOLBAR
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
    // BOTTOM BAR
    // ---------------------------------------------------------------

    private void OnAddFurnitureClicked()
    {
        Debug.Log("[RoomUIManager] Add furniture tapped.");
        _ignoreNextRootClick = true;
        SetRoomUIVisible(false);
        furniturePanelController?.Open();
    }

    // ---------------------------------------------------------------
    // PANEL CALLBACKS
    // ---------------------------------------------------------------

    private void OnFurniturePanelClosed()
    {
        Debug.Log("[RoomUIManager] Panel closed — restoring UI.");
        SetRoomUIVisible(true);
    }

    private void OnItemSelected(string itemData)
    {
        var parts = itemData.Split('|');
        if (parts.Length < 4) return;

        string emoji      = parts[0];
        string brand      = parts[1];
        string name       = parts[2];
        string dimensions = parts[3];

        furniturePanelController?.HideWithoutReset();
        itemDetailSheetController?.Open(emoji, brand, name, dimensions);
    }

    // ---------------------------------------------------------------
    // ITEM DETAIL CALLBACKS
    // ---------------------------------------------------------------

    private void OnAddToRoomHandler(string itemKey)
    {
        _addedToRoom = true;
        Debug.Log($"[RoomUIManager] Add to Room: {itemKey}");
        furnitureSpawnManager?.SpawnItem(itemKey);
    }

    private void OnItemDetailClosed()
    {
        if (_addedToRoom)
        {
            // User placed furniture — restore main UI, do NOT reopen panel
            _addedToRoom = false;
            SetRoomUIVisible(true);
            return;
        }

        // User dismissed sheet via back/close — reopen panel where they left off
        _ignoreNextRootClick = true;
        furniturePanelController?.Open();
    }

    // ---------------------------------------------------------------
    // VISIBILITY
    // ---------------------------------------------------------------

    private void SetRoomUIVisible(bool visible)
    {
        if (_topBar != null)
            _topBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (_bottomBar != null)
            _bottomBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (joystickObject != null)
            joystickObject.SetActive(visible);
    }
}