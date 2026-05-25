using System.Collections.Generic;

// ============================================
// FURNITURE CATALOG MODELS
// Plain C# data classes that mirror the shape
// of the DynamoDB catalog records.
//
// These are intentionally dumb data holders.
// No Unity dependencies, no MonoBehaviour.
// Swap FurniturePlaceholderData for a real
// DynamoDB service call and nothing here changes.
// ============================================

// ============================================
// LEVEL 1 - ROOM TYPE
// e.g. Living Room, Bedroom, Kitchen
// ============================================
public class RoomTypeModel
{
    // Unique identifier stored in DynamoDB
    // e.g. "living_room", "bedroom", "kitchen"
    public string Id { get; set; }

    // Display name shown in the grid card
    // e.g. "Living Room"
    public string DisplayName { get; set; }

    // Emoji or icon key used as the card image
    // until real asset URLs are available
    // e.g. "🛋"
    public string IconEmoji { get; set; }

    // URL to the category thumbnail image from S3/CloudFront
    // Empty string means fall back to IconEmoji
    public string ThumbnailUrl { get; set; }

    // Ordered list of categories inside this room type
    public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();

    public RoomTypeModel() { }

    public RoomTypeModel(string id, string displayName, string iconEmoji)
    {
        Id = id;
        DisplayName = displayName;
        IconEmoji = iconEmoji;
        ThumbnailUrl = string.Empty;
    }
}

// ============================================
// LEVEL 2 - CATEGORY
// e.g. Seating Furniture, Tables and Chairs,
//      Lighting, Electronic Devices
// ============================================
public class CategoryModel
{
    // Unique identifier stored in DynamoDB
    // e.g. "seating_furniture", "lighting"
    public string Id { get; set; }

    // Parent room type id this category belongs to
    // e.g. "living_room"
    public string RoomTypeId { get; set; }

    // Display name shown in the grid card
    // e.g. "Seating Furniture"
    public string DisplayName { get; set; }

    // Emoji or icon key used as the card image
    public string IconEmoji { get; set; }

    // URL to the category thumbnail image from S3/CloudFront
    public string ThumbnailUrl { get; set; }

    // Ordered list of subcategories inside this category
    public List<SubcategoryModel> Subcategories { get; set; } = new List<SubcategoryModel>();

    public CategoryModel() { }

    public CategoryModel(string id, string roomTypeId, string displayName, string iconEmoji)
    {
        Id = id;
        RoomTypeId = roomTypeId;
        DisplayName = displayName;
        IconEmoji = iconEmoji;
        ThumbnailUrl = string.Empty;
    }
}

// ============================================
// LEVEL 3 - SUBCATEGORY / TYPE
// e.g. Sofa, Armchair, Coffee Table,
//      Bean Bag, Rocking Chair
// ============================================
public class SubcategoryModel
{
    // Unique identifier stored in DynamoDB
    // e.g. "sofa", "armchair", "coffee_table"
    public string Id { get; set; }

    // Parent category id this subcategory belongs to
    // e.g. "seating_furniture"
    public string CategoryId { get; set; }

    // Display name shown in the grid card
    // e.g. "Armchair"
    public string DisplayName { get; set; }

    // Emoji or icon key used as the card image
    public string IconEmoji { get; set; }

    // URL to the subcategory thumbnail image from S3/CloudFront
    public string ThumbnailUrl { get; set; }

    public SubcategoryModel() { }

    public SubcategoryModel(string id, string categoryId, string displayName, string iconEmoji)
    {
        Id = id;
        CategoryId = categoryId;
        DisplayName = displayName;
        IconEmoji = iconEmoji;
        ThumbnailUrl = string.Empty;
    }
}

// ============================================
// LEVEL 4 - PRODUCT
// An individual furniture item with full details
// This is what gets placed in the 3D room
// ============================================
public class ProductModel
{
    // Unique identifier stored in DynamoDB
    // e.g. "ikea_tullsta_beige"
    public string Id { get; set; }

    // Parent subcategory id this product belongs to
    // e.g. "armchair"
    public string SubcategoryId { get; set; }

    // Brand name displayed in bold on the product card
    // e.g. "IKEA"
    public string Brand { get; set; }

    // Product name displayed below brand on the product card
    // e.g. "Tullsta"
    public string Name { get; set; }

    // Optional variant or series label
    // e.g. "VICTORINE", "POÄNG"
    public string VariantLabel { get; set; }

