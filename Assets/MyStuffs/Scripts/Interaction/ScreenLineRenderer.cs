using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScreenLineRenderer : MonoBehaviour
{
    [SerializeField] private Color lineColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private float lineWidth = 3f;

    private List<GameObject> _lines = new List<GameObject>();
    private RectTransform _container;

    void Awake()
    {
        _container = GetComponent<RectTransform>();
    }

    public void DrawLines(Vector2[] points, bool closed = true)
    {
        ClearLines();

        int count = closed ? points.Length : points.Length - 1;

        for (int i = 0; i < count; i++)
        {
            Vector2 from = points[i];
            Vector2 to = points[(i + 1) % points.Length];
            CreateLine(from, to);
        }
    }

    public void ClearLines()
    {
        foreach (var line in _lines)
        {
            if (line != null)
                Destroy(line);
        }
        _lines.Clear();
    }

    private void CreateLine(Vector2 from, Vector2 to)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(_container, false);

        RectTransform rt = lineObj.AddComponent<RectTransform>();
        Image img = lineObj.AddComponent<Image>();
        img.color = lineColor;
        img.raycastTarget = false;

        Vector2 dir = to - from;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(length, lineWidth);
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        _lines.Add(lineObj);
    }
}