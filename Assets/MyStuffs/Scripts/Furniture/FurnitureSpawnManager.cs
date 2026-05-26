using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Bridge between the furniture UI panel and the placement pipeline.
/// 1. Looks up the GLB filename from FurnitureRegistry
/// 2. Loads the model via FurnitureService
/// 3. Hands the loaded model to FurniturePlacer for ghost placement
/// </summary>
public class FurnitureSpawnManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private FurnitureRegistry registry;
    [SerializeField] private FurniturePlacer   furniturePlacer;

    [Header("Fallback Settings")]
    [SerializeField] private bool allowPlaceholderCube = true;

    // ---------------------------------------------------------------
    // CALLED BY RoomUIManager when Add to Room is tapped
    // ---------------------------------------------------------------

    public async void SpawnItem(string itemKey)
    {
        if (registry == null)
        {
            Debug.LogError("[FurnitureSpawnManager] Registry not assigned.");
            return;
        }

        if (furniturePlacer == null)
        {
            Debug.LogError("[FurnitureSpawnManager] FurniturePlacer not assigned.");
            return;
        }

        var entry = registry.GetEntry(itemKey);

        // -------------------------------------------------------
        // Path A — GLB from CloudFront
        // -------------------------------------------------------
        if (entry != null && !string.IsNullOrEmpty(entry.glbFileName))
        {
            Debug.Log($"[FurnitureSpawnManager] Loading GLB for '{itemKey}'.");

            GameObject loadedModel = null;

            // Subscribe once to capture result
            void OnLoaded(string fileName, GameObject model)
            {
                if (fileName != entry.glbFileName) return;
                FurnitureService.OnModelLoaded -= OnLoaded;
                loadedModel = model;
            }

            void OnFailed(string fileName, string error)
            {
                if (fileName != entry.glbFileName) return;
                FurnitureService.OnModelLoadFailed -= OnFailed;
                Debug.LogError($"[FurnitureSpawnManager] Load failed for '{itemKey}': {error}");
            }

            FurnitureService.OnModelLoaded    += OnLoaded;
            FurnitureService.OnModelLoadFailed += OnFailed;

            await FurnitureService.Instance.LoadModel(entry.glbFileName);

            FurnitureService.OnModelLoaded    -= OnLoaded;
            FurnitureService.OnModelLoadFailed -= OnFailed;

            if (loadedModel != null)
            {
                InitializeFurnitureItem(loadedModel, itemKey, entry);
                furniturePlacer.BeginPlacementFromInstance(loadedModel); // not BeginPlacement
                return;
            }
        }

        // -------------------------------------------------------
        // Path B — Prefab fallback
        // -------------------------------------------------------
        if (entry != null && entry.prefabFallback != null)
        {
            Debug.Log($"[FurnitureSpawnManager] Using prefab fallback for '{itemKey}'.");
            var instance = Instantiate(entry.prefabFallback);
            instance.transform.localScale = entry.spawnScale;
            InitializeFurnitureItem(instance, itemKey, entry);
            furniturePlacer.BeginPlacement(instance);
            return;
        }

        // -------------------------------------------------------
        // Path C — Placeholder cube (dev only)
        // -------------------------------------------------------
        if (allowPlaceholderCube)
        {
            Debug.LogWarning($"[FurnitureSpawnManager] No model for '{itemKey}'. Using placeholder.");
            var placeholder = CreatePlaceholder(itemKey);
            InitializeFurnitureItem(placeholder, itemKey, entry);
            furniturePlacer.BeginPlacement(placeholder);
        }
        else
        {
            Debug.LogError($"[FurnitureSpawnManager] No model available for '{itemKey}'.");
        }
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private void InitializeFurnitureItem(
        GameObject go, string itemKey, FurnitureRegistry.CatalogEntry entry)
    {
        // Add or get FurnitureItem
        var item = go.GetComponent<FurnitureItem>()
                   ?? go.AddComponent<FurnitureItem>();

        item.Initialize(
            id:            itemKey,
            furnitureName: entry != null ? entry.itemKey           : itemKey,
            category:      entry != null ? entry.category          : "Uncategorized",
            realWorldSize: entry != null ? entry.realWorldSizeMeters : Vector3.one
        );

        // Set interactable layer on all children too
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
        {
            go.layer = layer;
            foreach (Transform child in go.GetComponentsInChildren<Transform>())
                child.gameObject.layer = layer;
        }

        // Remove any existing colliders first to avoid duplicates
        foreach (var col in go.GetComponentsInChildren<Collider>())
            Destroy(col);

        // Calculate combined bounds from all renderers
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (var r in renderers)
                combined.Encapsulate(r.bounds);

            // Add a single BoxCollider on the root fitted to the mesh bounds
            var box = go.AddComponent<BoxCollider>();

            // Convert world bounds to local space
            box.center = go.transform.InverseTransformPoint(combined.center);
            box.size   = combined.size;

            Debug.Log($"[FurnitureSpawnManager] Fitted collider: center={box.center}, size={box.size}");
        }
        else
        {
            // Fallback — unit box
            go.AddComponent<BoxCollider>();
            Debug.LogWarning($"[FurnitureSpawnManager] No renderers found on '{itemKey}', using unit collider.");
        }
    }

    private GameObject CreatePlaceholder(string itemKey)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(1f, 0.5f, 2f);
        go.name = $"[Placeholder] {itemKey}";

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.4f, 0.6f, 1f, 0.5f);
        go.GetComponent<Renderer>().material = mat;

        go.AddComponent<FurnitureItem>().Initialize(
            itemKey, itemKey, "Unknown", Vector3.one);

        return go;
    }
}