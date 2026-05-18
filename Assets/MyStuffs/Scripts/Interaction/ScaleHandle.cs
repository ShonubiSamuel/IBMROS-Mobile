using UnityEngine;

public class ScaleHandle : MonoBehaviour
{
    public HandleType type;
    
    // We store the "direction" this handle pushes towards (e.g., +1, +1, +1 for top right)
    [HideInInspector]
    public Vector3 direction; 
}