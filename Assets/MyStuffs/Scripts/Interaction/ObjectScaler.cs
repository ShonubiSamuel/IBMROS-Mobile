using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ObjectScaler : MonoBehaviour
{
    [Header("Dependencies")]
    public SelectionManager selectionManager;

    [Header("Prefabs")]
    public GameObject cornerHandlePrefab;
    public GameObject sideHandlePrefab;

    [Header("Settings")]
    public Color lineColor = new Color(0f, 0.5f, 1f, 1f); 
    public float lineWidth = 0.01f;
    public float padding = 0.05f; 

    // --- State ---
    private Transform _currentTarget; 
    private GameObject _currentRig;
    private LineRenderer _lineRenderer;
    private List<GameObject> _spawnedHandles = new List<GameObject>();
    
    private Vector3 _localCenter;
    private Vector3 _localSize;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
        _lineRenderer.positionCount = 0; 
        
        // Fix Line Z-Fighting
        _lineRenderer.sortingOrder = 0; 

        if (selectionManager != null)
        {
            selectionManager.onObjectSelected += OnObjectSelected;
            selectionManager.onObjectDeselected += OnObjectDeselected;
        }
    }

    void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected -= OnObjectSelected;
            selectionManager.onObjectDeselected -= OnObjectDeselected;
        }
    }

    // --- Selection Handlers ---

    private void OnObjectSelected(Transform target)
    {
        _currentTarget = target;
        // Ensure rig is hidden initially (Menu Mode is default)
        DestroyRig(); 
    }

    private void OnObjectDeselected()
    {
        _currentTarget = null;
        DestroyRig();
    }

    // --- PUBLIC METHODS ---

    public void SetScalingMode(bool active)
    {
        if (active) CreateRig();
        else DestroyRig();
    }

    // --- Visual Logic ---

    void CreateRig()
    {
        if (_currentTarget == null) return; 
        if (_currentRig != null) return; // Already exists

        CalculateLocalBounds(_currentTarget);

        _currentRig = new GameObject("ScalingRig_Planar");
        
        // Corner Handles
        SpawnHandle(new Vector3(1, 0, 1), HandleType.Corner, cornerHandlePrefab);
        SpawnHandle(new Vector3(-1, 0, 1), HandleType.Corner, cornerHandlePrefab);
        SpawnHandle(new Vector3(1, 0, -1), HandleType.Corner, cornerHandlePrefab);
        SpawnHandle(new Vector3(-1, 0, -1), HandleType.Corner, cornerHandlePrefab);

        // Side Handles
        SpawnHandle(new Vector3(1, 0, 0), HandleType.AxisX, sideHandlePrefab);
        SpawnHandle(new Vector3(-1, 0, 0), HandleType.AxisX, sideHandlePrefab);
        SpawnHandle(new Vector3(0, 0, 1), HandleType.AxisZ, sideHandlePrefab);
        SpawnHandle(new Vector3(0, 0, -1), HandleType.AxisZ, sideHandlePrefab);

        UpdateRigVisuals();
    }

    void DestroyRig()
    {
        if (_currentRig != null)
        {
            Destroy(_currentRig);
            _currentRig = null; // <--- CRITICAL FIX: Force it null immediately
        }
        
        _spawnedHandles.Clear();
        
        if (_lineRenderer != null) 
            _lineRenderer.positionCount = 0;
    }

    void CalculateLocalBounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            _localCenter = Vector3.zero;
            _localSize = Vector3.one;
            return;
        }

        // Combine all renderer bounds into one world space bounds
        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        // Convert world bounds back to local space of the target transform
        _localCenter = t.InverseTransformPoint(worldBounds.center);
        _localSize = new Vector3(
            worldBounds.size.x / t.lossyScale.x,
            worldBounds.size.y / t.lossyScale.y,
            worldBounds.size.z / t.lossyScale.z
        );
    }

    void SpawnHandle(Vector3 dir, HandleType type, GameObject prefab)
    {
        if (prefab == null) return;

        // Safety: If DestroyRig was called this frame, _currentRig is null now.
        if (_currentRig == null) return;

        GameObject handle = Instantiate(prefab, _currentRig.transform);
        handle.tag = "ScaleHandle"; 

        ScaleHandle handleScript = handle.GetComponent<ScaleHandle>();
        if (handleScript == null) handleScript = handle.AddComponent<ScaleHandle>();

        handleScript.type = type;
        handleScript.direction = dir;

        // Fix Sprite Z-Fighting
        SpriteRenderer sr = handle.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 5; 
        if (handle.transform.childCount > 0)
        {
            SpriteRenderer srChild = handle.transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (srChild != null) srChild.sortingOrder = 10; 
        }
        
        _spawnedHandles.Add(handle);
    }

    void Update()
    {
        // Only update if rig exists (is not null)
        if (_currentTarget != null && _currentRig != null)
        {
            UpdateRigVisuals();
        }
    }

    void UpdateRigVisuals()
    {
        if (_currentTarget == null || _currentRig == null)
            return;

        Vector3 worldCenter = _currentTarget.TransformPoint(_localCenter);
        Vector3 scaledSize = Vector3.Scale(_localSize, _currentTarget.lossyScale);
        Vector3 sizeWithPadding = scaledSize + (Vector3.one * padding);
        Vector3 halfSize = sizeWithPadding * 0.5f;

        Quaternion yRotation = Quaternion.Euler(0, _currentTarget.eulerAngles.y, 0);

        // Add slight Y lift so handles render above the object
        Vector3 topPlaneCenter = worldCenter + Vector3.up * (halfSize.y + 0.02f);

        // Scale handles by camera distance so they stay clickable
        Camera cam = Camera.main;
        float distance = cam != null
            ? Vector3.Distance(cam.transform.position, worldCenter)
            : 1f;
        float handleScale = Mathf.Clamp(distance * 0.05f, 0.05f, 0.3f);

        foreach (GameObject handleObj in _spawnedHandles)
        {
            if (handleObj == null)
                continue;

            ScaleHandle handle = handleObj.GetComponent<ScaleHandle>();
            Vector3 dir = handle.direction;

            Vector3 localOffset = new Vector3(
                dir.x * halfSize.x,
                0,
                dir.z * halfSize.z
            );

            handleObj.transform.position = topPlaneCenter + (yRotation * localOffset);
            handleObj.transform.localScale = Vector3.one * handleScale;

            SmartBillboard billboard = handleObj.GetComponent<SmartBillboard>();
            if (billboard != null)
            {
                if (handle.type == HandleType.Corner)
                    billboard.targetAlignDirection = Vector3.zero;
                else if (handle.type == HandleType.AxisX)
                    billboard.targetAlignDirection = yRotation * Vector3.forward;
                else if (handle.type == HandleType.AxisZ)
                    billboard.targetAlignDirection = yRotation * Vector3.right;
            }
        }

        DrawRectangle(topPlaneCenter, halfSize, yRotation);
    }

