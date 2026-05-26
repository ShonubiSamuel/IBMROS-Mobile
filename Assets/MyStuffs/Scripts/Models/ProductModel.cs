public class ProductModel
{
    public string ProductId        { get; set; }
    public string MerchantId       { get; set; }
    public string ArticleNumber    { get; set; }
    public string CategoryId       { get; set; }
    public string SubcategoryId    { get; set; }
    public string Name             { get; set; }
    public string Description      { get; set; }
    public string Designer         { get; set; }
    public string Price            { get; set; }

    // IKEA CDN URL — may be rate limited in production
    public string ImageUrl         { get; set; }

    // Our S3 URL — preferred, populated once ML team runs image pipeline
    public string S3ImageUrl       { get; set; }

    // GLB model URL on CloudFront — empty until ML team populates
    public string S3ModelUrl       { get; set; }

    public string StarRating       { get; set; }
    public string ReviewCount      { get; set; }
    public string CareInstructions { get; set; }
    public string Materials        { get; set; }
    public string ProductUrl       { get; set; }

    // Returns the best available image URL
    // Prefers our S3 copy, falls back to IKEA CDN
    public string BestImageUrl =>
        !string.IsNullOrEmpty(S3ImageUrl) ? S3ImageUrl : ImageUrl;

    // True if a 3D model is available for scene placement
    public bool HasModel =>
        !string.IsNullOrEmpty(S3ModelUrl);

    // Parses star rating from "4.5/5" format to a float
    // Returns 0 if not available
    public float StarRatingValue
    {
        get
        {
            if (string.IsNullOrEmpty(StarRating) || StarRating == "N/A")
                return 0f;

            var parts = StarRating.Split('/');
            return float.TryParse(parts[0], out float val) ? val : 0f;
        }
    }

    // Parses review count — returns 0 if not available
    public int ReviewCountValue
    {
        get
        {
            if (string.IsNullOrEmpty(ReviewCount) || ReviewCount == "N/A")
                return 0;

            return int.TryParse(ReviewCount, out int val) ? val : 0;
        }
    }

    // Parses price from "£19" or "£1,899" format to float
    // Returns 0 if not available
    public float PriceValue
    {
        get
        {
            if (string.IsNullOrEmpty(Price) || Price == "N/A")
                return 0f;

            string cleaned = Price
                .Replace("£", "")
                .Replace("$", "")
                .Replace(",", "")
                .Trim();

            return float.TryParse(cleaned, out float val) ? val : 0f;
        }
    }

    // Formatted price string for display e.g. "£19.00"
    public string FormattedPrice
    {
        get
        {
            float val = PriceValue;
            return val > 0 ? $"£{val:F2}" : "Price unavailable";
        }
    }
}