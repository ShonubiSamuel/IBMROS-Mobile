using System;
using UnityEngine.UIElements;

public class CategoryViewController
{
    public event Action<CategoryModel> OnCategorySelected;

    private VisualElement _root;
    private ScrollView    _scrollView;
    private VisualElement _grid;

    private const string CardClass  = "browser-grid-card";
    private const string IconClass  = "browser-grid-card__icon";
    private const string LabelClass = "browser-grid-card__label";

    public void Initialize(VisualElement root)
    {
        _root       = root;
        _scrollView = root.Q<ScrollView>("CategoryScrollView");
        _grid       = root.Q<VisualElement>("CategoryGrid");
    }

    public void Populate(string roomTypeId)
    {
        _grid.Clear();

        var index = FurniturePlaceholderData.GetRoomIndex();

        if (!index.TryGetValue(roomTypeId, out RoomTypeModel roomType))
            return;

        foreach (var category in roomType.Categories)
            _grid.Add(BuildCard(category));
    }

    public void Clear()
    {
        _grid?.Clear();
    }

    private VisualElement BuildCard(CategoryModel category)
    {
        var card = new Button(() => OnCategorySelected?.Invoke(category));
        card.AddToClassList(CardClass);

        var icon = new Label(category.IconEmoji);
        icon.AddToClassList(IconClass);

        var label = new Label(category.DisplayName);
        label.AddToClassList(LabelClass);

        card.Add(icon);
        card.Add(label);

        return card;
    }
}