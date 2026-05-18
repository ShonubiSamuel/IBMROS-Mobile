using UnityEngine;
using System;
using System.Collections.Generic;

public class FurnitureRegistry : MonoBehaviour
{
    public static FurnitureRegistry Instance { get; private set; }

    public event Action<FurnitureItem> OnItemRegistered;
    public event Action<FurnitureItem> OnItemUnregistered;

    private List<FurnitureItem> _placedItems = new List<FurnitureItem>();

    public IReadOnlyList<FurnitureItem> PlacedItems => _placedItems;
    public int Count => _placedItems.Count;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(FurnitureItem item)
    {
        if (item == null || _placedItems.Contains(item))
            return;

        _placedItems.Add(item);
        OnItemRegistered?.Invoke(item);
        Debug.Log($"[FurnitureRegistry] Registered {item.FurnitureName}. " +
                  $"Total: {_placedItems.Count}");
    }

    public void Unregister(FurnitureItem item)
    {
        if (item == null || !_placedItems.Contains(item))
            return;

        _placedItems.Remove(item);
        OnItemUnregistered?.Invoke(item);
        Debug.Log($"[FurnitureRegistry] Unregistered {item.FurnitureName}. " +
                  $"Total: {_placedItems.Count}");
    }

    public FurnitureItem GetById(string id)
    {
        return _placedItems.Find(item => item.FurnitureId == id);
    }

    public List<FurnitureItem> GetByCategory(string category)
    {
        return _placedItems.FindAll(item => item.Category == category);
    }

    public void Clear()
    {
        _placedItems.Clear();
    }
}