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
    private Button        _tabRoom;
    private Button        _tabOtherRooms;
    private Label         _titleLabel;
    private Button        _backButton;
    private Button        _searchButton;

    // ---------------------------------------------------------------
    // STATE
    // ---------------------------------------------------------------

    private bool   _isOpen    = false;
    private bool   _isRoomTab = true;
    private bool _hasStoredState = false;
    private Coroutine _animationCoroutine;
    private const float SLIDE_DURATION = 0.3f;

    private readonly Stack<string> _navStack = new();

    // ---------------------------------------------------------------
    // CATEGORY DATA
    // ---------------------------------------------------------------

    private static readonly List<(string emoji, string name)> RoomCategories = new()
    {
        ("🏗️", "Build"),
        ("🛏️", "Bed"),
        ("🪑", "Tables and Chairs"),
        ("🪴", "Decoration"),
        ("🚪", "Cabinets, Shelves"),
        ("🛋️", "Seating Furniture"),
        ("💡", "Lighting"),
        ("📺", "Electronic Devices"),
        ("🍳", "Kitchen"),
        ("🎮", "Hobby & Entertainment"),
        ("➕", "More"),
    };

    private static readonly List<(string emoji, string name)> OtherRoomCategories = new()
    {
        ("🚿", "Bathroom"),
        ("🏋️", "Gym"),
        ("🖥️", "Store"),
        ("☕", "Cafe & Bar"),
        ("🎬", "Home Cinema"),
        ("🧺", "Laundry"),
        ("🚗", "Garage"),
        ("💼", "Home Office"),
        ("🍽️", "Dining Room"),
        ("🌿", "Yard or Patio"),
        ("🚪", "Hallway"),
    };

    private static readonly Dictionary<string, List<(string emoji, string name)>> SubCategories = new()
    {
        ["Bed"] = new()
        {
            ("🛏️", "Single Bed"), ("🛏️", "Double Bed"),
            ("🛏️", "Queen Bed"),  ("🛏️", "King Bed"),
            ("🪜", "Bunk Bed"),   ("🛋️", "Sofa Bed"),
        },
        ["Seating Furniture"] = new()
        {
            ("🛋️", "Sofas"),     ("🪑", "Armchairs"),
            ("🛋️", "Sofa Beds"), ("🪑", "Ottomans"),
            ("🪑", "Benches"),   ("🪑", "Stools"),
        },
        ["Tables and Chairs"] = new()
        {
            ("🍽️", "Dining Tables"), ("💻", "Desks"),
            ("☕", "Coffee Tables"), ("🪑", "Dining Chairs"),
            ("🪑", "Office Chairs"), ("📦", "Side Tables"),
        },
        ["Decoration"] = new()
        {
            ("🖼️", "Wall Art"), ("🪴", "Plants"),
            ("🧶", "Rugs"),     ("🪞", "Mirrors"),
            ("🕯️", "Candles"),  ("🏺", "Vases"),
        },
        ["Cabinets, Shelves"] = new()
        {
            ("🚪", "Wardrobes"),    ("📚", "Bookcases"),
            ("📺", "TV Units"),     ("🗄️", "Sideboards"),
            ("📦", "Storage Boxes"),("🗃️", "Shelving Units"),
        },
        ["Lighting"] = new()
        {
            ("💡", "Floor Lamps"),   ("🕯️", "Table Lamps"),
            ("💡", "Ceiling Lights"),("💡", "Wall Lights"),
            ("🔦", "Spotlights"),
        },
        ["Electronic Devices"] = new()
        {
            ("📺", "TVs"),       ("💻", "Computers"),
            ("🖨️", "Printers"),  ("🔊", "Speakers"),
        },
        ["Kitchen"] = new()
        {
            ("🍳", "Kitchen Units"),      ("🧊", "Refrigerators"),
            ("🫙", "Kitchen Accessories"),("🪣", "Sinks"),
        },
        ["Hobby & Entertainment"] = new()
        {
            ("🎮", "Gaming"), ("🎵", "Music"),
            ("🏸", "Sports"), ("📷", "Photography"),
        },
        ["Bathroom"] = new()
        {
            ("🚿", "Showers"),         ("🛁", "Bathtubs"),
            ("🪥", "Bathroom Storage"),("🪞", "Bathroom Mirrors"),
        },
        ["Home Office"] = new()
        {
            ("💻", "Desks"),       ("🪑", "Office Chairs"),
            ("📚", "Bookshelves"), ("💡", "Desk Lamps"),
        },
        ["Dining Room"] = new()
        {
            ("🍽️", "Dining Tables"), ("🪑", "Dining Chairs"),
            ("🗄️", "Sideboards"),    ("💡", "Dining Lighting"),
        },
    };

    // Item data per subcategory
    private static readonly Dictionary<string, List<(string emoji, string brand, string name, string dimensions, bool recommended)>> Items = new()
    {
        ["Single Bed"] = new()
        {
            ("🛏️", "IKEA", "BRIMNES", "90 × 200 cm", true),
            ("🛏️", "IKEA", "MALM",    "90 × 200 cm", false),
            ("🛏️", "IKEA", "HEMNES",  "90 × 200 cm", true),
        },
        ["Double Bed"] = new()
        {
            ("🛏️", "IKEA", "BRIMNES", "140 × 200 cm", true),
            ("🛏️", "IKEA", "MALM",    "140 × 200 cm", false),
        },
        ["Queen Bed"] = new()
        {
            ("🛏️", "IKEA", "BRIMNES", "160 × 200 cm", true),
            ("🛏️", "IKEA", "FOLLDAL", "160 × 200 cm", true),
            ("🛏️", "IKEA", "HEMNES",  "160 × 200 cm", false),
        },
        ["King Bed"] = new()
        {
            ("🛏️", "IKEA", "BRIMNES", "180 × 200 cm", false),
            ("🛏️", "IKEA", "MALM",    "180 × 200 cm", true),
        },
        ["Sofas"] = new()
        {
            ("🛋️", "IKEA", "KIVIK",   "280 × 95 cm", true),
            ("🛋️", "IKEA", "EKTORP",  "240 × 88 cm", false),
            ("🛋️", "IKEA", "SÖDERHAMN","270 × 99 cm", true),
        },
        ["Dining Tables"] = new()
        {
            ("🍽️", "IKEA", "EKEDALEN", "120 × 80 cm", true),
            ("🍽️", "IKEA", "LISABO",   "140 × 78 cm", false),
            ("🍽️", "IKEA", "INGATORP", "155 × 85 cm", true),
        },
        ["Desks"] = new()
        {
            ("💻", "IKEA", "MICKE",   "105 × 50 cm", true),
            ("💻", "IKEA", "LAGKAPTEN","140 × 60 cm", false),
            ("💻", "IKEA", "ALEX",    "132 × 58 cm", true),
        },
    };

    // ---------------------------------------------------------------
    // INITIALIZE
    // ---------------------------------------------------------------

    public void Initialize(VisualElement root)
    {
        _panel         = root.Q<VisualElement>("FurniturePanel");
        _categoryGrid  = root.Q<VisualElement>("FurnitureCategoryGrid");
        _tabs          = root.Q<VisualElement>("FurniturePanelTabs");
        _tabRoom       = root.Q<Button>("TabRoom");
        _tabOtherRooms = root.Q<Button>("TabOtherRooms");
        _titleLabel    = root.Q<Label>("FurniturePanelTitle");
        _backButton    = root.Q<Button>("FurniturePanelBackButton");
        _searchButton  = root.Q<Button>("FurniturePanelSearchButton");

        if (_panel == null)
            Debug.LogError("[FurniturePanelController] FurniturePanel not found.");
        
        _panel?.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        _panel?.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
        _panel?.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());

        _tabRoom?.RegisterCallback<ClickEvent>(evt => SwitchTab(true));
        _tabOtherRooms?.RegisterCallback<ClickEvent>(evt => SwitchTab(false));
        _backButton?.RegisterCallback<ClickEvent>(evt => NavigateBack());

        BuildCategoryGrid(RoomCategories);
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
            BuildCategoryGrid(RoomCategories);
        }
        else
        {
            _tabOtherRooms?.AddToClassList("furniture-tab--active");
            _tabRoom?.RemoveFromClassList("furniture-tab--active");
            BuildCategoryGrid(OtherRoomCategories);
        }
    }

    // ---------------------------------------------------------------
    // NAVIGATION
    // ---------------------------------------------------------------

    private void NavigateTo(string name)
    {
        _navStack.Push(name);
        UpdateHeader(name);

        if (Items.ContainsKey(name))
        {
            ShowItemList(name);
        }
        else if (SubCategories.TryGetValue(name, out var subs))
        {
            BuildCategoryGrid(subs, isSubCategory: true);
        }
        else
        {
            BuildEmptyState(name);
        }
    }

    private void NavigateBack()
    {
        if (_navStack.Count == 0) return;

        _navStack.Pop();

        if (_navStack.Count == 0)
        {
            UpdateHeader(null);
            BuildCategoryGrid(_isRoomTab ? RoomCategories : OtherRoomCategories);
        }
        else
        {
            string parent = _navStack.Peek();
            UpdateHeader(parent);

            if (Items.ContainsKey(parent))
                ShowItemList(parent);
            else if (SubCategories.TryGetValue(parent, out var subs))
                BuildCategoryGrid(subs, isSubCategory: true);
        }
    }

    private void UpdateHeader(string categoryName)
    {
        bool atRoot = (categoryName == null);

        if (_titleLabel != null)
            _titleLabel.text = atRoot ? "Room" : categoryName;

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
    // CATEGORY GRID
    // ---------------------------------------------------------------

    private void BuildCategoryGrid(
        List<(string emoji, string name)> categories,
        bool isSubCategory = false)
    {
        if (_categoryGrid == null) return;

        _categoryGrid.Clear();

        // Remove stale item list elements
        _panel.Q<ScrollView>("ItemListScroll")?.RemoveFromHierarchy();
        _panel.Q<ScrollView>("FilterChipsScroll")?.RemoveFromHierarchy();
        _panel.Q<VisualElement>("SortBar")?.RemoveFromHierarchy();

        // Show the category scroll view again
        var categoryScroll = _panel.Q<ScrollView>("FurniturePanelScroll");
        if (categoryScroll != null)
            categoryScroll.style.display = DisplayStyle.Flex;

        _categoryGrid.style.display = DisplayStyle.Flex;

        for (int i = 0; i < categories.Count; i += 2)
        {
            var row = new VisualElement();
            row.AddToClassList("category-grid-row");

            row.Add(CreateCategoryCard(categories[i].emoji, categories[i].name));

            if (i + 1 < categories.Count)
                row.Add(CreateCategoryCard(categories[i + 1].emoji, categories[i + 1].name));
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

    private Button CreateCategoryCard(string emoji, string name)
    {
        var card = new Button();
        card.AddToClassList("category-card");

        var iconArea = new VisualElement();
        iconArea.AddToClassList("category-card__icon-area");

        var emojiLabel = new Label(emoji);
        emojiLabel.AddToClassList("category-card__emoji");

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("category-card__name");

        iconArea.Add(emojiLabel);
        card.Add(iconArea);
        card.Add(nameLabel);

        card.RegisterCallback<ClickEvent>(evt => NavigateTo(name));

        return card;
    }

    // ---------------------------------------------------------------
    // ITEM LIST
    // ---------------------------------------------------------------

    private void ShowItemList(string subcategoryName)
    {
        if (_categoryGrid == null) return;

        // Hide the category scroll view
        var categoryScroll = _panel.Q<ScrollView>("FurniturePanelScroll");
        if (categoryScroll != null)
            categoryScroll.style.display = DisplayStyle.None;

        // Remove stale item list elements
        var body = _panel.Q<VisualElement>("FurniturePanelBody");
        if (body == null) return;

        body.Q<ScrollView>("ItemListScroll")?.RemoveFromHierarchy();
        body.Q<ScrollView>("FilterChipsScroll")?.RemoveFromHierarchy();
        body.Q<VisualElement>("SortBar")?.RemoveFromHierarchy();

        // -- Filter chips (horizontal scroll) --
        var chipsScroll = new ScrollView(ScrollViewMode.Horizontal);
        chipsScroll.name = "FilterChipsScroll";
        chipsScroll.AddToClassList("filter-chips-scroll");
        chipsScroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        chipsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        chipsScroll.touchScrollBehavior          = ScrollView.TouchScrollBehavior.Clamped;
        chipsScroll.mouseWheelScrollSize         = 100f;
        chipsScroll.contentContainer.style.flexDirection = FlexDirection.Row;
        chipsScroll.contentContainer.style.alignItems    = Align.Center;
        chipsScroll.contentContainer.pickingMode         = PickingMode.Position;
        
        var chips = GetSiblingNames(subcategoryName);
        foreach (var chip in chips)
        {
            var chipBtn = new Button();
            chipBtn.AddToClassList("filter-chip");

            // Mark active based on current subcategory, not just first
            if (chip == subcategoryName)
                chipBtn.AddToClassList("filter-chip--active");

            var chipLabel = new Label(chip);
            chipLabel.AddToClassList("filter-chip__label");
            chipBtn.Add(chipLabel);

            string chipName = chip;
            chipBtn.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                foreach (var c in chipsScroll.contentContainer.Children())
                    c.RemoveFromClassList("filter-chip--active");
                chipBtn.AddToClassList("filter-chip--active");

                _navStack.Pop();
                _navStack.Push(chipName);
                UpdateHeader(chipName);
                ShowItemList(chipName);
            });

            chipsScroll.Add(chipBtn);
        }

        body.Add(chipsScroll);

        // -- Sort bar --
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

        body.Add(sortBar);

        // -- Item list --
        var itemScroll = new ScrollView(ScrollViewMode.Vertical);
        itemScroll.name = "ItemListScroll";
        itemScroll.AddToClassList("item-list-scroll");
        itemScroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        itemScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        itemScroll.touchScrollBehavior          = ScrollView.TouchScrollBehavior.Clamped;
        itemScroll.contentContainer.pickingMode = PickingMode.Position;

        var itemContainer = new VisualElement();
        itemContainer.AddToClassList("item-list-container");

        var itemList = Items.ContainsKey(subcategoryName)
            ? Items[subcategoryName]
            : new List<(string, string, string, string, bool)>();

        foreach (var (emoji, brand, name, dimensions, recommended) in itemList)
            itemContainer.Add(CreateItemCard(emoji, brand, name, dimensions, recommended));

        if (itemList.Count == 0)
        {
            var empty = new Label($"No items yet for \"{subcategoryName}\".");
            empty.style.color              = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
            empty.style.fontSize           = 13;
            empty.style.unityTextAlign     = TextAnchor.MiddleCenter;
            empty.style.marginTop          = 40;
            empty.style.whiteSpace         = WhiteSpace.Normal;
            empty.style.paddingLeft        = 20;
            empty.style.paddingRight       = 20;
            itemContainer.Add(empty);
        }

        itemScroll.Add(itemContainer);
        body.Add(itemScroll);
    }

    private VisualElement CreateItemCard(
        string emoji, string brand, string name, string dimensions, bool recommended)
    {
        var card = new VisualElement();
        card.AddToClassList("item-card");

        // Image area
        var imageArea = new VisualElement();
        imageArea.AddToClassList("item-card__image-area");

        var emojiLabel = new Label(emoji);
        emojiLabel.AddToClassList("item-card__placeholder-emoji");
        imageArea.Add(emojiLabel);

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

        var nameLabel = new Label($"{brand} {name}");
        nameLabel.AddToClassList("item-card__name");

        var dimsLabel = new Label(dimensions);
        dimsLabel.AddToClassList("item-card__dimensions");

        textCol.Add(nameLabel);
        textCol.Add(dimsLabel);

        var favBtn = new Button();
        favBtn.AddToClassList("item-card__fav-btn");
        var favIcon = new Label("☆");
        favIcon.AddToClassList("item-card__fav-icon");
        favBtn.Add(favIcon);
        favBtn.RegisterCallback<ClickEvent>(evt =>
        {
            favIcon.text = favIcon.text == "☆" ? "★" : "☆";
        });

        info.Add(textCol);
        info.Add(favBtn);
        card.Add(info);

        // Tap whole card
        card.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            Debug.Log($"[FurniturePanel] Item tapped: {brand} {name}");
            OnItemSelected?.Invoke($"{emoji}|{brand}|{name}|{dimensions}");
        });

        return card;
    }

    // Returns sibling chip names for the horizontal filter row
    private List<string> GetSiblingNames(string currentName)
    {
        foreach (var subs in SubCategories.Values)
        {
            foreach (var (_, name) in subs)
            {
                if (name == currentName)
                    return subs.ConvertAll(s => s.name);
            }
        }
        return new List<string> { currentName };
    }

    private void BuildEmptyState(string categoryName)
    {
        if (_categoryGrid == null) return;
        _categoryGrid.Clear();

        var label = new Label($"Items for \"{categoryName}\" coming soon.");
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
    // OPEN / CLOSE
    // ---------------------------------------------------------------

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (!_hasStoredState)
        {
            // Fresh open — start from root
            _navStack.Clear();
            UpdateHeader(null);
            BuildCategoryGrid(_isRoomTab ? RoomCategories : OtherRoomCategories);
        }
        else
        {
            // Restore previous state
            _hasStoredState = false;
            RestoreState();
        }

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(SlideIn());
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _hasStoredState = false;

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(SlideOut(fireClosedEvent: true));
    }

    public bool IsOpen => _isOpen;

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
        _panel.style.translate = new StyleTranslate(new Translate(Length.Percent(0), 0));
    }

    private IEnumerator SlideOut()
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
        _panel.style.translate = new StyleTranslate(new Translate(Length.Percent(100), 0));
        OnPanelClosed?.Invoke();
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t)  => t * t * t;
    
    public void HideWithoutReset()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _hasStoredState = true;

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(SlideOut(fireClosedEvent: false));
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
        _panel.style.translate = new StyleTranslate(new Translate(Length.Percent(100), 0));

        if (fireClosedEvent)
            OnPanelClosed?.Invoke();
    }
    
    private void RestoreState()
    {
        if (_navStack.Count == 0)
        {
            UpdateHeader(null);
            BuildCategoryGrid(_isRoomTab ? RoomCategories : OtherRoomCategories);
            return;
        }

        string current = _navStack.Peek();
        UpdateHeader(current);

        if (Items.ContainsKey(current))
            ShowItemList(current);
        else if (SubCategories.TryGetValue(current, out var subs))
            BuildCategoryGrid(subs, isSubCategory: true);
        else
            BuildCategoryGrid(_isRoomTab ? RoomCategories : OtherRoomCategories);
    }
}