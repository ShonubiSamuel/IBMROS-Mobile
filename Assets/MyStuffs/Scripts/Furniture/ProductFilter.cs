using System.Collections.Generic;

/// <summary>
/// Filters out non-placeable products from the dataset.
/// Based on the issues identified in the ML team feedback.
/// </summary>
public static class ProductFilter
{
    // Product names that are accessories or parts, not furniture
    private static readonly HashSet<string> BlockedProductNames = new(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "NYTTIG FIL 400",
        "NYTTIG",
        "UPPDATERA",
        "HÅLLBAR",
        "BILLSBRO",
        "GRANHULT",
        "LÄTTHET",
        "KONSTRUERA",
        "GULSPARV",
        "DUVHOLMEN",
        "KÖSSEBÄR",
        "DVÄRGSTUBB",
        "SKYTTA",
        "TAGELSÄV",
    };

    // Keywords in description that indicate non-furniture items
    private static readonly List<string> BlockedDescriptionKeywords = new()
    {
        "filter",
        "charcoal filter",
        "plate holder",
        "hob separator",
        "handle",
        "jointing bracket",
        "blanket",
        "inner cushion",
        "resealable bag",
        "glass,",
        "jar with lid",
        "mattress protector",
        "ceiling height reducer",
        "drawer without front",
        "leg,",
        "legs,",
    };

    public static bool IsPlaceableProduct(ProductModel product)
    {
        if (product == null) return false;
        if (string.IsNullOrEmpty(product.Name)) return false;

        // Block by exact product name
        if (BlockedProductNames.Contains(product.Name.Trim()))
            return false;

        // Block by description keyword
        if (!string.IsNullOrEmpty(product.Description))
        {
            string descLower = product.Description.ToLower();
            foreach (var keyword in BlockedDescriptionKeywords)
            {
                if (descLower.Contains(keyword))
                    return false;
            }
        }

        return true;
    }
}