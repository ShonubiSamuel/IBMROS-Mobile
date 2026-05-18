using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ScaleHandleUI : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public HandleType type;
    public Vector3 direction;

    public event Action<ScaleHandleUI, Vector2> OnHandleDown;
    public event Action<ScaleHandleUI, Vector2> OnHandleDrag;
    public event Action<ScaleHandleUI, Vector2> OnHandleUp;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnHandleDown?.Invoke(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnHandleDrag?.Invoke(this, eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnHandleUp?.Invoke(this, eventData.position);
    }

    public void SetHighlighted(bool highlighted)
    {
        var image = GetComponent<Image>();
        if (image != null)
            image.color = highlighted ? Color.blue : Color.white;
    }
}