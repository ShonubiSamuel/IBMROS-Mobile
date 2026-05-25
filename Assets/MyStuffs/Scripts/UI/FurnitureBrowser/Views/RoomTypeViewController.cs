using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomTypeViewController
{
    public event Action<RoomTypeModel> OnRoomTypeSelected;

    private VisualElement _root;
    private ScrollView    _scrollView;
    private VisualElement _grid;

    private const string CardClass  = "browser-grid-card";
    private const string IconClass  = "browser-grid-card__icon";
    private const string LabelClass = "browser-grid-card__label";

    public void Initialize(VisualElement root)
    {
        _root       = root;
        _scrollView = root.Q<ScrollView>("RoomTypeScrollView");
        _grid       = root.Q<VisualElement>("RoomTypeGrid");

        if (_grid == null)
        {
            Debug.LogError("[RoomTypeViewController] 'RoomTypeGrid' not found. " +
                           "Your FurnitureBrowserPanel.uxml still has <ui:Instance> tags — " +
                           "replace it with the fully inlined version.\n\n" +
                           "Elements found in the cloned tree:\n" + DumpTree(root));
        }
    }

    public void Populate()
    {
        if (_grid == null)
        {
            Debug.LogError("[RoomTypeViewController] Cannot populate — grid is null. Check previous error.");
            return;
        }

        _grid.Clear();

        var catalog = FurniturePlaceholderData.GetCatalog();
        Debug.Log($"[RoomTypeViewController] Populating grid with {catalog.Count} room types");

        foreach (var roomType in catalog)
        {
            var card = BuildCard(roomType);
            _grid.Add(card);
            Debug.Log($"[RoomTypeViewController] Added card: {roomType.DisplayName}");
        }

        Debug.Log($"[RoomTypeViewController] Grid now has {_grid.childCount} children");
    }

    public void Clear()
    {
        _grid?.Clear();
    }

    private VisualElement BuildCard(RoomTypeModel roomType)
    {
        var card = new Button(() => OnRoomTypeSelected?.Invoke(roomType));
        card.AddToClassList(CardClass);

        var icon = new Label(roomType.IconEmoji);
        icon.AddToClassList(IconClass);

        var label = new Label(roomType.DisplayName);
        label.AddToClassList(LabelClass);

        card.Add(icon);
        card.Add(label);

        return card;
    }

    private static string DumpTree(VisualElement root, int depth = 0)
    {
        var sb = new StringBuilder();
        DumpElement(root, sb, depth);
        return sb.ToString();
    }

    private static void DumpElement(VisualElement el, StringBuilder sb, int depth)
    {
        sb.AppendLine($"{new string(' ', depth * 2)}{el.GetType().Name} name='{el.name}'");
        foreach (var child in el.Children())
            DumpElement(child, sb, depth + 1);
    }
}