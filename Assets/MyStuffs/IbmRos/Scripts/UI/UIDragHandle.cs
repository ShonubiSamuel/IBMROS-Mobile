using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class UIDragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    public event Action<PointerEventData> onDragStart;
    public event Action<PointerEventData> onDrag;
    public event Action<PointerEventData> onDragEnd;

    public void OnPointerDown(PointerEventData eventData)
    {
        onDragStart?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        onDrag?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onDragEnd?.Invoke(eventData);
    }
}