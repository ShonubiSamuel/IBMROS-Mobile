using System.Collections.Generic;

public class SubcategoryModel
{
    public string SubcategoryId   { get; set; }
    public string SubcategoryName { get; set; }
    public string SubcategoryUrl  { get; set; }
}

public class CategoryModel
{
    public string MerchantId   { get; set; }
    public string CategoryId   { get; set; }
    public string CategoryName { get; set; }
    public string CategoryUrl  { get; set; }

    public List<SubcategoryModel> Subcategories { get; set; } = new();
}