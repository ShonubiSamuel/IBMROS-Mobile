using UnityEngine;
using UnityEngine.UIElements;
using System;

public class BottomBarController : MonoBehaviour
{
    public event Action OnAddFurnitureClicked;
    public event Action OnOpen2DPlanClicked;

    private Button _addFurnitureButton;
    private Button _open2DPlanButton;
    private VisualElement _bottomBar;

    public void Initialize(VisualElement root)
    {
        _bottomBar = root.Q<VisualElement>("BottomBar");
        _addFurnitureButton = root.Q<Button>("AddFurnitureButton");
        _open2DPlanButton = root.Q<Button>("Open2DPlanButton");

        if (_addFurnitureButton != null)
            _addFurnitureButton.clicked += () => OnAddFurnitureClicked?.Invoke();

        if (_open2DPlanButton != null)
            _open2DPlanButton.clicked += () => OnOpen2DPlanClicked?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        if (_bottomBar != null)
            _bottomBar.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }
}