using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject catalog mapping UI item keys to GLB filenames.
/// Also tracks all placed FurnitureItem instances at runtime.
/// </summary>
[CreateAssetMenu(fileName = "FurnitureRegistry", 
                 menuName  = "IBMROS/Furniture Registry")]
public class FurnitureRegistry : ScriptableObject
{
    // ---------------------------------------------------------------
    // CATALOG ENTRY
    // ---------------------------------------------------------------

    [System.Serializable]
    public class CatalogEntry
    {
        [Tooltip("Must match exactly what FurniturePanelController sends e.g. 'IKEA BRIMNES'")]
        public string itemKey;

        [Tooltip("GLB filename stored on CloudFront e.g. 'ikea_brimnes_bed.glb'")]
        public string glbFileName;

        [Tooltip("Optional prefab fallback if GLB is not available")]
        public GameObject prefabFallback;

        public Vector3 spawnScale = Vector3.one;

        [Tooltip("Real world size in meters for FurnitureItem initialization")]
        public Vector3 realWorldSizeMeters = Vector3.one;

        public string category;
    }

    // ---------------------------------------------------------------
    // CATALOG DATA (set in Inspector)
    // ---------------------------------------------------------------

    [SerializeField] private List<CatalogEntry> entries = new();

    private Dictionary<string, CatalogEntry> _catalogLookup;

    private void OnEnable() => BuildLookup();

    private void BuildLookup()
    {
        _catalogLookup = new();
        foreach (var entry in entries)
            if (!string.IsNullOrEmpty(entry.itemKey))
                _catalogLookup[entry.itemKey] = entry;
    }

    public CatalogEntry GetEntry(string itemKey)
    {
        if (_catalogLookup == null) BuildLookup();
        return _catalogLookup.TryGetValue(itemKey, out var e) ? e : null;
    }

    // ---------------------------------------------------------------
    // RUNTIME TRACKING (placed items in scene)
    // ---------------------------------------------------------------

    private readonly List<FurnitureItem> _placedItems = new();

    public void Register(FurnitureItem item)
    {
        if (item != null && !_placedItems.Contains(item))
            _placedItems.Add(item);
    }

    public void Unregister(FurnitureItem item)
    {
        _placedItems.Remove(item);
    }

    public IReadOnlyList<FurnitureItem> PlacedItems => _placedItems;

    public void ClearPlacedItems()
    {
        _placedItems.Clear();
    }
}