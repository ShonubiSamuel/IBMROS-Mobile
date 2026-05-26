using System.Collections.Generic;

/// <summary>
/// Maps IKEA DynamoDB category IDs to the app's UI category names.
/// Filters out categories irrelevant to room design.
/// </summary>
public static class CategoryMapper
{
    // Maps IKEA category_id to app UI category name
    private static readonly Dictionary<string, string> CategoryIdToAppName = new()
    {
        { "0001", "Cabinets, Shelves" },   // Storage furniture
        { "0003", "Bed" },                  // Beds & mattresses
        { "0004", "Seating Furniture" },    // Living room seating
        { "0005", "Yard or Patio" },        // Outdoor living
        { "0007", "Home Office" },          // Home office furniture
        { "0008", "Decoration" },           // Home decoration
        { "0009", "Lighting" },             // Lighting
        { "0011", "Tables and Chairs" },    // Dining/chairs
        { "0016", "Decoration" },           // Plants and greenery
    };

    // Category IDs we want to show in the app
    private static readonly HashSet<string> RelevantCategoryIds = new()
    {
        "0001", // Storage furniture
        "0003", // Beds & mattresses
        "0004", // Living room seating
        "0005", // Outdoor living
        "0007", // Home office
        "0008", // Home decoration
        "0009", // Lighting
        "0011", // Tables & chairs
        "0016", // Plants & greenery
    };

    public static bool IsRelevantCategory(string categoryId)
    {
        return RelevantCategoryIds.Contains(categoryId);
    }

    public static string GetAppCategoryName(string categoryId)
    {
        return CategoryIdToAppName.TryGetValue(categoryId, out var name)
            ? name
            : null;
    }

    // Returns the emoji for the app category name
    public static string GetCategoryEmoji(string appCategoryName)
    {
        return appCategoryName switch
        {
            "Bed"                  => "🛏️",
            "Seating Furniture"    => "🛋️",
            "Tables and Chairs"    => "🪑",
            "Cabinets, Shelves"    => "🚪",
            "Decoration"           => "🪴",
            "Lighting"             => "💡",
            "Home Office"          => "💼",
            "Yard or Patio"        => "🌿",
            _                      => "📦",
        };
    }
}