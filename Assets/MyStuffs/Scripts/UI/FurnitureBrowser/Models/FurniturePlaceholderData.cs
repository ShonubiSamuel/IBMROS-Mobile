using System.Collections.Generic;

// ============================================
// FURNITURE PLACEHOLDER DATA
// Static catalog used before DynamoDB is wired.
// Structure mirrors the real DynamoDB schema
// exactly — swapping to live data is a one line
// change in FurnitureBrowserController.
//
// Rules enforced here:
//   - All ids are snake_case and globally unique
//   - Every product has a ModelFileName so
//     FurnitureModelLoader can request it
//   - Color variants reference real product ids
//   - Tags match the FilterOption keys exactly
// ============================================
public static class FurniturePlaceholderData
{
    // ============================================
    // PUBLIC ENTRY POINT
    // Returns the full catalog tree.
    // FurnitureBrowserController calls this once
    // on startup and caches the result.
    // ============================================
    public static List<RoomTypeModel> GetCatalog()
    {
        return new List<RoomTypeModel>
        {
            BuildLivingRoom(),
            BuildBedroom(),
            BuildDiningRoom(),
            BuildKitchen(),
            BuildHomeOffice(),
            BuildBathroom(),
            BuildOutdoor(),
        };
    }

    // ============================================
    // LOOKUP HELPERS
    // Flat lookups so controllers can jump
    // directly to any node without tree walks.
    // Built once and cached by the browser.
    // ============================================
    private static Dictionary<string, RoomTypeModel> _roomIndex;
    private static Dictionary<string, CategoryModel> _categoryIndex;
    private static Dictionary<string, SubcategoryModel> _subcategoryIndex;
    private static Dictionary<string, ProductModel> _productIndex;

    public static Dictionary<string, RoomTypeModel> GetRoomIndex()
    {
        if (_roomIndex != null)
            return _roomIndex;

        _roomIndex = new Dictionary<string, RoomTypeModel>();
        foreach (var room in GetCatalog())
            _roomIndex[room.Id] = room;

        return _roomIndex;
    }

    public static Dictionary<string, CategoryModel> GetCategoryIndex()
    {
        if (_categoryIndex != null)
            return _categoryIndex;

        _categoryIndex = new Dictionary<string, CategoryModel>();
        foreach (var room in GetCatalog())
            foreach (var cat in room.Categories)
                _categoryIndex[cat.Id] = cat;

        return _categoryIndex;
    }

    public static Dictionary<string, SubcategoryModel> GetSubcategoryIndex()
    {
        if (_subcategoryIndex != null)
            return _subcategoryIndex;

        _subcategoryIndex = new Dictionary<string, SubcategoryModel>();
        foreach (var room in GetCatalog())
            foreach (var cat in room.Categories)
                foreach (var sub in cat.Subcategories)
                    _subcategoryIndex[sub.Id] = sub;

        return _subcategoryIndex;
    }

    public static Dictionary<string, ProductModel> GetProductIndex()
    {
        if (_productIndex != null)
            return _productIndex;

        _productIndex = new Dictionary<string, ProductModel>();

        foreach (var room in GetCatalog())
            foreach (var cat in room.Categories)
                foreach (var sub in cat.Subcategories)
                    foreach (var product in GetProductsForSubcategory(sub.Id))
                        _productIndex[product.Id] = product;

        return _productIndex;
    }

    // ============================================
    // PRODUCTS ARE STORED FLAT BY SUBCATEGORY ID
    // This avoids embedding large product lists
    // inside the tree, keeping the tree lean for
    // navigation and loading products on demand.
    // ============================================
    public static List<ProductModel> GetProductsForSubcategory(string subcategoryId)
    {
        if (_productsBySubcategory.TryGetValue(subcategoryId, out var products))
            return products;

        return new List<ProductModel>();
    }

    // ============================================
    // DEFAULT FILTER OPTIONS
    // Returned for any product list view.
    // New instances every call so state is clean.
    // ============================================
    public static List<FilterOption> GetDefaultFilters()
    {
        return new List<FilterOption>
        {
            new FilterOption("popular",     "Popular",     isActive: true),
            new FilterOption("new",         "New",         isActive: false),
            new FilterOption("recommended", "Recommended", isActive: false),
        };
    }

