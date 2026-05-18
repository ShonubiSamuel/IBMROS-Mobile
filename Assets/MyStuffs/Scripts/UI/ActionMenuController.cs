using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ActionMenuController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private ObjectManipulator objectManipulator;
    [SerializeField] private ScaleRigUI scaleRigUI;

    [Header("Panel Above")]
    [SerializeField] private RectTransform panelAbove;
    [SerializeField] private Button colorButton;
    [SerializeField] private Button scaleButton;
    [SerializeField] private Button duplicateButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button moreOptionsButton;

    [Header("Panel Below")]
    [SerializeField] private RectTransform panelBelow;
    [SerializeField] private UIDragHandle rotationHandle;
    private Vector2 _panelBelowInitialAnchoredPos;
    private bool _panelBelowInitialCached = false;

    [Header("Settings")]
    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float minVerticalSpacing = 20f;

    public event Action OnScaleModeRequested;
    public event Action OnColorModeRequested;

    private Camera _mainCamera;
    private Transform _targetObject;
    private Renderer _targetRenderer;
    private bool _isVisible = false;
    private bool _isScalingMode = false;
    private Vector3[] _corners = new Vector3[4];
    private bool _isRotatingPanel = false;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected += HandleObjectSelected;
            selectionManager.onObjectDeselected += HandleObjectDeselected;
            selectionManager.onObjectReSelected += HandleObjectSelected;
        }

        if (objectManipulator != null)
        {
            objectManipulator.OnManipulationStart += HandleManipulationStart;
            objectManipulator.OnManipulationEnd += HandleManipulationEnd;
            objectManipulator.OnCameraRotationStart += HandleCameraRotationStart;
            objectManipulator.OnCameraRotationEnd += HandleCameraRotationEnd;
        }

        if (rotationHandle != null)
        {
            rotationHandle.onDragStart += HandleRotationDragStart;
            rotationHandle.onDrag += HandleRotationDrag;
            rotationHandle.onDragEnd += HandleRotationDragEnd;
        }

        WireButtons();
        HidePanels();
    }

    void OnDisable()
    {
        if (selectionManager != null)
        {
            selectionManager.onObjectSelected -= HandleObjectSelected;
            selectionManager.onObjectDeselected -= HandleObjectDeselected;
            selectionManager.onObjectReSelected -= HandleObjectSelected;
        }

        if (objectManipulator != null)
        {
            objectManipulator.OnManipulationStart -= HandleManipulationStart;
            objectManipulator.OnManipulationEnd -= HandleManipulationEnd;
            objectManipulator.OnCameraRotationStart -= HandleCameraRotationStart;
            objectManipulator.OnCameraRotationEnd -= HandleCameraRotationEnd;
        }

        if (rotationHandle != null)
        {
            rotationHandle.onDragStart -= HandleRotationDragStart;
            rotationHandle.onDrag -= HandleRotationDrag;
            rotationHandle.onDragEnd -= HandleRotationDragEnd;
        }

        UnwireButtons();
    }

    void Update()
    {
        if (!_isVisible || _targetRenderer == null)
            return;

        // Do not update positions while user is dragging rotation handle
        if (_isRotatingPanel)
            return;

        UpdatePanelPositions();
    }

    private void WireButtons()
    {
        if (colorButton != null)
            colorButton.onClick.AddListener(OnColorClicked);

        if (scaleButton != null)
            scaleButton.onClick.AddListener(OnScaleClicked);

        if (duplicateButton != null)
            duplicateButton.onClick.AddListener(OnDuplicateClicked);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);

        if (moveButton != null)
            moveButton.onClick.AddListener(OnMoveClicked);

        if (moreOptionsButton != null)
            moreOptionsButton.onClick.AddListener(OnMoreOptionsClicked);
    }

    private void UnwireButtons()
    {
        if (colorButton != null)
            colorButton.onClick.RemoveListener(OnColorClicked);

        if (scaleButton != null)
            scaleButton.onClick.RemoveListener(OnScaleClicked);

        if (duplicateButton != null)
            duplicateButton.onClick.RemoveListener(OnDuplicateClicked);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(OnDeleteClicked);

        if (moveButton != null)
            moveButton.onClick.RemoveListener(OnMoveClicked);

        if (moreOptionsButton != null)
            moreOptionsButton.onClick.RemoveListener(OnMoreOptionsClicked);
    }

    // BUTTON HANDLERS

    private void OnColorClicked()
    {
        OnColorModeRequested?.Invoke();
    }

    private void OnScaleClicked()
    {
        _isScalingMode = true;
        HidePanels();

        scaleRigUI?.ShowRig();
        OnScaleModeRequested?.Invoke();
    }

    private void OnDuplicateClicked()
    {
        objectManipulator?.DuplicateSelectedObject();
    }

    private void OnDeleteClicked()
    {
        objectManipulator?.DeleteSelectedObject();
    }

    private void OnMoveClicked()
    {
        Debug.Log("[ActionMenuController] Move tapped. Coming soon.");
    }

    private void OnMoreOptionsClicked()
    {
        Debug.Log("[ActionMenuController] More options tapped. Coming soon.");
    }

    // SELECTION HANDLERS

    private void HandleObjectSelected(Transform target)
    {
        _isScalingMode = false;
        scaleRigUI?.HideRig();

        _targetObject = target;
        _targetRenderer = target.GetComponentInChildren<Renderer>();
        ShowPanels();
    }

    private void HandleObjectDeselected()
    {
        _isScalingMode = false;
        scaleRigUI?.HideRig();

        _targetObject = null;
        _targetRenderer = null;
        HidePanels();
    }

    private void HandleManipulationStart()
    {
        HidePanels();
    }

    private void HandleManipulationEnd()
    {
        if (_targetObject != null && !_isScalingMode)
            ShowPanels();
    }

    private void HandleCameraRotationStart()
    {
        HidePanels();
    }

    private void HandleCameraRotationEnd()
    {
        // User must tap object again to reopen
    }

    private void HandleRotationDragStart(UnityEngine.EventSystems.PointerEventData data)
    {
        _isRotatingPanel = true;

        if (panelAbove != null)
            panelAbove.gameObject.SetActive(false);
    }

    private void HandleRotationDrag(UnityEngine.EventSystems.PointerEventData data)
    {
        if (panelBelow == null)
            return;

        Canvas canvas = panelBelow.GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            data.position,
            canvas.worldCamera,
            out localPoint
        );

        panelBelow.localPosition = localPoint;
    }

    private void HandleRotationDragEnd(UnityEngine.EventSystems.PointerEventData data)
    {
        _isRotatingPanel = false;

        if (_targetObject != null && !_isScalingMode)
            ShowPanels();
    }
    // PANEL VISIBILITY

    public void ShowPanels()
    {
        if (panelAbove != null)
            panelAbove.gameObject.SetActive(true);

        if (panelBelow != null)
            panelBelow.gameObject.SetActive(true);

        _isVisible = true;
        UpdatePanelPositions();
    }

    public void HidePanels()
    {
        if (panelAbove != null)
            panelAbove.gameObject.SetActive(false);

        if (panelBelow != null)
            panelBelow.gameObject.SetActive(false);

        _isVisible = false;
    }

    // PANEL POSITIONING

    private void UpdatePanelPositions()
    {
        if (_targetRenderer == null || _mainCamera == null)
            return;

        bool isOnScreen = ScreenSpaceHelper.TryGetScreenSpaceBounds(
            _targetRenderer.bounds,
            _mainCamera,
            out ScreenSpaceHelper.ObjectScreenBounds screenBounds
        );

        if (!isOnScreen)
        {
            HidePanels();
            return;
        }

        if (panelAbove != null)
        {
            float scaleFactor = panelAbove.lossyScale.y;
            float halfWidth = panelAbove.rect.width * scaleFactor * 0.5f;
            float halfHeight = panelAbove.rect.height * scaleFactor * 0.5f;

            Rect safeArea = GetConstrainedSafeArea();

            float targetX = Mathf.Clamp(
                screenBounds.CenterX,
                safeArea.xMin + halfWidth,
                safeArea.xMax - halfWidth
            );

            float targetY = screenBounds.TopY + verticalPadding * scaleFactor;
            targetY = Mathf.Clamp(
                targetY,
                safeArea.yMin + halfHeight,
                safeArea.yMax - halfHeight
            );

            panelAbove.position = new Vector2(targetX, targetY);
        }

        if (panelBelow != null)
        {
            float scaleFactor = panelBelow.lossyScale.y;
            float halfWidth = panelBelow.rect.width * scaleFactor * 0.5f;
            float halfHeight = panelBelow.rect.height * scaleFactor * 0.5f;

            Rect safeArea = GetConstrainedSafeArea();

            float edgeMargin = 24f * (Screen.width / 1080f);

            float targetX = Mathf.Clamp(
                screenBounds.CenterX,
                safeArea.xMin + halfWidth + edgeMargin,
                safeArea.xMax - halfWidth - edgeMargin
            );

            float targetY = screenBounds.BottomY - verticalPadding * scaleFactor;
            targetY = Mathf.Clamp(
                targetY,
                safeArea.yMin + halfHeight,
                safeArea.yMax - halfHeight
            );

            panelBelow.position = new Vector2(targetX, targetY);
        }
    }

    public float topBarLimit = 72f;
    public float bottomBarLimit = 120f;
    private Rect GetConstrainedSafeArea()
    {
        Rect safeArea = Screen.safeArea;

        float topBarHeight = topBarLimit * (Screen.height / 1920f);
        float bottomBarHeight = bottomBarLimit * (Screen.height / 1920f);

        // Add horizontal padding so panels never touch screen edges
        float horizontalPadding = 16f * (Screen.width / 1080f);

        float yMin = safeArea.yMin + bottomBarHeight;
        float yMax = safeArea.yMax - topBarHeight;

        return new Rect(
            safeArea.x + horizontalPadding,
            yMin,
            safeArea.width - (horizontalPadding * 2f),
            Mathf.Max(0, yMax - yMin)
        );
    }
}