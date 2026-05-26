public class ProductVariantModel
{
    public string ProductId          { get; set; }
    public string VariantId          { get; set; }
    public string ArticleNumber      { get; set; }
    public string CategoryId         { get; set; }
    public string SubcategoryId      { get; set; }
    public string ProductName        { get; set; }
    public string ProductDescription { get; set; }
    public string VariantType        { get; set; }  // "color" or "none"
    public string VariantValue       { get; set; }  // e.g. "White", "Dark grey/metal black"
    public string ImageUrl           { get; set; }
    public int    RawPrice           { get; set; }  // stored in pence e.g. 7272

    // Converts pence to pounds for display e.g. "£72.72"
    public string FormattedPrice =>
        RawPrice > 0 ? $"£{RawPrice / 100f:F2}" : "Price unavailable";

    // True if this variant has a distinct colour option
    public bool HasColourVariant =>
        VariantType == "color" && !string.IsNullOrEmpty(VariantValue);
}