using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using UnityEngine;

/// <summary>
/// Direct DynamoDB access layer for furniture data.
/// Mirrors the pattern of AuthManager — raw AWS calls, returns result objects.
/// Never referenced by UI directly. FurnitureDataService wraps this.
/// </summary>
public class FurnitureRepository : MonoBehaviour
{
    public static FurnitureRepository Instance { get; private set; }

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

    /// <summary>
    /// Fetches all categories from DynamoDB.
    /// Returns only categories that map to the app's UI taxonomy.
    /// </summary>
    public async Task<List<CategoryModel>> GetCategories(string merchantId = "0001")
    {
        try
        {
            var request = new QueryRequest
            {
                TableName              = AwsConfig.CategoriesTableName,
                KeyConditionExpression = "merchant_id = :mid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":mid", new AttributeValue { S = merchantId } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .QueryAsync(request);

            var categories = new List<CategoryModel>();

            foreach (var item in response.Items)
            {
                var category = ParseCategory(item);
                if (category == null) continue;

                if (CategoryMapper.IsRelevantCategory(category.CategoryId))
                    categories.Add(category);
            }

            Debug.Log($"[FurnitureRepository] Fetched {categories.Count} relevant categories.");
            return categories;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] GetCategories error: {e.Message}");
            return new List<CategoryModel>();
        }
    }
    // ---------------------------------------------------------------
    // PRODUCTS BY CATEGORY
    // ---------------------------------------------------------------