    // ============================================
    // INVALIDATE CACHE
    // Call this when live DynamoDB data replaces
    // placeholder data so indexes rebuild cleanly.
    // ============================================
    public static void InvalidateCache()
    {
        _roomIndex        = null;
        _categoryIndex    = null;
        _subcategoryIndex = null;
        _productIndex     = null;
    }

    // ============================================================
    // ROOM BUILDERS
    // ============================================================

    private static RoomTypeModel BuildLivingRoom()
    {
        var room = new RoomTypeModel("living_room", "Living Room", "🛋");

        room.Categories.AddRange(new[]
        {
            BuildCategory_Seating("living_room"),
            BuildCategory_TablesAndChairs("living_room"),
            BuildCategory_Lighting("living_room"),
            BuildCategory_ElectronicDevices("living_room"),
            BuildCategory_Decoration("living_room"),
            BuildCategory_CabinetsShelves("living_room"),
            BuildCategory_Fireplaces("living_room"),
            BuildCategory_HobbyEntertainment("living_room"),
        });

        return room;
    }

    private static RoomTypeModel BuildBedroom()
    {
        var room = new RoomTypeModel("bedroom", "Bedroom", "🛏");

        room.Categories.AddRange(new[]
        {
            BuildCategory_Beds("bedroom"),
            BuildCategory_Storage("bedroom"),
            BuildCategory_Lighting("bedroom"),
            BuildCategory_Decoration("bedroom"),
            BuildCategory_CabinetsShelves("bedroom"),
        });

        return room;
    }

    private static RoomTypeModel BuildDiningRoom()
    {
        var room = new RoomTypeModel("dining_room", "Dining Room", "🍽");

        room.Categories.AddRange(new[]
        {
            BuildCategory_TablesAndChairs("dining_room"),
            BuildCategory_CabinetsShelves("dining_room"),
            BuildCategory_Lighting("dining_room"),
            BuildCategory_Decoration("dining_room"),
        });

        return room;
    }

    private static RoomTypeModel BuildKitchen()
    {
        var room = new RoomTypeModel("kitchen", "Kitchen", "🍳");

        room.Categories.AddRange(new[]
        {
            BuildCategory_KitchenStorage("kitchen"),
            BuildCategory_KitchenAppliances("kitchen"),
            BuildCategory_Lighting("kitchen"),
            BuildCategory_Decoration("kitchen"),
        });

        return room;
    }

    private static RoomTypeModel BuildHomeOffice()
    {
        var room = new RoomTypeModel("home_office", "Home Office", "💼");

        room.Categories.AddRange(new[]
        {
            BuildCategory_OfficeSeating("home_office"),
            BuildCategory_OfficeStorage("home_office"),
            BuildCategory_Lighting("home_office"),
            BuildCategory_ElectronicDevices("home_office"),
            BuildCategory_Decoration("home_office"),
        });

        return room;
    }

    private static RoomTypeModel BuildBathroom()
    {
        var room = new RoomTypeModel("bathroom", "Bathroom", "🚿");

        room.Categories.AddRange(new[]
        {
            BuildCategory_BathroomFurniture("bathroom"),
            BuildCategory_Lighting("bathroom"),
            BuildCategory_Decoration("bathroom"),
        });

        return room;
    }

    private static RoomTypeModel BuildOutdoor()
    {
        var room = new RoomTypeModel("outdoor", "Outdoor", "🌿");

        room.Categories.AddRange(new[]
        {
            BuildCategory_OutdoorSeating("outdoor"),
            BuildCategory_OutdoorTables("outdoor"),
            BuildCategory_Lighting("outdoor"),
            BuildCategory_Decoration("outdoor"),
        });

        return room;
    }

    // ============================================================
    // SHARED CATEGORY BUILDERS
    // Reused across rooms. The roomTypeId parameter scopes the
    // category id so "living_room_seating" and "bedroom_seating"
    // are distinct nodes in the tree and index.
    // ============================================================

