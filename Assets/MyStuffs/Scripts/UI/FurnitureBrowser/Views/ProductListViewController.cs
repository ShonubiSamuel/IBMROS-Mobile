using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProductListViewController
{
    public event Action<ProductModel> OnProductSelected;
    public event Action<string>       OnFavouriteToggled;

    private VisualElement _root;
    private ScrollView    _productScrollView;
    private VisualElement _filterRow;
    private VisualElement _colorSwatchRow;
    private VisualElement _productList;

    private List<ProductModel>  _allProducts   = new List<ProductModel>();
    private List<FilterOption>  _activeFilters = new List<FilterOption>();
    private string              _activeColorProductId;

    private const string FilterChipClass          = "filter-chip";
    private const string FilterChipActiveClass    = "filter-chip--active";
    private const string ColorSwatchClass         = "color-swatch";
    private const string ColorSwatchActiveClass   = "color-swatch--active";
    private const string ProductCardClass         = "product-card";
    private const string BadgeRecommendedClass    = "badge--recommended";
    private const string BadgeNewClass            = "badge--new";
    private const string FavouriteActiveClass     = "favourite-btn--active";

    public void Initialize(VisualElement root)
    {
        _root              = root;
        _productScrollView = root.Q<ScrollView>("ProductScrollView");
        _filterRow         = root.Q<VisualElement>("FilterRow");
        _colorSwatchRow    = root.Q<VisualElement>("ColorSwatchRow");
        _productList       = root.Q<VisualElement>("ProductList");
    }

    public void Populate(string subcategoryId)
    {
        _allProducts   = FurniturePlaceholderData.GetProductsForSubcategory(subcategoryId);
        _activeFilters = FurniturePlaceholderData.GetDefaultFilters();
        _activeColorProductId = null;

        BuildFilterRow();
        HideColorSwatches();
        ApplyFiltersAndRebuildList();
    }

    public void Clear()
    {
        _filterRow?.Clear();
        _colorSwatchRow?.Clear();
        _productList?.Clear();
        _allProducts.Clear();
        _activeFilters.Clear();
        _activeColorProductId = null;
    }

    // ---- Filter Row ----

    private void BuildFilterRow()
    {
        _filterRow.Clear();

        foreach (var filter in _activeFilters)
        {
            var chip = new Button(() => OnFilterChipClicked(filter.Key));
            chip.text = filter.Label;
            chip.AddToClassList(FilterChipClass);

            if (filter.IsActive)
                chip.AddToClassList(FilterChipActiveClass);

            _filterRow.Add(chip);
        }
    }

    private void OnFilterChipClicked(string key)
    {
        foreach (var f in _activeFilters)
            f.IsActive = f.Key == key;

        BuildFilterRow();
        ApplyFiltersAndRebuildList();
    }

    // ---- Product List ----

    private void ApplyFiltersAndRebuildList()
    {
        _productList.Clear();

        var activeFilter = _activeFilters.Find(f => f.IsActive);
        var filtered     = FilterProducts(_allProducts, activeFilter?.Key);

        foreach (var product in filtered)
            _productList.Add(BuildProductCard(product));
    }

    private List<ProductModel> FilterProducts(List<ProductModel> source, string filterKey)
    {
        if (string.IsNullOrEmpty(filterKey))
            return source;

        switch (filterKey)
        {
            case "popular":
                return source.FindAll(p => p.Tags.Contains("popular"));
            case "new":
                return source.FindAll(p => p.IsNew);
            case "recommended":
                return source.FindAll(p => p.IsRecommended);
            default:
                return source;
        }
    }

    private VisualElement BuildProductCard(ProductModel product)
    {
        var card = new VisualElement();
        card.AddToClassList(ProductCardClass);

        var imageContainer = new VisualElement();
        imageContainer.AddToClassList("product-card__image-container");

        var badgeRow = new VisualElement();
        badgeRow.AddToClassList("product-card__badge-row");

        if (product.IsRecommended)
        {
            var recommended = new Label("RECOMMENDED");
            recommended.AddToClassList(BadgeRecommendedClass);
            badgeRow.Add(recommended);
        }

        if (product.IsNew)
        {
            var newBadge = new Label("NEW");
            newBadge.AddToClassList(BadgeNewClass);
            badgeRow.Add(newBadge);
        }

        imageContainer.Add(badgeRow);

        var favouriteBtn = new Button(() => OnFavouriteClicked(product));
        favouriteBtn.AddToClassList("favourite-btn");

        if (product.IsFavourited)
            favouriteBtn.AddToClassList(FavouriteActiveClass);

        favouriteBtn.text = product.IsFavourited ? "★" : "☆";
        imageContainer.Add(favouriteBtn);

        card.Add(imageContainer);

        var info = new VisualElement();
        info.AddToClassList("product-card__info");

        var brandLabel = new Label(product.Brand);
        brandLabel.AddToClassList("product-card__brand");

        var nameLabel = new Label(product.Name);
        nameLabel.AddToClassList("product-card__name");

        var dimensionsLabel = new Label(product.Dimensions);
        dimensionsLabel.AddToClassList("product-card__dimensions");

        info.Add(brandLabel);
        info.Add(nameLabel);

        if (!string.IsNullOrEmpty(product.VariantLabel))
        {
            var variantLabel = new Label(product.VariantLabel);
            variantLabel.AddToClassList("product-card__variant");
            info.Add(variantLabel);
        }

        info.Add(dimensionsLabel);

        if (!string.IsNullOrEmpty(product.Price))
        {
            var priceLabel = new Label(product.Price);
            priceLabel.AddToClassList("product-card__price");
            info.Add(priceLabel);
        }

        card.Add(info);

        card.RegisterCallback<ClickEvent>(_ => OnProductCardClicked(product));

        if (product.ColorVariants != null && product.ColorVariants.Count > 1)
            card.RegisterCallback<PointerEnterEvent>(_ => ShowColorSwatches(product));

        return card;
    }

    // ---- Color Swatches ----

    private void ShowColorSwatches(ProductModel product)
    {
        if (_activeColorProductId == product.Id)
            return;

        _activeColorProductId = product.Id;
        _colorSwatchRow.Clear();
        _colorSwatchRow.style.display = DisplayStyle.Flex;

        foreach (var variant in product.ColorVariants)
        {
            var swatch = new Button(() => OnSwatchClicked(variant.ProductId));
            swatch.AddToClassList(ColorSwatchClass);

            if (ColorUtility.TryParseHtmlString(variant.HexColor, out Color color))
                swatch.style.backgroundColor = new StyleColor(color);

            swatch.tooltip = variant.ColorName;

            if (variant.ProductId == product.Id)
                swatch.AddToClassList(ColorSwatchActiveClass);

            _colorSwatchRow.Add(swatch);
        }
    }

    private void HideColorSwatches()
    {
        _activeColorProductId = null;
        _colorSwatchRow?.Clear();

        if (_colorSwatchRow != null)
            _colorSwatchRow.style.display = DisplayStyle.None;
    }

    private void OnSwatchClicked(string productId)
    {
        var index = FurniturePlaceholderData.GetProductIndex();

        if (!index.TryGetValue(productId, out ProductModel product))
            return;

        _activeColorProductId = productId;
        OnProductSelected?.Invoke(product);
    }

    // ---- Interaction ----

    private void OnProductCardClicked(ProductModel product)
    {
        OnProductSelected?.Invoke(product);
    }

    private void OnFavouriteClicked(ProductModel product)
    {
        product.IsFavourited = !product.IsFavourited;
        OnFavouriteToggled?.Invoke(product.Id);
        ApplyFiltersAndRebuildList();
    }
}