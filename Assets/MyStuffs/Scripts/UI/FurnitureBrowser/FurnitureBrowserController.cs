using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FurnitureBrowserController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // EVENTS
    // ---------------------------------------------------------------

    public event Action<ProductModel> OnProductSelected;
    public event Action OnClosed;

    // ---------------------------------------------------------------
    // INSPECTOR FIELDS
    // ---------------------------------------------------------------

    [SerializeField] private UIDocument furnitureBrowserDocument;
    [SerializeField] private VisualTreeAsset panelAsset;

    // ---------------------------------------------------------------
    // PRIVATE STATE
    // ---------------------------------------------------------------

    private VisualElement _root;
    private VisualElement _panelRoot;
    private VisualElement _browserPanel;
    private VisualElement _backdrop;
    private Button        _backButton;
    private Label         _headerTitle;
    private Button        _searchButton;
    private Button        _closeButton;

    private RoomTypeViewController    _roomTypeVC;
    private CategoryViewController    _categoryVC;
    private SubcategoryViewController _subcategoryVC;
    private ProductListViewController _productListVC;

    private FurnitureBrowserNav _nav;

    private bool _isOpen;
    private bool _isInitialized;
    private Coroutine _animCoroutine;
    private Coroutine _initCoroutine;

    private const string PanelVisibleClass    = "furniture-browser--visible";
    private const string BackdropVisibleClass = "furniture-browser-backdrop--visible";
    private const float  AnimDurationSec      = 0.25f;

    // ---------------------------------------------------------------
    // UNITY LIFECYCLE
    // ---------------------------------------------------------------

    void OnEnable()
    {
        Debug.Log("[FurnitureBrowser] OnEnable — starting deferred init.");
        _initCoroutine = StartCoroutine(DeferredInitialize());
    }

    void OnDisable()
    {
        Debug.Log("[FurnitureBrowser] OnDisable — cleaning up.");

        if (_initCoroutine != null)
        {
            StopCoroutine(_initCoroutine);
            _initCoroutine = null;
        }

        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        CleanupBrowser();
    }

    // ---------------------------------------------------------------
    // DEFERRED INIT (one-frame wait so all UIDocuments are ready)
    // ---------------------------------------------------------------

    private IEnumerator DeferredInitialize()
    {
        Debug.Log("[FurnitureBrowser] Waiting one frame for UIDocuments to initialize...");
        yield return null; // wait one frame

        Debug.Log("[FurnitureBrowser] Starting InitializeBrowser.");
        InitializeBrowser();
        _initCoroutine = null;
    }

    private void InitializeBrowser()
    {
        // --- Validate Inspector references ---
        if (furnitureBrowserDocument == null)
        {
            Debug.LogWarning("[FurnitureBrowser] ⚠️ 'Furniture Browser Document' field is NULL — " +
                           "drag the FurnitureBrowserUI UIDocument into this slot.");
            return;
        }

        // Check if the reference is valid (not destroyed)
        try
        {
            var _ = furnitureBrowserDocument.gameObject;
        }
        catch (UnityEngine.MissingReferenceException)
        {
            Debug.LogWarning("[FurnitureBrowser] ⚠️ 'Furniture Browser Document' reference is destroyed. " +
                           "Please reassign it in the inspector.");
            return;
        }

        Debug.Log("[FurnitureBrowser] furnitureBrowserDocument is assigned: " +
                  furnitureBrowserDocument.name);

        if (panelAsset == null)
        {
            Debug.LogError("[FurnitureBrowser] ❌ 'Panel Asset' field is NULL — " +
                           "drag FurnitureBrowserPanel.uxml into this slot.");
            return;
        }

        Debug.Log("[FurnitureBrowser] panelAsset is assigned: " + panelAsset.name);

        // --- Get the document root ---
        _root = furnitureBrowserDocument.rootVisualElement;

        if (_root == null)
        {
            Debug.LogError("[FurnitureBrowser] ❌ rootVisualElement is NULL — " +
                           "the UIDocument has no Panel Settings assigned.");
            return;
        }

        Debug.Log($"[FurnitureBrowser] Document root acquired. " +
                  $"Root child count BEFORE clone: {_root.childCount}");

        // Root must NOT block input when the panel is closed
        _root.pickingMode = PickingMode.Ignore;

        // --- Clone the panel UXML ---
        _panelRoot = panelAsset.CloneTree();

        if (_panelRoot == null)
        {
            Debug.LogError("[FurnitureBrowser] ❌ panelAsset.CloneTree() returned null.");
            return;
        }

        // The UXML has a .furniture-browser-root element as its single
        // root which CSS sizes to fill the full screen — no programmatic
        // hacks needed. Just mark the TemplateContainer as non-blocking.
        _panelRoot.pickingMode = PickingMode.Ignore;

        _root.Add(_panelRoot);

        Debug.Log($"[FurnitureBrowser] Panel cloned and added. " +
                  $"Root child count AFTER clone: {_root.childCount}. " +
                  $"PanelRoot child count: {_panelRoot.childCount}");

        // --- Query named elements ---
        QueryElements();

        if (_browserPanel == null)
        {
            Debug.LogError("[FurnitureBrowser] ❌ 'FurnitureBrowserPanel' element NOT found " +
                           "in cloned tree. Check FurnitureBrowserPanel.uxml has a VisualElement " +
                           "with name='FurnitureBrowserPanel'.");

            // Dump the full cloned tree so we can see what names DO exist
            DumpTree(_panelRoot, 0);
            return;
        }

        if (_backdrop == null)
        {
            Debug.LogError("[FurnitureBrowser] ❌ 'FurnitureBrowserBackdrop' element NOT found.");
            return;
        }

        // --- Wire sub-controllers ---
        InitViewControllers();
        InitNav();
        WireHeaderEvents();
        WireBackdrop();

        // Start hidden
        _browserPanel.style.display = DisplayStyle.None;
        _backdrop.style.display     = DisplayStyle.None;

        _isInitialized = true;
        Debug.Log("[FurnitureBrowser] ✅ Initialized successfully and ready to open.");
    }

    // ---------------------------------------------------------------
    // CLEANUP
    // ---------------------------------------------------------------

    private void CleanupBrowser()
    {
        if (_nav != null)
        {
            _nav.OnNavigatedForward -= HandleNavigatedForward;
            _nav.OnNavigatedBack    -= HandleNavigatedBack;
            _nav.OnStackCleared     -= HandleStackCleared;
        }

        if (_roomTypeVC    != null) _roomTypeVC.OnRoomTypeSelected       -= HandleRoomTypeSelected;
        if (_categoryVC    != null) _categoryVC.OnCategorySelected       -= HandleCategorySelected;
        if (_subcategoryVC != null) _subcategoryVC.OnSubcategorySelected -= HandleSubcategorySelected;
        if (_productListVC != null)
        {
            _productListVC.OnProductSelected  -= HandleProductSelected;
            _productListVC.OnFavouriteToggled -= HandleFavouriteToggled;
        }

        if (_panelRoot != null && _root != null && _root.Contains(_panelRoot))
            _root.Remove(_panelRoot);

        _isInitialized = false;
        _isOpen        = false;
    }

    // ---------------------------------------------------------------
    // QUERY
    // ---------------------------------------------------------------

    private void QueryElements()
    {
        _backdrop     = _panelRoot.Q<VisualElement>("FurnitureBrowserBackdrop");
        _browserPanel = _panelRoot.Q<VisualElement>("FurnitureBrowserPanel");
        _backButton   = _panelRoot.Q<Button>("BrowserBackButton");
        _headerTitle  = _panelRoot.Q<Label>("BrowserHeaderTitle");
        _searchButton = _panelRoot.Q<Button>("BrowserSearchButton");
        _closeButton  = _panelRoot.Q<Button>("BrowserCloseButton");

        Debug.Log($"[FurnitureBrowser] Query results:\n" +
                  $"  FurnitureBrowserBackdrop : {(_backdrop     != null ? "✅ FOUND" : "❌ NOT FOUND")}\n" +
                  $"  FurnitureBrowserPanel    : {(_browserPanel != null ? "✅ FOUND" : "❌ NOT FOUND")}\n" +
                  $"  BrowserBackButton        : {(_backButton   != null ? "✅ FOUND" : "❌ NOT FOUND")}\n" +
                  $"  BrowserHeaderTitle       : {(_headerTitle  != null ? "✅ FOUND" : "❌ NOT FOUND")}\n" +
                  $"  BrowserSearchButton      : {(_searchButton != null ? "✅ FOUND" : "❌ NOT FOUND")}\n" +
                  $"  BrowserCloseButton       : {(_closeButton  != null ? "✅ FOUND" : "❌ NOT FOUND")}");
    }

    private void DumpTree(VisualElement el, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"[FurnitureBrowser] TREE: {indent}{el.GetType().Name} name='{el.name}' " +
                  $"classes='{string.Join(",", el.GetClasses())}'");

        foreach (var child in el.Children())
            DumpTree(child, depth + 1);
    }

    // ---------------------------------------------------------------
    // INIT HELPERS
    // ---------------------------------------------------------------

    private void InitViewControllers()
    {
        _roomTypeVC    = new RoomTypeViewController();
        _categoryVC    = new CategoryViewController();
        _subcategoryVC = new SubcategoryViewController();
        _productListVC = new ProductListViewController();

        _roomTypeVC.Initialize(_panelRoot);
        _categoryVC.Initialize(_panelRoot);
        _subcategoryVC.Initialize(_panelRoot);
        _productListVC.Initialize(_panelRoot);

        _roomTypeVC.OnRoomTypeSelected       += HandleRoomTypeSelected;
        _categoryVC.OnCategorySelected       += HandleCategorySelected;
        _subcategoryVC.OnSubcategorySelected += HandleSubcategorySelected;
        _productListVC.OnProductSelected     += HandleProductSelected;
        _productListVC.OnFavouriteToggled    += HandleFavouriteToggled;

        Debug.Log("[FurnitureBrowser] View controllers initialized.");
    }

    private void InitNav()
    {
        _nav = new FurnitureBrowserNav();
        _nav.OnNavigatedForward += HandleNavigatedForward;
        _nav.OnNavigatedBack    += HandleNavigatedBack;
        _nav.OnStackCleared     += HandleStackCleared;
    }

    private void WireHeaderEvents()
    {
        if (_backButton   != null) _backButton.clicked   += OnBackClicked;
        if (_closeButton  != null) _closeButton.clicked  += OnCloseClicked;
        if (_searchButton != null) _searchButton.clicked += OnSearchClicked;
    }

    private void WireBackdrop()
    {
        _backdrop?.RegisterCallback<ClickEvent>(_ => OnCloseClicked());
    }

    // ---------------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------------

    public bool Open()
    {
        if (_isOpen)
        {
            Debug.Log("[FurnitureBrowser] Open() called but already open.");
            return true;
        }

        if (!_isInitialized)
        {
            Debug.LogWarning("[FurnitureBrowser] ⚠️ Open() called before initialization completed. " +
                             "Try again in the next frame.");
            return false;
        }

        _isOpen = true;

        Debug.Log("[FurnitureBrowser] Populating room type grid...");
        _roomTypeVC.Populate();

        ShowView(BrowserLevel.RoomType);
        SetHeader("By room type", canGoBack: false);

        Debug.Log("[FurnitureBrowser] Opening panel — starting animation coroutine.");

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());

        return true;
    }

    public void Close()
    {
        if (!_isOpen)
        {
            Debug.Log("[FurnitureBrowser] Close() called but already closed.");
            return;
        }

        Debug.Log("[FurnitureBrowser] Closing panel — starting animation coroutine.");
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    // ---------------------------------------------------------------
    // VIEW CONTROLLER EVENTS
    // ---------------------------------------------------------------

    private void HandleRoomTypeSelected(RoomTypeModel roomType)
    {
        Debug.Log($"[FurnitureBrowser] Room type selected: {roomType.DisplayName}");
        _nav.PushCategory(roomType.Id, roomType.DisplayName);
    }

    private void HandleCategorySelected(CategoryModel category)
    {
        Debug.Log($"[FurnitureBrowser] Category selected: {category.DisplayName}");
        _nav.PushSubcategory(category.Id, category.DisplayName);
    }

    private void HandleSubcategorySelected(SubcategoryModel subcategory)
    {
        Debug.Log($"[FurnitureBrowser] Subcategory selected: {subcategory.DisplayName}");
        _nav.PushProductList(subcategory.Id, subcategory.DisplayName);
    }

    private void HandleProductSelected(ProductModel product)
    {
        Debug.Log($"[FurnitureBrowser] Product selected: {product.Brand} — {product.Name}");
        OnProductSelected?.Invoke(product);
        Close();
    }

    private void HandleFavouriteToggled(string productId)
    {
        Debug.Log($"[FurnitureBrowser] Favourite toggled: {productId}");
    }

    // ---------------------------------------------------------------
    // NAV EVENTS
    // ---------------------------------------------------------------

    private void HandleNavigatedForward(BrowserNavEntry entry)
    {
        Debug.Log($"[FurnitureBrowser] Nav forward → {entry.Level} (id={entry.SelectedId})");

        switch (entry.Level)
        {
            case BrowserLevel.Category:
                _categoryVC.Populate(entry.SelectedId);
                break;
            case BrowserLevel.Subcategory:
                _subcategoryVC.Populate(entry.SelectedId);
                break;
            case BrowserLevel.ProductList:
                _productListVC.Populate(entry.SelectedId);
                break;
        }

        ShowView(entry.Level);
        SetHeader(entry.HeaderTitle, canGoBack: _nav.CanGoBack);
    }

    private void HandleNavigatedBack(BrowserNavEntry entry)
    {
        Debug.Log($"[FurnitureBrowser] Nav back → {entry.Level}");
        ShowView(entry.Level);
        SetHeader(entry.HeaderTitle, canGoBack: _nav.CanGoBack);
    }

    private void HandleStackCleared()
    {
        Debug.Log("[FurnitureBrowser] Nav stack cleared — back to room type view.");
        _roomTypeVC.Populate();
        ShowView(BrowserLevel.RoomType);
        SetHeader("By room type", canGoBack: false);
    }

    // ---------------------------------------------------------------
    // HEADER
    // ---------------------------------------------------------------

    private void SetHeader(string title, bool canGoBack)
    {
        if (_headerTitle != null) _headerTitle.text = title;

        if (_backButton != null)
            _backButton.style.display = canGoBack ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------------------------------------------------------------
    // VIEW SWITCHING
    // ---------------------------------------------------------------

    private void ShowView(BrowserLevel level)
    {
        Debug.Log($"[FurnitureBrowser] Showing view: {level}");
        SetViewDisplay("RoomTypeView",    level == BrowserLevel.RoomType);
        SetViewDisplay("CategoryView",    level == BrowserLevel.Category);
        SetViewDisplay("SubcategoryView", level == BrowserLevel.Subcategory);
        SetViewDisplay("ProductListView", level == BrowserLevel.ProductList);
    }

     private void SetViewDisplay(string elementName, bool visible)
     {
         var el = _panelRoot?.Q<VisualElement>(elementName);
         if (el != null)
         {
             el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
             Debug.Log($"[FurnitureBrowser] SetViewDisplay: '{elementName}' set to {el.style.display.value}");
         }
         else
             Debug.LogWarning($"[FurnitureBrowser] SetViewDisplay: element '{elementName}' not found.");
     }

    // ---------------------------------------------------------------
    // BUTTON HANDLERS
    // ---------------------------------------------------------------

    private void OnBackClicked()   => _nav.Pop();
    private void OnCloseClicked()  => Close();
    private void OnSearchClicked() => Debug.Log("[FurnitureBrowser] Search tapped.");

    // ---------------------------------------------------------------
    // ANIMATION — coroutines only (guaranteed main thread in Unity)
    // ---------------------------------------------------------------

    private IEnumerator AnimateOpen()
    {
        Debug.Log("[FurnitureBrowser] AnimateOpen START — setting display:Flex on panel and backdrop.");

        // Make elements renderable first
        _browserPanel.style.display = DisplayStyle.Flex;
        _backdrop.style.display     = DisplayStyle.Flex;

        Debug.Log("[FurnitureBrowser] AnimateOpen — display set. " +
                  $"Panel display style: {_browserPanel.style.display.value}");

        // Yield one frame so the layout pass applies the display
        // change before the CSS transition class is added
        yield return null;

        Debug.Log("[FurnitureBrowser] AnimateOpen — adding visible CSS classes.");
        _browserPanel.AddToClassList(PanelVisibleClass);
        _backdrop.AddToClassList(BackdropVisibleClass);

        Debug.Log($"[FurnitureBrowser] AnimateOpen — panel classes: " +
                  $"{string.Join(", ", _browserPanel.GetClasses())}");

        // Wait for the transition to complete
        yield return new WaitForSeconds(AnimDurationSec);

        Debug.Log("[FurnitureBrowser] AnimateOpen COMPLETE — panel should be fully visible.");
        _animCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        Debug.Log("[FurnitureBrowser] AnimateClose START — removing visible CSS classes.");

        _browserPanel.RemoveFromClassList(PanelVisibleClass);
        _backdrop.RemoveFromClassList(BackdropVisibleClass);

        // Wait for the slide-out transition
        yield return new WaitForSeconds(AnimDurationSec);

        _browserPanel.style.display = DisplayStyle.None;
        _backdrop.style.display     = DisplayStyle.None;

        _isOpen = false;

        _nav.Clear();
        _categoryVC.Clear();
        _subcategoryVC.Clear();
        _productListVC.Clear();

        Debug.Log("[FurnitureBrowser] AnimateClose COMPLETE — panel hidden, nav reset.");
        _animCoroutine = null;

        OnClosed?.Invoke();
    }
}