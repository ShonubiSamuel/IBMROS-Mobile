using UnityEngine;
using System;

public class FurniturePlacer : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private FurnitureRegistry furnitureRegistry;

    [Header("Config")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float placementHeightOffset = 0f;

    public event Action<FurnitureItem> OnFurniturePlaced;
    public event Action OnPlacementCancelled;

    private GameObject _previewObject;
    private FurnitureItem _previewItem;
    private bool _isPlacing = false;
    private Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnPointerMove += HandlePointerMove;
            inputManager.OnPointerClick += HandlePointerClick;
        }
    }

    void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnPointerMove -= HandlePointerMove;
            inputManager.OnPointerClick -= HandlePointerClick;
        }
    }

    // Call this from BottomBarController when Add Furniture is tapped
    // Pass in the prefab the player selected from the catalog
    public void BeginPlacement(GameObject furniturePrefab)
    {
        if (furniturePrefab == null)
            return;

        if (_isPlacing)
            CancelPlacement();

        // Spawn preview object
        _previewObject = Instantiate(furniturePrefab);
        _previewItem = _previewObject.GetComponent<FurnitureItem>();

        if (_previewItem == null)
            _previewItem = _previewObject.AddComponent<FurnitureItem>();

        // Make preview semi-transparent
        SetPreviewMaterial(true);

        _isPlacing = true;

        // Deselect any currently selected object
        selectionManager?.DeselectObject();

        Debug.Log($"[FurniturePlacer] Placement started for {furniturePrefab.name}");
    }

    public void CancelPlacement()
    {
        if (!_isPlacing)
            return;

        if (_previewObject != null)
            Destroy(_previewObject);

        _previewObject = null;
        _previewItem = null;
        _isPlacing = false;

        OnPlacementCancelled?.Invoke();
        Debug.Log("[FurniturePlacer] Placement cancelled.");
    }

    public bool IsPlacing => _isPlacing;

    private void HandlePointerMove(Vector2 screenPosition)
    {
        if (!_isPlacing || _previewObject == null)
            return;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
        {
            _previewObject.transform.position = hit.point;
            ApplyFloorOffset(hit.point);
        }
    }

    private void HandlePointerClick(Vector2 screenPosition)
    {
        if (!_isPlacing || _previewObject == null)
            return;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
            return;

        // Confirm placement at tap position
        _previewObject.transform.position = hit.point;
        ApplyFloorOffset(hit.point);

        // Restore normal material
        SetPreviewMaterial(false);

        _previewItem.SetPlaced(true);

        // Register with furniture registry
        furnitureRegistry?.Register(_previewItem);

        // Record undo action
        UndoRedoManager.Instance?.Record(
            new PlaceAction(_previewObject, selectionManager)
        );

        FurnitureItem placedItem = _previewItem;

        _previewObject = null;
        _previewItem = null;
        _isPlacing = false;

        // Auto select the placed object
        selectionManager?.SelectObject(placedItem.transform);

        OnFurniturePlaced?.Invoke(placedItem);
    }
    
    private void ApplyFloorOffset(Vector3 hitPoint)
    {
        if (_previewObject == null)
            return;

        Vector3 targetPosition = hitPoint;
        targetPosition.y += placementHeightOffset;

        Renderer renderer = _previewObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            float pivotToBase = _previewObject.transform.position.y
                                - renderer.bounds.min.y;
            targetPosition.y += pivotToBase;
        }

        _previewObject.transform.position = targetPosition;
    }

    private void SetPreviewMaterial(bool isPreview)
    {
        if (_previewObject == null)
            return;

        Renderer[] renderers = _previewObject.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = isPreview ? 0.5f : 1f;
                mat.color = color;

                if (isPreview)
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                }
                else
                {
                    mat.SetFloat("_Mode", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = -1;
                }
            }
        }
    }
}