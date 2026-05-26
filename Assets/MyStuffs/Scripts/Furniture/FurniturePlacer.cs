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
    
    private Vector3 _smoothedPosition;
    private bool    _hasInitialPosition = false;
    public float POSITION_SMOOTH_SPEED = 2f;
    
    private float _pivotToBottomOffset = 0f;

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
    

    public void CancelPlacement()
    {
        if (!_isPlacing) return;

        if (_previewObject != null)
            Destroy(_previewObject);

        _previewObject       = null;
        _previewItem         = null;
        _isPlacing           = false;
        _hasInitialPosition  = false;
        _pivotToBottomOffset = 0f;

        OnPlacementCancelled?.Invoke();
        Debug.Log("[FurniturePlacer] Placement cancelled.");
    }

    public bool IsPlacing => _isPlacing;

    private void HandlePointerMove(Vector2 screenPosition)
    {
        if (!_isPlacing || _previewObject == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, floorLayer))
            return;

        // Calculate the correctly lifted target position
        Vector3 targetPosition = GetLiftedPosition(hit.point);

        // Initialise smoothed position on first move
        if (!_hasInitialPosition)
        {
            _smoothedPosition    = targetPosition;
            _hasInitialPosition  = true;
        }

        // Smooth toward target — kills the jitter
        _smoothedPosition = Vector3.Lerp(
            _smoothedPosition,
            targetPosition,
            Time.deltaTime * POSITION_SMOOTH_SPEED
        );

        _previewObject.transform.position = _smoothedPosition;
    }
    
    private Vector3 GetLiftedPosition(Vector3 hitPoint)
    {
        return new Vector3(
            hitPoint.x,
            hitPoint.y + _pivotToBottomOffset + placementHeightOffset,
            hitPoint.z
        );
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
        if (_previewObject == null) return;

        _previewObject.transform.position = hitPoint;

        Renderer[] renderers = _previewObject.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            _pivotToBottomOffset = 0f;
            _previewObject.transform.position = hitPoint + Vector3.up * placementHeightOffset;
            return;
        }

        Bounds combined = renderers[0].bounds;
        foreach (var r in renderers)
            combined.Encapsulate(r.bounds);

        // Cache this offset — reused every move frame
        _pivotToBottomOffset = _previewObject.transform.position.y - combined.min.y;

        Vector3 finalPosition  = hitPoint;
        finalPosition.y       += _pivotToBottomOffset + placementHeightOffset;
        _previewObject.transform.position = finalPosition;
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
    
    // Use this when the GameObject is already instantiated in the scene (e.g. from GLB loader)
    public void BeginPlacement(GameObject furniturePrefab)
    {
        if (furniturePrefab == null) return;
        if (_isPlacing) CancelPlacement();

        _previewObject = Instantiate(furniturePrefab);
        _previewItem   = _previewObject.GetComponent<FurnitureItem>()
                         ?? _previewObject.AddComponent<FurnitureItem>();

        SetPreviewMaterial(true);
        _isPlacing = true;

        selectionManager?.DeselectObject();

        // Snap to floor immediately so it never appears sunken
        SnapToInitialPosition();

        Debug.Log($"[FurniturePlacer] Placement started for {furniturePrefab.name}");
    }

    public void BeginPlacementFromInstance(GameObject sceneInstance)
    {
        if (sceneInstance == null) return;
        if (_isPlacing) CancelPlacement();

        _previewObject = sceneInstance;
        _previewItem   = _previewObject.GetComponent<FurnitureItem>()
                         ?? _previewObject.AddComponent<FurnitureItem>();

        SetPreviewMaterial(true);
        _isPlacing = true;

        selectionManager?.DeselectObject();

        // Snap to floor immediately so it never appears sunken
        SnapToInitialPosition();

        Debug.Log($"[FurniturePlacer] Placement started (from instance) for {sceneInstance.name}");
    }
    
    private void SnapToInitialPosition()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null) return;

        // Cast a ray from camera centre forward to find the floor
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        Vector3 spawnPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 20f, floorLayer))
        {
            spawnPoint = hit.point;
        }
        else
        {
            // Fallback — fixed distance in front of camera on XZ plane
            Vector3 forward = _mainCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            spawnPoint   = _mainCamera.transform.position + forward * 3f;
            spawnPoint.y = 0f;
        }

        // Use ApplyFloorOffset to lift it correctly off the floor
        ApplyFloorOffset(spawnPoint);
    }
}