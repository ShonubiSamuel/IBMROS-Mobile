using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class SubcategoryViewController
{
    public event Action<SubcategoryModel> OnSubcategorySelected;

    private VisualElement _root;
    private ScrollView    _scrollView;
    private VisualElement _grid;

    private const string CardClass      = "browser-grid-card";
    private const string IconClass      = "browser-grid-card__icon";
    private const string LabelClass     = "browser-grid-card__label";

    public void Initialize(VisualElement root)
    {
        _root       = root;
        _scrollView = root.Q<ScrollView>("SubcategoryScrollView");
        _grid       = root.Q<VisualElement>("SubcategoryGrid");
    }

    public void Populate(string categoryId)
    {
        _grid.Clear();

        var index = FurniturePlaceholderData.GetCategoryIndex();

        if (!index.TryGetValue(categoryId, out CategoryModel category))
            return;

        foreach (var subcategory in category.Subcategories)
            _grid.Add(BuildCard(subcategory));
    }

    public void Clear()
    {
        _grid?.Clear();
    }

    private VisualElement BuildCard(SubcategoryModel subcategory)
    {
        var card = new Button(() => OnSubcategorySelected?.Invoke(subcategory));
        card.AddToClassList(CardClass);

        var icon = new Label(subcategory.IconEmoji);
        icon.AddToClassList(IconClass);

        var label = new Label(subcategory.DisplayName);
        label.AddToClassList(LabelClass);

        card.Add(icon);
        card.Add(label);

        return card;
    }
}