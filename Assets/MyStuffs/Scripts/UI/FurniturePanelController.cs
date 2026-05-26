using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FurniturePanelController : MonoBehaviour
{
    public event System.Action OnPanelClosed;
    public event System.Action<string> OnItemSelected;

    // ---------------------------------------------------------------
    // UI REFERENCES
    // ---------------------------------------------------------------

    private VisualElement _panel;
    private VisualElement _categoryGrid;
    private VisualElement _tabs;
    private VisualElement _body;
    private Button        _tabRoom;
    private Button        _tabOtherRooms;
    private Label         _titleLabel;
    private Button        _backButton;
    private Button        _searchButton;

    // ---------------------------------------------------------------
    // STATE
    // ---------------------------------------------------------------

    private bool      _isOpen          = false;
    private bool      _hasStoredState  = false;
    private bool      _isRoomTab       = true;
    private Coroutine _animationCoroutine;
    private const float SLIDE_DURATION = 0.3f;

    // Navigation stack entries carry enough info to restore each level
    private class NavEntry
    {
        public string Title;
        public string CategoryId;
        public string SubcategoryId; // null when at category level
        public bool   IsItemList;
    }

    private readonly Stack<NavEntry> _navStack = new();

    // Cached data so back navigation does not re-fetch
    private List<CategoryModel>        _cachedCategories;
    private List<ProductModel>         _cachedProducts;
    private string                     _activeCategoryId;
    private string                     _activeSubcategoryId;

    // ---------------------------------------------------------------
    // INITIALIZE
    // ---------------------------------------------------------------

    public void Initialize(VisualElement root)
    {
        _panel         = root.Q<VisualElement>("FurniturePanel");
        _body          = root.Q<VisualElement>("FurniturePanelBody");
        _categoryGrid  = root.Q<VisualElement>("FurnitureCategoryGrid");
        _tabs          = root.Q<VisualElement>("FurniturePanelTabs");
        _tabRoom       = root.Q<Button>("TabRoom");
        _tabOtherRooms = root.Q<Button>("TabOtherRooms");
        _titleLabel    = root.Q<Label>("FurniturePanelTitle");
        _backButton    = root.Q<Button>("FurniturePanelBackButton");
        _searchButton  = root.Q<Button>("FurniturePanelSearchButton");

        if (_panel == null)
            Debug.LogError("[FurniturePanelController] FurniturePanel not found.");

        // Block all pointer events from leaking outside the panel
        _panel?.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        _panel?.RegisterCallback<PointerDownEvent>(
            evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        _panel?.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
        _panel?.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());

        _tabRoom?.RegisterCallback<ClickEvent>(evt => SwitchTab(true));
        _tabOtherRooms?.RegisterCallback<ClickEvent>(evt => SwitchTab(false));
        _backButton?.RegisterCallback<ClickEvent>(evt => NavigateBack());

        // Subscribe to data service events
        FurnitureDataService.OnCategoriesLoaded  += HandleCategoriesLoaded;
        FurnitureDataService.OnCategoriesFailed  += HandleDataFailed;
        FurnitureDataService.OnProductsLoaded    += HandleProductsLoaded;
        FurnitureDataService.OnProductsFailed    += HandleDataFailed;
        FurnitureDataService.OnSearchResultsLoaded += HandleSearchResults;
        FurnitureDataService.OnSearchFailed      += HandleDataFailed;
        FurnitureDataService.OnLoadingChanged    += HandleLoadingChanged;
    }

    void OnDestroy()
    {
        FurnitureDataService.OnCategoriesLoaded  -= HandleCategoriesLoaded;
        FurnitureDataService.OnCategoriesFailed  -= HandleDataFailed;
        FurnitureDataService.OnProductsLoaded    -= HandleProductsLoaded;
        FurnitureDataService.OnProductsFailed    -= HandleDataFailed;
        FurnitureDataService.OnSearchResultsLoaded -= HandleSearchResults;
        FurnitureDataService.OnSearchFailed      -= HandleDataFailed;
        FurnitureDataService.OnLoadingChanged    -= HandleLoadingChanged;
    }

    // ---------------------------------------------------------------
    // TAB SWITCHING
    // ---------------------------------------------------------------

    private void SwitchTab(bool isRoomTab)
    {
        _isRoomTab = isRoomTab;

        if (isRoomTab)
        {
            _tabRoom?.AddToClassList("furniture-tab--active");
            _tabOtherRooms?.RemoveFromClassList("furniture-tab--active");
        }
        else
        {
            _tabOtherRooms?.AddToClassList("furniture-tab--active");
            _tabRoom?.RemoveFromClassList("furniture-tab--active");
        }

        // Re-render categories for the selected tab
        if (_cachedCategories != null)
            BuildCategoryGrid(FilterCategoriesForTab(_cachedCategories));
    }

    private List<CategoryModel> FilterCategoriesForTab(List<CategoryModel> all)
    {
        // Room tab — indoor furniture categories
        var roomIds = new HashSet<string>
            { "0001", "0003", "0004", "0007", "0008", "0009", "0011", "0016" };

        // Other Rooms tab — outdoor and specialty
        var otherIds = new HashSet<string> { "0005" };

        return _isRoomTab
            ? all.FindAll(c => roomIds.Contains(c.CategoryId))
            : all.FindAll(c => otherIds.Contains(c.CategoryId));
    }

    // ---------------------------------------------------------------
    // NAVIGATION
    // ---------------------------------------------------------------

    private void NavigateTo(CategoryModel category)
    {
        _activeCategoryId    = category.CategoryId;
        _activeSubcategoryId = null;

        _navStack.Push(new NavEntry
        {
            Title         = CategoryMapper.GetAppCategoryName(category.CategoryId)
                            ?? category.CategoryName,
            CategoryId    = category.CategoryId,
            IsItemList    = false
        });

        UpdateHeader(_navStack.Peek().Title);

        // Show subcategories from the category's own subcategory list
        if (category.Subcategories != null && category.Subcategories.Count > 0)
        {
            BuildSubcategoryGrid(category.Subcategories, category.CategoryId);
        }
        else
        {
            // No subcategories — go straight to product list
            _ = FurnitureDataService.Instance.LoadProductsByCategory(category.CategoryId);
        }
    }

    private void NavigateToSubcategory(
        SubcategoryModel sub, string categoryId)
    {
        _activeSubcategoryId = sub.SubcategoryId;

        _navStack.Push(new NavEntry
        {
            Title          = sub.SubcategoryName,
            CategoryId     = categoryId,
            SubcategoryId  = sub.SubcategoryId,
            IsItemList     = true
        });

        UpdateHeader(sub.SubcategoryName);

        _ = FurnitureDataService.Instance
            .LoadProductsBySubcategory(categoryId, sub.SubcategoryId);
    }

    private void NavigateBack()
    {
        if (_navStack.Count == 0) return;

        _navStack.Pop();

        if (_navStack.Count == 0)
        {
            // Back to root category grid
            UpdateHeader(null);
            CleanItemListViews();
            ShowCategoryScroll();

            if (_cachedCategories != null)
                BuildCategoryGrid(FilterCategoriesForTab(_cachedCategories));
            else
                _ = FurnitureDataService.Instance.LoadCategories();

            return;
        }

        var entry = _navStack.Peek();
        UpdateHeader(entry.Title);

        if (entry.IsItemList)
        {
            // Back to a product list
            if (_cachedProducts != null)
            {
                CleanItemListViews();
                HideCategoryScroll();
                BuildItemList(_cachedProducts, entry.SubcategoryId);
            }
            else
            {
                _ = FurnitureDataService.Instance
                    .LoadProductsBySubcategory(entry.CategoryId, entry.SubcategoryId);
            }
        }
        else
        {
            // Back to a subcategory grid
            CleanItemListViews();
            ShowCategoryScroll();

            var category = _cachedCategories?.Find(c => c.CategoryId == entry.CategoryId);
            if (category?.Subcategories != null)
                BuildSubcategoryGrid(category.Subcategories, entry.CategoryId);
        }
    }

    private void UpdateHeader(string title)
    {
        bool atRoot = (title == null);

        if (_titleLabel != null)
            _titleLabel.text = atRoot ? "Room" : title;

        if (_backButton != null)
        {
            if (atRoot)
                _backButton.RemoveFromClassList("furniture-panel__back-btn--visible");
            else
                _backButton.AddToClassList("furniture-panel__back-btn--visible");
        }

        if (_tabs != null)
            _tabs.style.display = atRoot ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------------------------------------------------------------
    // DATA SERVICE HANDLERS
    // ---------------------------------------------------------------

    private void HandleCategoriesLoaded(List<CategoryModel> categories)
    {
        _cachedCategories = categories;
        BuildCategoryGrid(FilterCategoriesForTab(categories));
    }

    private void HandleProductsLoaded(List<ProductModel> products, string context)
    {
        _cachedProducts = products;
        CleanItemListViews();
        HideCategoryScroll();
        BuildItemList(products, context);
    }

    private void HandleSearchResults(List<ProductModel> results)
    {
        _cachedProducts = results;
        CleanItemListViews();
        HideCategoryScroll();
        BuildItemList(results, "Search Results");
    }

    private void HandleDataFailed(string message)
    {
        ShowErrorState(message);
    }

    private void HandleLoadingChanged(bool isLoading, string message)
    {
        if (isLoading)
            ShowLoadingState(message);
        else
            HideLoadingState();
    }

    // ---------------------------------------------------------------
    // CATEGORY GRID
    // ---------------------------------------------------------------

    private void BuildCategoryGrid(List<CategoryModel> categories)
    {
        if (_categoryGrid == null) return;

        _categoryGrid.Clear();
        ShowCategoryScroll();

        if (categories == null || categories.Count == 0)
        {
            ShowEmptyState("No categories available.");
            return;
        }

        for (int i = 0; i < categories.Count; i += 2)
        {
            var row = new VisualElement();
            row.AddToClassList("category-grid-row");

            row.Add(CreateCategoryCard(categories[i]));

            if (i + 1 < categories.Count)
                row.Add(CreateCategoryCard(categories[i + 1]));
            else
            {
                var spacer = new VisualElement();
                spacer.style.flexGrow   = 1;
                spacer.style.flexShrink = 1;
                spacer.style.marginLeft  = 4;
                spacer.style.marginRight = 4;
                row.Add(spacer);
            }

            _categoryGrid.Add(row);
        }
    }

    private Button CreateCategoryCard(CategoryModel category)
    {
        string appName = CategoryMapper.GetAppCategoryName(category.CategoryId)
                         ?? category.CategoryName;
        string emoji   = CategoryMapper.GetCategoryEmoji(appName);

        var card = new Button();
        card.AddToClassList("category-card");

        var iconArea = new VisualElement();
        iconArea.AddToClassList("category-card__icon-area");

        var emojiLabel = new Label(emoji);
        emojiLabel.AddToClassList("category-card__emoji");

        var nameLabel = new Label(appName);
        nameLabel.AddToClassList("category-card__name");

        iconArea.Add(emojiLabel);
        card.Add(iconArea);
        card.Add(nameLabel);

        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            NavigateTo(category);
        });

        return card;
    }

    // ---------------------------------------------------------------
    // SUBCATEGORY GRID
    // ---------------------------------------------------------------

    private void BuildSubcategoryGrid(
        List<SubcategoryModel> subcategories, string categoryId)
    {
        if (_categoryGrid == null) return;

        _categoryGrid.Clear();
        ShowCategoryScroll();

        string emoji = CategoryMapper.GetCategoryEmoji(
            CategoryMapper.GetAppCategoryName(categoryId) ?? "");

        for (int i = 0; i < subcategories.Count; i += 2)
        {
            var row = new VisualElement();
            row.AddToClassList("category-grid-row");

            row.Add(CreateSubcategoryCard(subcategories[i], categoryId, emoji));

            if (i + 1 < subcategories.Count)
                row.Add(CreateSubcategoryCard(
                    subcategories[i + 1], categoryId, emoji));
            else
            {
                var spacer = new VisualElement();
                spacer.style.flexGrow   = 1;
                spacer.style.flexShrink = 1;
                spacer.style.marginLeft  = 4;
                spacer.style.marginRight = 4;
                row.Add(spacer);
            }

            _categoryGrid.Add(row);
        }
    }

    private Button CreateSubcategoryCard(
        SubcategoryModel sub, string categoryId, string emoji)
    {
        var card = new Button();
        card.AddToClassList("category-card");

        var iconArea = new VisualElement();
        iconArea.AddToClassList("category-card__icon-area");

        var emojiLabel = new Label(emoji);
        emojiLabel.AddToClassList("category-card__emoji");

        var nameLabel = new Label(sub.SubcategoryName);
        nameLabel.AddToClassList("category-card__name");

        iconArea.Add(emojiLabel);
        card.Add(iconArea);
        card.Add(nameLabel);

        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            NavigateToSubcategory(sub, categoryId);
        });

        return card;
    }

    // ---------------------------------------------------------------
    // ITEM LIST
    // ---------------------------------------------------------------

    private void BuildItemList(List<ProductModel> products, string context)
    {
        if (_body == null) return;

        // Filter chips from sibling subcategories if available
        BuildFilterChips(context);

        // Sort bar
        BuildSortBar();

        // Item scroll
        var itemScroll = new ScrollView(ScrollViewMode.Vertical);
        itemScroll.name = "ItemListScroll";
        itemScroll.AddToClassList("item-list-scroll");
        itemScroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        itemScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        itemScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        itemScroll.contentContainer.pickingMode = PickingMode.Position;

        var itemContainer = new VisualElement();
        itemContainer.AddToClassList("item-list-container");

        if (products == null || products.Count == 0)
        {
            var empty = new Label("No items found in this category.");
            empty.style.color          = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
            empty.style.fontSize       = 13;
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.marginTop      = 40;
            empty.style.whiteSpace     = WhiteSpace.Normal;
            empty.style.paddingLeft    = 20;
            empty.style.paddingRight   = 20;
            itemContainer.Add(empty);
        }
        else
        {
            foreach (var product in products)
                itemContainer.Add(CreateItemCard(product));
        }

        itemScroll.Add(itemContainer);
        _body.Add(itemScroll);
    }

    private void BuildFilterChips(string activeSubcategoryId)
    {
        if (_body == null) return;

        // Get sibling subcategories from active category
        var siblings = GetSiblingSubcategories(activeSubcategoryId);
        if (siblings == null || siblings.Count <= 1) return;

        var chipsScroll = new ScrollView(ScrollViewMode.Horizontal);
        chipsScroll.name = "FilterChipsScroll";
        chipsScroll.AddToClassList("filter-chips-scroll");
        chipsScroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        chipsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        chipsScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        chipsScroll.mouseWheelScrollSize = 100f;
        chipsScroll.contentContainer.style.flexDirection = FlexDirection.Row;
        chipsScroll.contentContainer.style.alignItems    = Align.Center;
        chipsScroll.contentContainer.pickingMode         = PickingMode.Position;

        foreach (var (sub, categoryId) in siblings)
        {
            var chipBtn = new Button();
            chipBtn.AddToClassList("filter-chip");

            if (sub.SubcategoryId == activeSubcategoryId)
                chipBtn.AddToClassList("filter-chip--active");

            var chipLabel = new Label(sub.SubcategoryName);
            chipLabel.AddToClassList("filter-chip__label");
            chipBtn.Add(chipLabel);

            var capturedSub       = sub;
            var capturedCategoryId = categoryId;

            chipBtn.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();

                foreach (var c in chipsScroll.contentContainer.Children())
                    c.RemoveFromClassList("filter-chip--active");
                chipBtn.AddToClassList("filter-chip--active");

                // Update nav stack top entry
                if (_navStack.Count > 0)
                {
                    var top = _navStack.Peek();
                    top.Title        = capturedSub.SubcategoryName;
                    top.SubcategoryId = capturedSub.SubcategoryId;
                    _activeSubcategoryId = capturedSub.SubcategoryId;
                }

                UpdateHeader(capturedSub.SubcategoryName);
                CleanItemListViews();
                HideCategoryScroll();

                _ = FurnitureDataService.Instance
                    .LoadProductsBySubcategory(capturedCategoryId,
                                               capturedSub.SubcategoryId);
            });

            chipsScroll.Add(chipBtn);
        }

        _body.Add(chipsScroll);
    }

    private void BuildSortBar()
    {
        if (_body == null) return;

        var sortBar = new VisualElement();
        sortBar.name = "SortBar";
        sortBar.AddToClassList("sort-bar");

        foreach (var (label, isActive) in new[] { ("Popular", true), ("New", false) })
        {
            var btn = new Button();
            btn.AddToClassList("sort-btn");
            if (isActive) btn.AddToClassList("sort-btn--active");

            var lbl = new Label(label);
            lbl.AddToClassList("sort-btn__label");
            btn.Add(lbl);

            btn.RegisterCallback<ClickEvent>(evt =>
            {
                foreach (var c in sortBar.Children())
                    c.RemoveFromClassList("sort-btn--active");
                btn.AddToClassList("sort-btn--active");
            });

            sortBar.Add(btn);
        }

        var filterIconBtn = new Button();
        filterIconBtn.AddToClassList("sort-filter-icon-btn");
        var filterIcon = new Label("⚙");
        filterIcon.AddToClassList("sort-filter-icon-btn__label");
        filterIconBtn.Add(filterIcon);
        sortBar.Add(filterIconBtn);

        _body.Add(sortBar);
    }

    private VisualElement CreateItemCard(ProductModel product)
    {
        var card = new VisualElement();
        card.AddToClassList("item-card");

        // Image area
        var imageArea = new VisualElement();
        imageArea.AddToClassList("item-card__image-area");

        // Emoji placeholder until real images load
        var emojiLabel = new Label(
            CategoryMapper.GetCategoryEmoji(
                CategoryMapper.GetAppCategoryName(_activeCategoryId) ?? ""));
        emojiLabel.AddToClassList("item-card__placeholder-emoji");
        imageArea.Add(emojiLabel);

        // Recommended badge for well-rated items
        bool recommended = product.StarRatingValue >= 4.5f
                           && product.ReviewCountValue >= 10;
        if (recommended)
        {
            var badge     = new VisualElement();
            badge.AddToClassList("item-card__badge");
            var badgeText = new Label("RECOMMENDED");
            badgeText.AddToClassList("item-card__badge-text");
            badge.Add(badgeText);
            imageArea.Add(badge);
        }

        card.Add(imageArea);

        // Info row
        var info = new VisualElement();
        info.AddToClassList("item-card__info");

        var textCol = new VisualElement();
        textCol.AddToClassList("item-card__text");

        var nameLabel = new Label(product.Name);
        nameLabel.AddToClassList("item-card__name");

        var priceLabel = new Label(product.FormattedPrice);
        priceLabel.AddToClassList("item-card__dimensions");

        textCol.Add(nameLabel);
        textCol.Add(priceLabel);

        var favBtn  = new Button();
        favBtn.AddToClassList("item-card__fav-btn");
        var favIcon = new Label("☆");
        favIcon.AddToClassList("item-card__fav-icon");
        favBtn.Add(favIcon);
        favBtn.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            favIcon.text = favIcon.text == "☆" ? "★" : "☆";
        });

        info.Add(textCol);
        info.Add(favBtn);
        card.Add(info);

        // Tap card — pass full product data through pipe format
        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            string emoji = CategoryMapper.GetCategoryEmoji(
                CategoryMapper.GetAppCategoryName(_activeCategoryId) ?? "");

            // Format: emoji|name|price|productId
            OnItemSelected?.Invoke(
                $"{emoji}|{product.Name}|{product.FormattedPrice}|{product.ProductId}");
        });

        return card;
    }

    // ---------------------------------------------------------------
    // SIBLING SUBCATEGORIES FOR FILTER CHIPS
    // ---------------------------------------------------------------

    private List<(SubcategoryModel sub, string categoryId)> GetSiblingSubcategories(
        string subcategoryId)
    {
        if (_cachedCategories == null || string.IsNullOrEmpty(subcategoryId))
            return null;

        foreach (var category in _cachedCategories)
        {
            if (category.Subcategories == null) continue;

            foreach (var sub in category.Subcategories)
            {
                if (sub.SubcategoryId != subcategoryId) continue;

                var result = new List<(SubcategoryModel, string)>();
                foreach (var sibling in category.Subcategories)
                    result.Add((sibling, category.CategoryId));

                return result;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------
    // LOADING AND ERROR STATES
    // ---------------------------------------------------------------

    private void ShowLoadingState(string message)
    {
        if (_categoryGrid == null) return;

        _categoryGrid.Clear();

        var label = new Label(string.IsNullOrEmpty(message) ? "Loading..." : message);
        label.style.color          = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
        label.style.fontSize       = 13;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginTop      = 40;
        label.style.whiteSpace     = WhiteSpace.Normal;
        label.style.paddingLeft    = 20;
        label.style.paddingRight   = 20;

        _categoryGrid.Add(label);
        ShowCategoryScroll();
    }

    private void HideLoadingState()
    {
        // Data handlers will rebuild the grid when data arrives
    }

    private void ShowErrorState(string message)
    {
        if (_categoryGrid == null) return;

        _categoryGrid.Clear();
        CleanItemListViews();
        ShowCategoryScroll();

        var label = new Label(message);
        label.style.color          = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
        label.style.fontSize       = 13;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginTop      = 40;
        label.style.whiteSpace     = WhiteSpace.Normal;
        label.style.paddingLeft    = 20;
        label.style.paddingRight   = 20;

        _categoryGrid.Add(label);

        // Retry button
        var retryBtn = new Button();
        retryBtn.text = "Retry";
        retryBtn.style.marginTop      = 16;
        retryBtn.style.alignSelf      = Align.Center;
        retryBtn.style.paddingLeft    = 20;
        retryBtn.style.paddingRight   = 20;

        retryBtn.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            _ = FurnitureDataService.Instance.LoadCategories();
        });

        _categoryGrid.Add(retryBtn);
    }

    private void ShowEmptyState(string message)
    {
        if (_categoryGrid == null) return;

        var label = new Label(message);
        label.style.color          = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
        label.style.fontSize       = 13;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginTop      = 40;
        label.style.whiteSpace     = WhiteSpace.Normal;
        label.style.paddingLeft    = 20;
        label.style.paddingRight   = 20;

        _categoryGrid.Add(label);
    }

    // ---------------------------------------------------------------
    // SCROLL VIEW HELPERS
    // ---------------------------------------------------------------

    private void ShowCategoryScroll()
    {
        var scroll = _panel?.Q<ScrollView>("FurniturePanelScroll");
        if (scroll != null)
            scroll.style.display = DisplayStyle.Flex;
    }

    private void HideCategoryScroll()
    {
        var scroll = _panel?.Q<ScrollView>("FurniturePanelScroll");
        if (scroll != null)
            scroll.style.display = DisplayStyle.None;
    }

    private void CleanItemListViews()
    {
        if (_body == null) return;
        _body.Q<ScrollView>("ItemListScroll")?.RemoveFromHierarchy();
        _body.Q<ScrollView>("FilterChipsScroll")?.RemoveFromHierarchy();
        _body.Q<VisualElement>("SortBar")?.RemoveFromHierarchy();
    }

    // ---------------------------------------------------------------
    // OPEN / CLOSE / RESTORE
    // ---------------------------------------------------------------

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (!_hasStoredState)
        {
            _navStack.Clear();
            UpdateHeader(null);
            CleanItemListViews();
            ShowCategoryScroll();

            if (_cachedCategories != null)
                BuildCategoryGrid(FilterCategoriesForTab(_cachedCategories));
            else
                _ = FurnitureDataService.Instance.LoadCategories();
        }
        else
        {
            _hasStoredState = false;
            RestoreState();
        }

        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(SlideIn());
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen         = false;
        _hasStoredState = false;

        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(SlideOut(fireClosedEvent: true));
    }

    public void HideWithoutReset()
    {
        if (!_isOpen) return;
        _isOpen         = false;
        _hasStoredState = true;

        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(SlideOut(fireClosedEvent: false));
    }

    public bool IsOpen => _isOpen;

    private void RestoreState()
    {
        if (_navStack.Count == 0)
        {
            UpdateHeader(null);
            CleanItemListViews();
            ShowCategoryScroll();

            if (_cachedCategories != null)
                BuildCategoryGrid(FilterCategoriesForTab(_cachedCategories));
            else
                _ = FurnitureDataService.Instance.LoadCategories();

            return;
        }

        var entry = _navStack.Peek();
        UpdateHeader(entry.Title);

        if (entry.IsItemList && _cachedProducts != null)
        {
            CleanItemListViews();
            HideCategoryScroll();
            BuildItemList(_cachedProducts, entry.SubcategoryId);
        }
        else if (!entry.IsItemList)
        {
            CleanItemListViews();
            ShowCategoryScroll();

            var category = _cachedCategories?.Find(c => c.CategoryId == entry.CategoryId);
            if (category?.Subcategories != null)
                BuildSubcategoryGrid(category.Subcategories, entry.CategoryId);
        }
    }

    // ---------------------------------------------------------------
    // ANIMATION
    // ---------------------------------------------------------------

    private IEnumerator SlideIn()
    {
        float elapsed = 0f;
        while (elapsed < SLIDE_DURATION)
        {
            elapsed += Time.deltaTime;
            float eased = EaseOutCubic(Mathf.Clamp01(elapsed / SLIDE_DURATION));
            _panel.style.translate = new StyleTranslate(
                new Translate(Length.Percent(Mathf.Lerp(100f, 0f, eased)), 0));
            yield return null;
        }
        _panel.style.translate = new StyleTranslate(
            new Translate(Length.Percent(0), 0));
    }

    private IEnumerator SlideOut(bool fireClosedEvent = true)
    {
        float elapsed = 0f;
        while (elapsed < SLIDE_DURATION)
        {
            elapsed += Time.deltaTime;
            float eased = EaseInCubic(Mathf.Clamp01(elapsed / SLIDE_DURATION));
            _panel.style.translate = new StyleTranslate(
                new Translate(Length.Percent(Mathf.Lerp(0f, 100f, eased)), 0));
            yield return null;
        }
        _panel.style.translate = new StyleTranslate(
            new Translate(Length.Percent(100), 0));

        if (fireClosedEvent)
            OnPanelClosed?.Invoke();
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t)  => t * t * t;
}