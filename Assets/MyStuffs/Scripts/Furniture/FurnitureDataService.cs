using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Event-driven service layer for furniture data.
/// Wraps FurnitureRepository exactly as AuthService wraps AuthManager.
/// UI controllers subscribe to events here — never call FurnitureRepository directly.
/// </summary>
public class FurnitureDataService : MonoBehaviour
{
    public static FurnitureDataService Instance { get; private set; }

    // ---------------------------------------------------------------
    // EVENTS
    // ---------------------------------------------------------------

    // Categories
    public static event Action<List<CategoryModel>>        OnCategoriesLoaded;
    public static event Action<string>                     OnCategoriesFailed;

    // Products
    public static event Action<List<ProductModel>, string> OnProductsLoaded;
    // string = categoryId or subcategoryId context
    public static event Action<string>                     OnProductsFailed;

    // Single product
    public static event Action<ProductModel>               OnProductLoaded;
    public static event Action<string>                     OnProductFailed;

    // Variants
    public static event Action<List<ProductVariantModel>>  OnVariantsLoaded;
    public static event Action<string>                     OnVariantsFailed;

    // Search
    public static event Action<List<ProductModel>>         OnSearchResultsLoaded;
    public static event Action<string>                     OnSearchFailed;

    // Loading state — bool = isLoading, string = message
    public static event Action<bool, string>               OnLoadingChanged;

    // ---------------------------------------------------------------
    // LIFECYCLE
    // ---------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------------------------------------------------------
    // CATEGORIES
    // ---------------------------------------------------------------

    public async Task LoadCategories()
    {
        if (!IsRepositoryReady()) return;

        OnLoadingChanged?.Invoke(true, "Loading categories...");

        var categories = await FurnitureRepository.Instance.GetCategories();

        OnLoadingChanged?.Invoke(false, string.Empty);

        if (categories == null || categories.Count == 0)
        {
            string msg = "No categories available. Please try again.";
            Debug.LogWarning("[FurnitureDataService] No categories returned.");
            OnCategoriesFailed?.Invoke(msg);
            return;
        }

        Debug.Log($"[FurnitureDataService] Categories loaded: {categories.Count}");
        OnCategoriesLoaded?.Invoke(categories);
    }

    // ---------------------------------------------------------------
    // PRODUCTS BY CATEGORY
    // ---------------------------------------------------------------

    public async Task LoadProductsByCategory(string categoryId)
    {
        if (!IsRepositoryReady()) return;

        if (string.IsNullOrEmpty(categoryId))
        {
            OnProductsFailed?.Invoke("Invalid category.");
            return;
        }

        OnLoadingChanged?.Invoke(true, "Loading products...");

        var products = await FurnitureRepository.Instance
            .GetProductsByCategory(categoryId);

        OnLoadingChanged?.Invoke(false, string.Empty);

        if (products == null || products.Count == 0)
        {
            string msg = "No products found in this category.";
            Debug.LogWarning($"[FurnitureDataService] No products for category {categoryId}.");
            OnProductsFailed?.Invoke(msg);
            return;
        }

        Debug.Log($"[FurnitureDataService] Products loaded: {products.Count} " +
                  $"for category {categoryId}.");
        OnProductsLoaded?.Invoke(products, categoryId);
    }

    // ---------------------------------------------------------------
    // PRODUCTS BY SUBCATEGORY
    // ---------------------------------------------------------------

    public async Task LoadProductsBySubcategory(string categoryId, string subcategoryId)
    {
        if (!IsRepositoryReady()) return;

        if (string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(subcategoryId))
        {
            OnProductsFailed?.Invoke("Invalid category or subcategory.");
            return;
        }

        OnLoadingChanged?.Invoke(true, "Loading products...");

        var products = await FurnitureRepository.Instance
            .GetProductsBySubcategory(categoryId, subcategoryId);

        OnLoadingChanged?.Invoke(false, string.Empty);

        if (products == null || products.Count == 0)
        {
            string msg = "No products found in this subcategory.";
            Debug.LogWarning($"[FurnitureDataService] No products for subcategory {subcategoryId}.");
            OnProductsFailed?.Invoke(msg);
            return;
        }

        Debug.Log($"[FurnitureDataService] Products loaded: {products.Count} " +
                  $"for subcategory {subcategoryId}.");
        OnProductsLoaded?.Invoke(products, subcategoryId);
    }

    // ---------------------------------------------------------------
    // SINGLE PRODUCT
    // ---------------------------------------------------------------

    public async Task LoadProduct(string productId)
    {
        if (!IsRepositoryReady()) return;

        if (string.IsNullOrEmpty(productId))
        {
            OnProductFailed?.Invoke("Invalid product ID.");
            return;
        }

        OnLoadingChanged?.Invoke(true, "Loading product...");

        var product = await FurnitureRepository.Instance.GetProduct(productId);

        OnLoadingChanged?.Invoke(false, string.Empty);

        if (product == null)
        {
            string msg = "Product not found.";
            Debug.LogWarning($"[FurnitureDataService] Product {productId} not found.");
            OnProductFailed?.Invoke(msg);
            return;
        }

        Debug.Log($"[FurnitureDataService] Product loaded: {product.Name}");
        OnProductLoaded?.Invoke(product);
    }

    // ---------------------------------------------------------------
    // VARIANTS
    // ---------------------------------------------------------------

    public async Task LoadVariants(string productId)
    {
        if (!IsRepositoryReady()) return;

        if (string.IsNullOrEmpty(productId))
        {
            OnVariantsFailed?.Invoke("Invalid product ID.");
            return;
        }

        // No loading overlay for variants — loaded silently in background
        // while detail sheet is already open

        var variants = await FurnitureRepository.Instance.GetVariants(productId);

        if (variants == null || variants.Count == 0)
        {
            Debug.LogWarning($"[FurnitureDataService] No variants for product {productId}.");
            OnVariantsFailed?.Invoke("No variants available.");
            return;
        }

        Debug.Log($"[FurnitureDataService] Variants loaded: {variants.Count} " +
                  $"for product {productId}.");
        OnVariantsLoaded?.Invoke(variants);
    }

    // ---------------------------------------------------------------
    // SEARCH
    // ---------------------------------------------------------------

    public async Task Search(string searchTerm)
    {
        if (!IsRepositoryReady()) return;

        if (string.IsNullOrEmpty(searchTerm) || searchTerm.Trim().Length < 2)
        {
            OnSearchFailed?.Invoke("Please enter at least 2 characters.");
            return;
        }

        OnLoadingChanged?.Invoke(true, $"Searching for \"{searchTerm}\"...");

        var results = await FurnitureRepository.Instance
            .SearchProducts(searchTerm.Trim());

        OnLoadingChanged?.Invoke(false, string.Empty);

        if (results == null || results.Count == 0)
        {
            Debug.Log($"[FurnitureDataService] No results for '{searchTerm}'.");
            OnSearchFailed?.Invoke($"No results found for \"{searchTerm}\".");
            return;
        }

        Debug.Log($"[FurnitureDataService] Search returned {results.Count} results.");
        OnSearchResultsLoaded?.Invoke(results);
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private bool IsRepositoryReady()
    {
        if (FurnitureRepository.Instance == null)
        {
            Debug.LogError("[FurnitureDataService] FurnitureRepository not found in scene.");
            return false;
        }

        if (AwsManager.Instance == null || !AwsManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[FurnitureDataService] AWS not ready yet.");
            OnLoadingChanged?.Invoke(false, string.Empty);
            return false;
        }

        return true;
    }
}