    // Real world dimensions shown under the name
    // Width x Depth in inches or cm depending on region
    // e.g. "31x28 in"
    public string Dimensions { get; set; }

    // Price as a formatted string
    // e.g. "$299" or "£199"
    public string Price { get; set; }

    // URL to the product thumbnail image from S3/CloudFront
    // Used in the product list card
    public string ThumbnailUrl { get; set; }

    // Filename of the GLB 3D model stored in S3
    // e.g. "ikea_tullsta_beige.glb"
    // FurnitureModelLoader uses this to download and cache the model
    public string ModelFileName { get; set; }

    // Available color variants for this product
    // Each variant is a separate ProductModel with its own ModelFileName
    public List<ProductColorVariant> ColorVariants { get; set; } = new List<ProductColorVariant>();

    // Tags used for filtering and search
    // e.g. ["popular", "recommended", "new"]
    public List<string> Tags { get; set; } = new List<string>();

    // Whether this product is marked as recommended
    // Maps to the RECOMMENDED badge in the reference images
    public bool IsRecommended { get; set; }

    // Whether this product is newly added
    // Maps to the NEW badge in the reference images
    public bool IsNew { get; set; }

    // Whether the user has starred/favourited this product
    // Persisted locally and synced to DynamoDB per user
    public bool IsFavourited { get; set; }

    public ProductModel() { }

    public ProductModel(
        string id,
        string subcategoryId,
        string brand,
        string name,
        string dimensions,
        string modelFileName)
    {
        Id = id;
        SubcategoryId = subcategoryId;
        Brand = brand;
        Name = name;
        Dimensions = dimensions;
        ModelFileName = modelFileName;
        ThumbnailUrl = string.Empty;
        VariantLabel = string.Empty;
        Price = string.Empty;
    }
}

// ============================================
// COLOR VARIANT
// Represents one color option for a product
// e.g. the beige, grey, and blue versions
// of the same IKEA Tullsta chair
// ============================================
public class ProductColorVariant
{
    // The product id of this specific variant
    // Links to a separate ProductModel with its own ModelFileName
    public string ProductId { get; set; }

    // Hex color string shown as the swatch circle in the UI
    // e.g. "#F5F5DC" for beige, "#808080" for grey
    public string HexColor { get; set; }

    // Human readable color name for accessibility
    // e.g. "Beige", "Grey", "Blue"
    public string ColorName { get; set; }

    public ProductColorVariant() { }

    public ProductColorVariant(string productId, string hexColor, string colorName)
    {
        ProductId = productId;
        HexColor = hexColor;
        ColorName = colorName;
    }
}

// ============================================
// FILTER OPTION
// Represents one chip in the filter row at
// the top of the product list view
// e.g. "Popular", "New", filter icon
// ============================================
public class FilterOption
{
    // Unique key used to filter the product list
    // e.g. "popular", "new", "recommended"
    public string Key { get; set; }

    // Label shown on the chip button
    // e.g. "Popular", "New"
    public string Label { get; set; }

    // Whether this filter is currently active
    public bool IsActive { get; set; }

    public FilterOption() { }

    public FilterOption(string key, string label, bool isActive = false)
    {
        Key = key;
        Label = label;
        IsActive = isActive;
    }
}

// ============================================
// NAVIGATION ENTRY
// One item in the browser navigation stack.
// FurnitureBrowserNav pushes and pops these
// as the user drills down and goes back.
// ============================================
public class BrowserNavEntry
{
    public BrowserLevel Level { get; set; }

    // The id of the item selected at this level
    // null means we are at the root room type list
    public string SelectedId { get; set; }

    // The title shown in the panel header at this level
    // e.g. "Living Room", "Seating Furniture"
    public string HeaderTitle { get; set; }

    public BrowserNavEntry(BrowserLevel level, string selectedId, string headerTitle)
    {
        Level = level;
        SelectedId = selectedId;
        HeaderTitle = headerTitle;
    }
}

// ============================================
// BROWSER LEVEL ENUM
// Tracks which depth of the hierarchy is
// currently visible in the browser panel
// ============================================
public enum BrowserLevel
{
    RoomType    = 0,   // Level 1 - room type grid
    Category    = 1,   // Level 2 - category grid
    Subcategory = 2,   // Level 3 - subcategory grid
    ProductList = 3    // Level 4 - product list with filters
}