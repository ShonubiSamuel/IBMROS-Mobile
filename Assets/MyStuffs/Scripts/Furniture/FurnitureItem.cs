using UnityEngine;

public class FurnitureItem : MonoBehaviour
{
    [Header("Furniture Data")]
    [SerializeField] private string _furnitureId;
    [SerializeField] private string _furnitureName;
    [SerializeField] private string _category;
    [SerializeField] private Vector3 _realWorldSizeMeters;

    // Runtime state
    private bool _isPlaced = false;
    private bool _isSelected = false;

    // Read only properties
    public string FurnitureId => _furnitureId;
    public string FurnitureName => _furnitureName;
    public string Category => _category;
    public Vector3 RealWorldSizeMeters => _realWorldSizeMeters;
    public bool IsPlaced => _isPlaced;
    public bool IsSelected => _isSelected;

    public void Initialize(string id, string furnitureName, string category, Vector3 realWorldSize)
    {
        _furnitureId = id;
        _furnitureName = furnitureName;
        _category = category;
        _realWorldSizeMeters = realWorldSize;
    }

    public void SetPlaced(bool placed)
    {
        _isPlaced = placed;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
    }

    // Returns real world dimensions in meters based on current scale
    // compared to the original real world size
    public Vector3 GetCurrentDimensionsMeters()
    {
        if (_realWorldSizeMeters == Vector3.zero)
            return transform.lossyScale;

        return new Vector3(
            transform.lossyScale.x * _realWorldSizeMeters.x,
            transform.lossyScale.y * _realWorldSizeMeters.y,
            transform.lossyScale.z * _realWorldSizeMeters.z
        );
    }

    // Returns width and depth as a formatted string e.g. "0.59 x 0.65"
    public string GetFootprintLabel()
    {
        Vector3 dims = GetCurrentDimensionsMeters();
        return $"{dims.x:F2} x {dims.z:F2}";
    }

    // Returns height as a formatted string e.g. "1.08"
    public string GetHeightLabel()
    {
        Vector3 dims = GetCurrentDimensionsMeters();
        return $"{dims.y:F2}";
    }
}