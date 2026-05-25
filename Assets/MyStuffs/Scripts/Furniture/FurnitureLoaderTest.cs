using UnityEngine;

public class FurnitureLoaderTest : MonoBehaviour
{
    async void Start()
    {
        FurnitureModelLoader.Instance.ClearCache();
    
        Debug.Log("[FurnitureLoaderTest] Starting model load test.");
        GameObject model = await FurnitureModelLoader.Instance.LoadModel("chair_glb.glb");

        if (model != null)
        {
            Debug.Log("[FurnitureLoaderTest] Model loaded successfully.");
            model.transform.position = new Vector3(0, 0.414f, 0);
            model.transform.localScale = Vector3.one;
            model.AddComponent<BoxCollider>();
            model.AddComponent<FurnitureItem>();
            model.gameObject.layer = LayerMask.NameToLayer("Interactable");

        }
        else
        {
            Debug.LogError("[FurnitureLoaderTest] Model failed to load.");
        }
    }
}