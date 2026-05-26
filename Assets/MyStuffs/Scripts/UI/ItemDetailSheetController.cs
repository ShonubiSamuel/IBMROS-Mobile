using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemDetailSheetController : MonoBehaviour
{
    public event System.Action<string> OnAddToRoomClicked;
    public event System.Action OnSheetClosed;

    private VisualElement _overlay;
    private VisualElement _sheet;
    private Button        _backButton;
    private Button        _closeButton;
    private Button        _favButton;
    private Button        _addButton;
    private Label         _emojiLabel;
    private Label         _brandLabel;
    private Label         _nameLabel;
    private Label         _dimensionsLabel;
    private Label         _descriptionLabel;
    private Label         _favIcon;
    private VisualElement _colorRow;

    private bool      _isOpen = false;
    private Coroutine _animCoroutine;
    private string    _currentItemKey;

    private const float SLIDE_DURATION = 0.28f;

    // Colour swatches per item — extend as needed
    private static readonly System.Collections.Generic.Dictionary<string, (Color color, string label)[]> ItemColors = new()
    {
        ["IKEA BRIMNES"] = new[]
        {
            (new Color(0.95f, 0.95f, 0.95f), "White"),
            (new Color(0.15f, 0.15f, 0.15f), "Black"),
        },
        ["IKEA MALM"] = new[]
        {
            (new Color(0.95f, 0.95f, 0.95f), "White"),
            (new Color(0.55f, 0.38f, 0.25f), "Oak"),
        },
        ["IKEA HEMNES"] = new[]
        {
            (new Color(0.95f, 0.95f, 0.95f), "White"),
            (new Color(0.30f, 0.20f, 0.12f), "Dark Brown"),
            (new Color(0.70f, 0.60f, 0.50f), "Light Beige"),
        },
    };

    private static readonly System.Collections.Generic.Dictionary<string, string> ItemDescriptions = new()
    {
        ["IKEA BRIMNES"] = "A bed frame with hidden storage in several places — perfect for smaller spaces. The BRIMNES series has smart solutions that help you save space without compromising on style.",
        ["IKEA MALM"]    = "Clean lines and a simple design make MALM a versatile choice. The high headboard gives you extra storage space and a modern look.",
        ["IKEA HEMNES"]  = "Made from solid wood, which is a durable and warm natural material. The HEMNES series has a timeless design that fits well in traditional and modern homes alike.",
        ["IKEA FOLLDAL"] = "A sturdy bed frame with a simple, timeless design. The low profile gives the bedroom a relaxed, spacious feel.",
        ["IKEA KIVIK"]   = "KIVIK is a generous seating series with a soft, deep seat and comfortable support for your back. Easy to keep clean with removable, washable covers.",
        ["IKEA EKTORP"]  = "The EKTORP sofa has a classic look and is built for comfort and durability. Generous dimensions and machine-washable covers make it a practical family choice.",
    };

    // ---------------------------------------------------------------
    // INITIALIZE
    // ---------------------------------------------------------------

    public void Initialize(VisualElement root)
    {
        _overlay         = root.Q<VisualElement>("ItemDetailOverlay");
        _sheet           = root.Q<VisualElement>("ItemDetailSheet");
        _backButton      = root.Q<Button>("ItemDetailBackButton");
        _closeButton     = root.Q<Button>("ItemDetailCloseButton");
        _favButton       = root.Q<Button>("ItemDetailFavBtn");
        _addButton       = root.Q<Button>("ItemDetailAddButton");
        _emojiLabel      = root.Q<Label>("ItemDetailEmoji");
        _brandLabel      = root.Q<Label>("ItemDetailBrand");
        _nameLabel       = root.Q<Label>("ItemDetailName");
        _dimensionsLabel = root.Q<Label>("ItemDetailDimensions");
        _descriptionLabel= root.Q<Label>("ItemDetailDescription");
        _favIcon         = root.Q<Label>("ItemDetailFavIcon");
        _colorRow        = root.Q<VisualElement>("ItemDetailColorRow");

        if (_overlay == null)
            Debug.LogError("[ItemDetailSheetController] ItemDetailOverlay not found.");

        _backButton?.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            Close();
        });

        _closeButton?.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            Close();
        });

        _favButton?.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            if (_favIcon != null)
                _favIcon.text = _favIcon.text == "☆" ? "★" : "☆";
        });

        _addButton?.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            Debug.Log($"[ItemDetail] Add to Room: {_currentItemKey}");
            OnAddToRoomClicked?.Invoke(_currentItemKey);
            Close();
        });

        // Tap overlay background to dismiss
        _overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == _overlay)
                Close();
        });

        // Stop sheet from bubbling taps to overlay
        _sheet?.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        _sheet?.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
    }

    // ---------------------------------------------------------------
    // OPEN
    // ---------------------------------------------------------------

    public void Open(string emoji, string brand, string name, string dimensions)
    {
        _currentItemKey = $"{brand} {name}";

        if (_emojiLabel      != null) _emojiLabel.text      = emoji;
        if (_brandLabel      != null) _brandLabel.text      = brand;
        if (_nameLabel       != null) _nameLabel.text       = name;
        if (_dimensionsLabel != null) _dimensionsLabel.text = dimensions;
        if (_favIcon         != null) _favIcon.text         = "☆";

        if (_descriptionLabel != null)
        {
            _descriptionLabel.text = ItemDescriptions.TryGetValue(_currentItemKey, out var desc)
                ? desc
                : "A quality furniture piece designed for comfort and durability.";
        }

        BuildColorSwatches(_currentItemKey);

        _overlay.style.display = DisplayStyle.Flex;
        _isOpen = true;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(SlideIn());
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(SlideOut());
    }

    public bool IsOpen => _isOpen;

    // ---------------------------------------------------------------
    // COLOUR SWATCHES
    // ---------------------------------------------------------------

    private void BuildColorSwatches(string itemKey)
    {
        if (_colorRow == null) return;
        _colorRow.Clear();

        var colors = ItemColors.TryGetValue(itemKey, out var c)
            ? c
            : new[] { (new Color(0.95f, 0.95f, 0.95f), "Default") };

        bool first = true;
        foreach (var (color, label) in colors)
        {
            var swatch = new VisualElement();
            swatch.AddToClassList("item-detail-color-swatch");
            if (first) swatch.AddToClassList("item-detail-color-swatch--selected");
            swatch.style.backgroundColor = new StyleColor(color);

            swatch.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                foreach (var s in _colorRow.Children())
                    s.RemoveFromClassList("item-detail-color-swatch--selected");
                swatch.AddToClassList("item-detail-color-swatch--selected");
            });

            _colorRow.Add(swatch);
            first = false;
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
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / SLIDE_DURATION), 3f);
            _sheet.style.translate = new StyleTranslate(
                new Translate(0, Length.Percent(Mathf.Lerp(100f, 0f, eased))));
            yield return null;
        }
        _sheet.style.translate = new StyleTranslate(new Translate(0, Length.Percent(0)));
    }

    private IEnumerator SlideOut()
    {
        float elapsed = 0f;
        while (elapsed < SLIDE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / SLIDE_DURATION);
            float eased = t * t * t;
            _sheet.style.translate = new StyleTranslate(
                new Translate(0, Length.Percent(Mathf.Lerp(0f, 100f, eased))));
            yield return null;
        }
        _sheet.style.translate = new StyleTranslate(new Translate(0, Length.Percent(100)));
        _overlay.style.display = DisplayStyle.None;
        OnSheetClosed?.Invoke();
    }
}