    private static CategoryModel BuildCategory_Seating(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_seating",
            roomTypeId,
            "Seating Furniture",
            "🛋");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_seating_sofa",               $"{roomTypeId}_seating", "Sofa",                "🛋"),
            new SubcategoryModel($"{roomTypeId}_seating_armchair",           $"{roomTypeId}_seating", "Armchair",            "💺"),
            new SubcategoryModel($"{roomTypeId}_seating_chaise",             $"{roomTypeId}_seating", "Chaise Longue",       "🛋"),
            new SubcategoryModel($"{roomTypeId}_seating_bean_bag",           $"{roomTypeId}_seating", "Bean Bag",            "🫘"),
            new SubcategoryModel($"{roomTypeId}_seating_pouf",               $"{roomTypeId}_seating", "Pouf & Footstool",    "🪑"),
            new SubcategoryModel($"{roomTypeId}_seating_rocking_chair",      $"{roomTypeId}_seating", "Rocking Chair",       "🪑"),
            new SubcategoryModel($"{roomTypeId}_seating_outdoor_furniture",  $"{roomTypeId}_seating", "Outdoor Furniture",   "☀️"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_TablesAndChairs(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_tables_chairs",
            roomTypeId,
            "Tables and Chairs",
            "🪑");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_tables_chairs_dining_table",  $"{roomTypeId}_tables_chairs", "Dining Table",   "🍽"),
            new SubcategoryModel($"{roomTypeId}_tables_chairs_coffee_table",  $"{roomTypeId}_tables_chairs", "Coffee Table",   "☕"),
            new SubcategoryModel($"{roomTypeId}_tables_chairs_side_table",    $"{roomTypeId}_tables_chairs", "Side Table",     "🟫"),
            new SubcategoryModel($"{roomTypeId}_tables_chairs_dining_chair",  $"{roomTypeId}_tables_chairs", "Dining Chair",   "🪑"),
            new SubcategoryModel($"{roomTypeId}_tables_chairs_bench",         $"{roomTypeId}_tables_chairs", "Bench",          "🪵"),
            new SubcategoryModel($"{roomTypeId}_tables_chairs_bar_stool",     $"{roomTypeId}_tables_chairs", "Bar Stool",      "🍸"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_Lighting(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_lighting",
            roomTypeId,
            "Lighting",
            "💡");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_lighting_floor_lamp",    $"{roomTypeId}_lighting", "Floor Lamp",     "🕯"),
            new SubcategoryModel($"{roomTypeId}_lighting_table_lamp",    $"{roomTypeId}_lighting", "Table Lamp",     "💡"),
            new SubcategoryModel($"{roomTypeId}_lighting_ceiling_light", $"{roomTypeId}_lighting", "Ceiling Light",  "🔆"),
            new SubcategoryModel($"{roomTypeId}_lighting_pendant",       $"{roomTypeId}_lighting", "Pendant Light",  "🏮"),
            new SubcategoryModel($"{roomTypeId}_lighting_wall_light",    $"{roomTypeId}_lighting", "Wall Light",     "🕯"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_ElectronicDevices(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_electronics",
            roomTypeId,
            "Electronic Devices",
            "📺");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_electronics_tv",           $"{roomTypeId}_electronics", "Television",     "📺"),
            new SubcategoryModel($"{roomTypeId}_electronics_speaker",      $"{roomTypeId}_electronics", "Speaker",        "🔊"),
            new SubcategoryModel($"{roomTypeId}_electronics_monitor",      $"{roomTypeId}_electronics", "Monitor",        "🖥"),
            new SubcategoryModel($"{roomTypeId}_electronics_gaming",       $"{roomTypeId}_electronics", "Gaming Setup",   "🎮"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_Decoration(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_decoration",
            roomTypeId,
            "Decoration",
            "🌿");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_decoration_plant",         $"{roomTypeId}_decoration", "Plants",             "🌿"),
            new SubcategoryModel($"{roomTypeId}_decoration_rug",           $"{roomTypeId}_decoration", "Rugs",               "🟥"),
            new SubcategoryModel($"{roomTypeId}_decoration_wall_art",      $"{roomTypeId}_decoration", "Wall Art",           "🖼"),
            new SubcategoryModel($"{roomTypeId}_decoration_mirror",        $"{roomTypeId}_decoration", "Mirrors",            "🪞"),
            new SubcategoryModel($"{roomTypeId}_decoration_cushion",       $"{roomTypeId}_decoration", "Cushions & Throws",  "🛋"),
            new SubcategoryModel($"{roomTypeId}_decoration_vase",          $"{roomTypeId}_decoration", "Vases & Bowls",      "🏺"),
            new SubcategoryModel($"{roomTypeId}_decoration_clock",         $"{roomTypeId}_decoration", "Clocks",             "🕐"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_CabinetsShelves(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_cabinets_shelves",
            roomTypeId,
            "Cabinets & Shelves",
            "🗄");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_cabinets_shelves_bookcase",    $"{roomTypeId}_cabinets_shelves", "Bookcase",        "📚"),
            new SubcategoryModel($"{roomTypeId}_cabinets_shelves_tv_unit",     $"{roomTypeId}_cabinets_shelves", "TV Unit",         "📺"),
            new SubcategoryModel($"{roomTypeId}_cabinets_shelves_sideboard",   $"{roomTypeId}_cabinets_shelves", "Sideboard",       "🗄"),
            new SubcategoryModel($"{roomTypeId}_cabinets_shelves_wall_shelf",  $"{roomTypeId}_cabinets_shelves", "Wall Shelf",      "📦"),
            new SubcategoryModel($"{roomTypeId}_cabinets_shelves_display",     $"{roomTypeId}_cabinets_shelves", "Display Cabinet", "🏆"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_Fireplaces(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_fireplaces",
            roomTypeId,
            "Fireplaces",
            "🔥");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_fireplaces_electric",  $"{roomTypeId}_fireplaces", "Electric Fireplace", "⚡"),
            new SubcategoryModel($"{roomTypeId}_fireplaces_bio",       $"{roomTypeId}_fireplaces", "Bio Fireplace",      "🔥"),
            new SubcategoryModel($"{roomTypeId}_fireplaces_surround",  $"{roomTypeId}_fireplaces", "Fireplace Surround", "🧱"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_HobbyEntertainment(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_hobby_entertainment",
            roomTypeId,
            "Hobby & Entertainment",
            "🎸");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_hobby_entertainment_bar",       $"{roomTypeId}_hobby_entertainment", "Home Bar",      "🍸"),
            new SubcategoryModel($"{roomTypeId}_hobby_entertainment_cinema",    $"{roomTypeId}_hobby_entertainment", "Home Cinema",   "🎬"),
            new SubcategoryModel($"{roomTypeId}_hobby_entertainment_gym",       $"{roomTypeId}_hobby_entertainment", "Home Gym",      "🏋"),
            new SubcategoryModel($"{roomTypeId}_hobby_entertainment_billiards", $"{roomTypeId}_hobby_entertainment", "Billiards",     "🎱"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_Beds(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_beds",
            roomTypeId,
            "Beds",
            "🛏");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_beds_double",    $"{roomTypeId}_beds", "Double Bed",   "🛏"),
            new SubcategoryModel($"{roomTypeId}_beds_single",    $"{roomTypeId}_beds", "Single Bed",   "🛏"),
            new SubcategoryModel($"{roomTypeId}_beds_bunk",      $"{roomTypeId}_beds", "Bunk Bed",     "🛏"),
            new SubcategoryModel($"{roomTypeId}_beds_sofa_bed",  $"{roomTypeId}_beds", "Sofa Bed",     "🛋"),
            new SubcategoryModel($"{roomTypeId}_beds_headboard", $"{roomTypeId}_beds", "Headboard",    "🟫"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_Storage(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_storage",
            roomTypeId,
            "Storage",
            "🗄");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_storage_wardrobe",   $"{roomTypeId}_storage", "Wardrobe",        "👔"),
            new SubcategoryModel($"{roomTypeId}_storage_dresser",    $"{roomTypeId}_storage", "Dresser",         "🗄"),
            new SubcategoryModel($"{roomTypeId}_storage_chest",      $"{roomTypeId}_storage", "Chest of Drawers","📦"),
            new SubcategoryModel($"{roomTypeId}_storage_bedside",    $"{roomTypeId}_storage", "Bedside Table",   "🟫"),
            new SubcategoryModel($"{roomTypeId}_storage_ottoman",    $"{roomTypeId}_storage", "Ottoman",         "🪑"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_KitchenStorage(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_kitchen_storage",
            roomTypeId,
            "Kitchen Storage",
            "🍳");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_kitchen_storage_island",     $"{roomTypeId}_kitchen_storage", "Kitchen Island",    "🏝"),
            new SubcategoryModel($"{roomTypeId}_kitchen_storage_cabinet",    $"{roomTypeId}_kitchen_storage", "Kitchen Cabinet",   "🗄"),
            new SubcategoryModel($"{roomTypeId}_kitchen_storage_pantry",     $"{roomTypeId}_kitchen_storage", "Pantry Unit",       "📦"),
            new SubcategoryModel($"{roomTypeId}_kitchen_storage_cart",       $"{roomTypeId}_kitchen_storage", "Kitchen Cart",      "🛒"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_KitchenAppliances(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_kitchen_appliances",
            roomTypeId,
            "Appliances",
            "🧊");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_kitchen_appliances_fridge",       $"{roomTypeId}_kitchen_appliances", "Refrigerator",  "🧊"),
            new SubcategoryModel($"{roomTypeId}_kitchen_appliances_oven",         $"{roomTypeId}_kitchen_appliances", "Oven & Stove",  "🔥"),
            new SubcategoryModel($"{roomTypeId}_kitchen_appliances_dishwasher",   $"{roomTypeId}_kitchen_appliances", "Dishwasher",    "🫧"),
            new SubcategoryModel($"{roomTypeId}_kitchen_appliances_microwave",    $"{roomTypeId}_kitchen_appliances", "Microwave",     "📡"),
            new SubcategoryModel($"{roomTypeId}_kitchen_appliances_coffee",       $"{roomTypeId}_kitchen_appliances", "Coffee Machine","☕"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_OfficeSeating(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_office_seating",
            roomTypeId,
            "Office Seating",
            "💺");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_office_seating_task_chair",   $"{roomTypeId}_office_seating", "Task Chair",    "💺"),
            new SubcategoryModel($"{roomTypeId}_office_seating_gaming_chair", $"{roomTypeId}_office_seating", "Gaming Chair",  "🎮"),
            new SubcategoryModel($"{roomTypeId}_office_seating_stool",        $"{roomTypeId}_office_seating", "Stool",         "🪑"),
            new SubcategoryModel($"{roomTypeId}_office_seating_sofa",         $"{roomTypeId}_office_seating", "Office Sofa",   "🛋"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_OfficeStorage(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_office_storage",
            roomTypeId,
            "Office Storage",
            "📁");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_office_storage_desk",        $"{roomTypeId}_office_storage", "Desk",           "🖥"),
            new SubcategoryModel($"{roomTypeId}_office_storage_bookcase",    $"{roomTypeId}_office_storage", "Bookcase",       "📚"),
            new SubcategoryModel($"{roomTypeId}_office_storage_filing",      $"{roomTypeId}_office_storage", "Filing Cabinet", "📁"),
            new SubcategoryModel($"{roomTypeId}_office_storage_shelf",       $"{roomTypeId}_office_storage", "Wall Shelf",     "📦"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_BathroomFurniture(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_bathroom_furniture",
            roomTypeId,
            "Bathroom Furniture",
            "🚿");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_bathroom_furniture_vanity",    $"{roomTypeId}_bathroom_furniture", "Vanity Unit",    "🪞"),
            new SubcategoryModel($"{roomTypeId}_bathroom_furniture_cabinet",   $"{roomTypeId}_bathroom_furniture", "Bathroom Cabinet","🗄"),
            new SubcategoryModel($"{roomTypeId}_bathroom_furniture_towel",     $"{roomTypeId}_bathroom_furniture", "Towel Rail",     "🧺"),
            new SubcategoryModel($"{roomTypeId}_bathroom_furniture_hamper",    $"{roomTypeId}_bathroom_furniture", "Laundry Hamper", "🧺"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_OutdoorSeating(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_outdoor_seating",
            roomTypeId,
            "Outdoor Seating",
            "☀️");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_outdoor_seating_lounger",    $"{roomTypeId}_outdoor_seating", "Sun Lounger",    "🏖"),
            new SubcategoryModel($"{roomTypeId}_outdoor_seating_sofa",       $"{roomTypeId}_outdoor_seating", "Garden Sofa",    "🛋"),
            new SubcategoryModel($"{roomTypeId}_outdoor_seating_chair",      $"{roomTypeId}_outdoor_seating", "Garden Chair",   "🪑"),
            new SubcategoryModel($"{roomTypeId}_outdoor_seating_hammock",    $"{roomTypeId}_outdoor_seating", "Hammock",        "🌴"),
            new SubcategoryModel($"{roomTypeId}_outdoor_seating_bench",      $"{roomTypeId}_outdoor_seating", "Garden Bench",   "🪵"),
        });

        return cat;
    }

    private static CategoryModel BuildCategory_OutdoorTables(string roomTypeId)
    {
        var cat = new CategoryModel(
            $"{roomTypeId}_outdoor_tables",
            roomTypeId,
            "Outdoor Tables",
            "🌳");

        cat.Subcategories.AddRange(new[]
        {
            new SubcategoryModel($"{roomTypeId}_outdoor_tables_dining",   $"{roomTypeId}_outdoor_tables", "Garden Dining Table", "🍽"),
            new SubcategoryModel($"{roomTypeId}_outdoor_tables_side",     $"{roomTypeId}_outdoor_tables", "Garden Side Table",   "🟫"),
            new SubcategoryModel($"{roomTypeId}_outdoor_tables_bbq",      $"{roomTypeId}_outdoor_tables", "BBQ & Grill",         "🔥"),
            new SubcategoryModel($"{roomTypeId}_outdoor_tables_parasol",  $"{roomTypeId}_outdoor_tables", "Parasol & Base",      "☂️"),
        });

        return cat;
    }

    // ============================================================
    // PRODUCT DATA — STORED FLAT BY SUBCATEGORY ID
    // Only living room seating products are fully populated here
    // as a complete reference implementation.
    // All other subcategories return an empty list until real
    // DynamoDB data is connected, preventing null errors.
    // ============================================================
    private static readonly Dictionary<string, List<ProductModel>> _productsBySubcategory
        = new Dictionary<string, List<ProductModel>>
    {
        // ----------------------------------------------------------
        // LIVING ROOM > SEATING > SOFA
        // ----------------------------------------------------------
        ["living_room_seating_sofa"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "sofa_ikea_kivik_beige",
                subcategoryId: "living_room_seating_sofa",
                brand:         "IKEA",
                name:          "KIVIK",
                dimensions:    "90x57 in",
                modelFileName: "sofa_ikea_kivik_beige.glb")
            {
                Price        = "$649",
                VariantLabel = string.Empty,
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "popular", "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("sofa_ikea_kivik_beige",  "#D4C5A9", "Beige"),
                    new ProductColorVariant("sofa_ikea_kivik_grey",   "#8A8E91", "Grey"),
                    new ProductColorVariant("sofa_ikea_kivik_blue",   "#3A5A7C", "Blue"),
                    new ProductColorVariant("sofa_ikea_kivik_green",  "#4A6741", "Green"),
                }
            },
            new ProductModel(
                id:            "sofa_ikea_kivik_grey",
                subcategoryId: "living_room_seating_sofa",
                brand:         "IKEA",
                name:          "KIVIK",
                dimensions:    "90x57 in",
                modelFileName: "sofa_ikea_kivik_grey.glb")
            {
                Price        = "$649",
                IsRecommended = false,
                IsNew        = false,
                Tags         = new List<string> { "popular" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("sofa_ikea_kivik_beige",  "#D4C5A9", "Beige"),
                    new ProductColorVariant("sofa_ikea_kivik_grey",   "#8A8E91", "Grey"),
                    new ProductColorVariant("sofa_ikea_kivik_blue",   "#3A5A7C", "Blue"),
                    new ProductColorVariant("sofa_ikea_kivik_green",  "#4A6741", "Green"),
                }
            },
            new ProductModel(
                id:            "sofa_ikea_vimle_beige",
                subcategoryId: "living_room_seating_sofa",
                brand:         "IKEA",
                name:          "VIMLE",
                dimensions:    "98x65 in",
                modelFileName: "sofa_ikea_vimle_beige.glb")
            {
                Price        = "$899",
                IsRecommended = true,
                IsNew        = true,
                Tags         = new List<string> { "popular", "recommended", "new" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("sofa_ikea_vimle_beige", "#D4C5A9", "Beige"),
                    new ProductColorVariant("sofa_ikea_vimle_grey",  "#8A8E91", "Grey"),
                }
            },
            new ProductModel(
                id:            "sofa_muuto_connect_grey",
                subcategoryId: "living_room_seating_sofa",
                brand:         "Muuto",
                name:          "Connect Modular Sofa",
                dimensions:    "106x63 in",
                modelFileName: "sofa_muuto_connect_grey.glb")
            {
                Price        = "$2,499",
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("sofa_muuto_connect_grey",  "#8A8E91", "Grey"),
                    new ProductColorVariant("sofa_muuto_connect_green", "#4A6741", "Green"),
                }
            },
        },

        // ----------------------------------------------------------
        // LIVING ROOM > SEATING > ARMCHAIR
        // ----------------------------------------------------------
        ["living_room_seating_armchair"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "armchair_ikea_tullsta_beige",
                subcategoryId: "living_room_seating_armchair",
                brand:         "IKEA",
                name:          "TULLSTA",
                dimensions:    "31x28 in",
                modelFileName: "armchair_ikea_tullsta_beige.glb")
            {
                Price        = "$229",
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "popular", "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("armchair_ikea_tullsta_beige", "#D4C5A9", "Beige"),
                    new ProductColorVariant("armchair_ikea_tullsta_grey",  "#8A8E91", "Grey"),
                    new ProductColorVariant("armchair_ikea_tullsta_black", "#2C2C2C", "Black"),
                }
            },
            new ProductModel(
                id:            "armchair_ikea_tullsta_grey",
                subcategoryId: "living_room_seating_armchair",
                brand:         "IKEA",
                name:          "TULLSTA",
                dimensions:    "31x28 in",
                modelFileName: "armchair_ikea_tullsta_grey.glb")
            {
                Price        = "$229",
                IsRecommended = false,
                IsNew        = false,
                Tags         = new List<string> { "popular" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("armchair_ikea_tullsta_beige", "#D4C5A9", "Beige"),
                    new ProductColorVariant("armchair_ikea_tullsta_grey",  "#8A8E91", "Grey"),
                    new ProductColorVariant("armchair_ikea_tullsta_black", "#2C2C2C", "Black"),
                }
            },
            new ProductModel(
                id:            "armchair_seo_fritz_victorine",
                subcategoryId: "living_room_seating_armchair",
                brand:         "Seo Fritz Móveis e Ambientes",
                name:          "Armchair VICTORINE",
                dimensions:    "29x27 in",
                modelFileName: "armchair_seo_fritz_victorine.glb")
            {
                Price        = "$549",
                IsRecommended = true,
                IsNew        = true,
                Tags         = new List<string> { "recommended", "new" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("armchair_seo_fritz_victorine", "#8FAD8E", "Sage Green"),
                }
            },
            new ProductModel(
                id:            "armchair_ikea_poang_birch",
                subcategoryId: "living_room_seating_armchair",
                brand:         "IKEA",
                name:          "POÄNG",
                dimensions:    "26x32 in",
                modelFileName: "armchair_ikea_poang_birch.glb")
            {
                Price        = "$149",
                IsRecommended = false,
                IsNew        = false,
                Tags         = new List<string> { "popular" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("armchair_ikea_poang_birch",  "#C4A882", "Birch/Beige"),
                    new ProductColorVariant("armchair_ikea_poang_black",  "#2C2C2C", "Black/Dark Brown"),
                }
            },
        },

        // ----------------------------------------------------------
        // LIVING ROOM > SEATING > COFFEE TABLE
        // ----------------------------------------------------------
        ["living_room_seating_chaise"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "chaise_ikea_vallentuna_beige",
                subcategoryId: "living_room_seating_chaise",
                brand:         "IKEA",
                name:          "VALLENTUNA",
                dimensions:    "68x31 in",
                modelFileName: "chaise_ikea_vallentuna_beige.glb")
            {
                Price        = "$899",
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "popular", "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("chaise_ikea_vallentuna_beige", "#D4C5A9", "Beige"),
                    new ProductColorVariant("chaise_ikea_vallentuna_grey",  "#8A8E91", "Grey"),
                }
            },
        },

        // ----------------------------------------------------------
        // LIVING ROOM > TABLES AND CHAIRS > COFFEE TABLE
        // ----------------------------------------------------------
        ["living_room_tables_chairs_coffee_table"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "coffee_table_ikea_lack_black",
                subcategoryId: "living_room_tables_chairs_coffee_table",
                brand:         "IKEA",
                name:          "LACK",
                dimensions:    "46x30 in",
                modelFileName: "coffee_table_ikea_lack_black.glb")
            {
                Price        = "$49",
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "popular", "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("coffee_table_ikea_lack_black",  "#2C2C2C", "Black"),
                    new ProductColorVariant("coffee_table_ikea_lack_white",  "#F5F5F0", "White"),
                    new ProductColorVariant("coffee_table_ikea_lack_birch",  "#C4A882", "Birch"),
                }
            },
            new ProductModel(
                id:            "coffee_table_ikea_hemnes_white",
                subcategoryId: "living_room_tables_chairs_coffee_table",
                brand:         "IKEA",
                name:          "HEMNES",
                dimensions:    "46x24 in",
                modelFileName: "coffee_table_ikea_hemnes_white.glb")
            {
                Price        = "$149",
                IsRecommended = false,
                IsNew        = false,
                Tags         = new List<string> { "popular" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("coffee_table_ikea_hemnes_white",      "#F5F5F0", "White"),
                    new ProductColorVariant("coffee_table_ikea_hemnes_dark_brown", "#3B2A1A", "Dark Brown"),
                }
            },
        },

        // ----------------------------------------------------------
        // LIGHTING — shared placeholder products across rooms
        // ----------------------------------------------------------
        ["living_room_lighting_floor_lamp"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "floor_lamp_ikea_hektar_dark_grey",
                subcategoryId: "living_room_lighting_floor_lamp",
                brand:         "IKEA",
                name:          "HEKTAR",
                dimensions:    "15x70 in",
                modelFileName: "floor_lamp_ikea_hektar_dark_grey.glb")
            {
                Price        = "$99",
                IsRecommended = true,
                IsNew        = false,
                Tags         = new List<string> { "popular", "recommended" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("floor_lamp_ikea_hektar_dark_grey", "#4A4A4A", "Dark Grey"),
                    new ProductColorVariant("floor_lamp_ikea_hektar_beige",     "#D4C5A9", "Beige"),
                }
            },
        },

        ["living_room_lighting_pendant"] = new List<ProductModel>
        {
            new ProductModel(
                id:            "pendant_ikea_ranarp_off_white",
                subcategoryId: "living_room_lighting_pendant",
                brand:         "IKEA",
                name:          "RANARP",
                dimensions:    "10x12 in",
                modelFileName: "pendant_ikea_ranarp_off_white.glb")
            {
                Price        = "$49",
                IsRecommended = false,
                IsNew        = true,
                Tags         = new List<string> { "new" },
                ColorVariants = new List<ProductColorVariant>
                {
                    new ProductColorVariant("pendant_ikea_ranarp_off_white", "#F0EDE4", "Off White"),
                    new ProductColorVariant("pendant_ikea_ranarp_black",     "#2C2C2C", "Black"),
                }
            },
        },
    };
}