void DrawRectangle(Vector3 center, Vector3 halfSize, Quaternion rotation)
{
    if (_lineRenderer == null)
        return;

    Vector3[] corners = new Vector3[5];
    corners[0] = center + rotation * new Vector3(halfSize.x, 0, halfSize.z);
    corners[1] = center + rotation * new Vector3(-halfSize.x, 0, halfSize.z);
    corners[2] = center + rotation * new Vector3(-halfSize.x, 0, -halfSize.z);
    corners[3] = center + rotation * new Vector3(halfSize.x, 0, -halfSize.z);
    corners[4] = corners[0];

    _lineRenderer.positionCount = 5;
    _lineRenderer.SetPositions(corners);
}
    // --- VISUAL HIGHLIGHTING METHODS ---
    public void OnScalingStart(ScaleHandle activeHandle)
    {
        if (activeHandle == null) return;
        foreach (GameObject handleObj in _spawnedHandles)
        {
            if (handleObj == null) continue;
            ScaleHandle current = handleObj.GetComponent<ScaleHandle>();
            bool shouldShow = false;
            bool highlight = false;

            if (current == activeHandle) { shouldShow = true; highlight = true; }
            else if (current.type == activeHandle.type)
            {
                if (Vector3.Dot(current.direction, activeHandle.direction) < -0.9f)
                {
                    shouldShow = true; 
                    highlight = true; 
                }
            }
            handleObj.SetActive(shouldShow);
            SetHandleColor(handleObj, highlight ? Color.blue : Color.white);
        }
    }

    public void OnScalingEnd()
    {
        foreach (GameObject handleObj in _spawnedHandles)
        {
            if (handleObj == null) continue;
            handleObj.SetActive(true); 
            SetHandleColor(handleObj, Color.white); 
        }
    }

    private void SetHandleColor(GameObject handleObj, Color color)
    {
        SpriteRenderer sr = handleObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
        if (handleObj.transform.childCount > 0)
        {
            SpriteRenderer childSr = handleObj.transform.GetChild(0).GetComponent<SpriteRenderer>();
            if (childSr != null) childSr.color = color;
        }
    }
}