    /// <summary>
    /// Fetches all products belonging to a given category ID.
    /// Filters out non-placeable products automatically.
    /// </summary>
    public async Task<List<ProductModel>> GetProductsByCategory(
        string categoryId, string merchantId = "0001")
    {
        try
        {
            var request = new QueryRequest
            {
                TableName              = AwsConfig.ProductsTableName,
                IndexName              = "MerchantCategoryIndex",
                KeyConditionExpression = "merchant_id = :mid AND category_id = :catId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":mid",   new AttributeValue { S = merchantId } },
                    { ":catId", new AttributeValue { S = categoryId } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .QueryAsync(request);

            var products = new List<ProductModel>();

            foreach (var item in response.Items)
            {
                var product = ParseProduct(item);
                if (product == null) continue;

                if (!ProductFilter.IsPlaceableProduct(product))
                {
                    Debug.Log($"[FurnitureRepository] Filtered out: {product.Name}");
                    continue;
                }

                products.Add(product);
            }

            Debug.Log($"[FurnitureRepository] Fetched {products.Count} products " +
                      $"for category {categoryId}.");
            return products;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] GetProductsByCategory error: {e.Message}");
            return new List<ProductModel>();
        }
    }
    // ---------------------------------------------------------------
    // PRODUCTS BY SUBCATEGORY
    // ---------------------------------------------------------------

    /// <summary>
    /// Fetches products filtered by both category and subcategory.
    /// </summary>
    public async Task<List<ProductModel>> GetProductsBySubcategory(
        string categoryId, string subcategoryId, string merchantId = "0001")
    {
        try
        {
            var request = new QueryRequest
            {
                TableName              = AwsConfig.ProductsTableName,
                IndexName              = "MerchantCategoryIndex",
                KeyConditionExpression = "merchant_id = :mid AND category_id = :catId",
                FilterExpression       = "subcategory_id = :subId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":mid",   new AttributeValue { S = merchantId } },
                    { ":catId", new AttributeValue { S = categoryId } },
                    { ":subId", new AttributeValue { S = subcategoryId } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .QueryAsync(request);

            var products = new List<ProductModel>();

            foreach (var item in response.Items)
            {
                var product = ParseProduct(item);
                if (product == null) continue;

                if (!ProductFilter.IsPlaceableProduct(product))
                    continue;

                products.Add(product);
            }

            Debug.Log($"[FurnitureRepository] Fetched {products.Count} products " +
                      $"for subcategory {subcategoryId}.");
            return products;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] GetProductsBySubcategory error: {e.Message}");
            return new List<ProductModel>();
        }
    }
    
    // ---------------------------------------------------------------
    // SINGLE PRODUCT
    // ---------------------------------------------------------------

    /// <summary>
    /// Fetches a single product by its product ID.
    /// </summary>
    public async Task<ProductModel> GetProduct(string productId)
    {
        try
        {
            var request = new GetItemRequest
            {
                TableName = AwsConfig.ProductsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "product_id", new AttributeValue { S = productId } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .GetItemAsync(request);

            if (!response.IsItemSet)
            {
                Debug.LogWarning($"[FurnitureRepository] Product {productId} not found.");
                return null;
            }

            return ParseProduct(response.Item);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] GetProduct error: {e.Message}");
            return null;
        }
    }

    // ---------------------------------------------------------------
    // VARIANTS
    // ---------------------------------------------------------------

    /// <summary>
    /// Fetches all variants for a given product ID.
    /// </summary>
    public async Task<List<ProductVariantModel>> GetVariants(string productId)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName              = AwsConfig.ProductVariantsTableName,
                KeyConditionExpression = "product_id = :pid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pid", new AttributeValue { S = productId } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .QueryAsync(request);

            var variants = new List<ProductVariantModel>();

            foreach (var item in response.Items)
            {
                var variant = ParseVariant(item);
                if (variant != null)
                    variants.Add(variant);
            }

            Debug.Log($"[FurnitureRepository] Fetched {variants.Count} " +
                      $"variants for product {productId}.");
            return variants;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] GetVariants error: {e.Message}");
            return new List<ProductVariantModel>();
        }
    }

    // ---------------------------------------------------------------
    // SEARCH
    // ---------------------------------------------------------------

    /// <summary>
    /// Scans the products table for items whose name contains the search term.
    /// Note: DynamoDB scan is expensive — consider ElasticSearch for production.
    /// </summary>
    public async Task<List<ProductModel>> SearchProducts(
        string searchTerm, string merchantId = "0001")
    {
        try
        {
            // Search across all categories for this merchant
            // Still a scan but limited to one merchant's products
            var request = new ScanRequest
            {
                TableName        = AwsConfig.ProductsTableName,
                FilterExpression = "merchant_id = :mid AND contains(#nm, :term)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#nm", "name" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":mid",  new AttributeValue { S = merchantId } },
                    { ":term", new AttributeValue { S = searchTerm.ToUpper() } }
                }
            };

            var response = await AwsManager.Instance.DynamoDBClient
                .ScanAsync(request);

            var products = new List<ProductModel>();

            foreach (var item in response.Items)
            {
                var product = ParseProduct(item);
                if (product != null && ProductFilter.IsPlaceableProduct(product))
                    products.Add(product);
            }

            Debug.Log($"[FurnitureRepository] Search '{searchTerm}' " +
                      $"returned {products.Count} results.");
            return products;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] SearchProducts error: {e.Message}");
            return new List<ProductModel>();
        }
    }
    
    // ---------------------------------------------------------------
    // PARSERS
    // ---------------------------------------------------------------

    private CategoryModel ParseCategory(Dictionary<string, AttributeValue> item)
    {
        try
        {
            var category = new CategoryModel
            {
                MerchantId   = GetString(item, "merchant_id"),
                CategoryId   = GetString(item, "category_id"),
                CategoryName = GetString(item, "category_name"),
                CategoryUrl  = GetString(item, "category_url"),
                Subcategories = new List<SubcategoryModel>()
            };

            // Parse subcategories from DynamoDB List attribute
            if (item.TryGetValue("subcategories", out var subsAttr) &&
                subsAttr.L != null)
            {
                foreach (var subAttr in subsAttr.L)
                {
                    if (subAttr.M == null) continue;

                    var sub = new SubcategoryModel();

                    // Handle both raw DynamoDB format {"S":"value"} and plain strings
                    if (subAttr.M.TryGetValue("subcategory_id", out var idAttr))
                        sub.SubcategoryId = idAttr.M != null
                            ? GetString(idAttr.M, "S")
                            : idAttr.S;

                    if (subAttr.M.TryGetValue("subcategory_name", out var nameAttr))
                        sub.SubcategoryName = nameAttr.M != null
                            ? GetString(nameAttr.M, "S")
                            : nameAttr.S;

                    if (subAttr.M.TryGetValue("subcategory_url", out var urlAttr))
                        sub.SubcategoryUrl = urlAttr.M != null
                            ? GetString(urlAttr.M, "S")
                            : urlAttr.S;

                    if (!string.IsNullOrEmpty(sub.SubcategoryId))
                        category.Subcategories.Add(sub);
                }
            }

            return category;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] ParseCategory error: {e.Message}");
            return null;
        }
    }

    private ProductModel ParseProduct(Dictionary<string, AttributeValue> item)
    {
        try
        {
            return new ProductModel
            {
                ProductId        = GetString(item, "product_id"),
                MerchantId       = GetString(item, "merchant_id"),
                ArticleNumber    = GetString(item, "article_number"),
                CategoryId       = GetString(item, "category_id"),
                SubcategoryId    = GetString(item, "subcategory_id"),
                Name             = GetString(item, "name"),
                Description      = GetString(item, "description"),
                Designer         = GetString(item, "designer"),
                Price            = GetString(item, "price"),
                ImageUrl         = GetString(item, "image_url"),
                S3ImageUrl       = GetString(item, "s3_image_url"),
                S3ModelUrl       = GetString(item, "s3_model_url"),
                StarRating       = GetString(item, "star_rating"),
                ReviewCount      = GetString(item, "review_count"),
                CareInstructions = GetString(item, "care_instructions"),
                ProductUrl       = GetString(item, "url"),
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] ParseProduct error: {e.Message}");
            return null;
        }
    }

    private ProductVariantModel ParseVariant(Dictionary<string, AttributeValue> item)
    {
        try
        {
            var rawPrice = GetString(item, "final_price");
            int parsedPrice = 0;

            if (!string.IsNullOrEmpty(rawPrice) && rawPrice != "N/A")
            {
                // Handle both integer pence (7272) and float (11.5)
                if (float.TryParse(rawPrice, out float floatPrice))
                    parsedPrice = Mathf.RoundToInt(floatPrice);
            }

            return new ProductVariantModel
            {
                ProductId          = GetString(item, "product_id"),
                VariantId          = GetString(item, "variant_id"),
                ArticleNumber      = GetString(item, "article_number"),
                CategoryId         = GetString(item, "category_id"),
                SubcategoryId      = GetString(item, "subcategory_id"),
                ProductName        = GetString(item, "product_name"),
                ProductDescription = GetString(item, "product_description"),
                VariantType        = GetString(item, "variant_type"),
                VariantValue       = GetString(item, "variant_value"),
                ImageUrl           = GetString(item, "image_url"),
                RawPrice           = parsedPrice,
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureRepository] ParseVariant error: {e.Message}");
            return null;
        }
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private string GetString(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var attr))
            return attr.S ?? string.Empty;

        return string.Empty;
    }
}