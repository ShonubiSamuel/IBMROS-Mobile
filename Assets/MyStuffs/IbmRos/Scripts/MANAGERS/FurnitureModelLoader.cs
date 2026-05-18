using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

public class FurnitureModelLoader : MonoBehaviour
{
    public static FurnitureModelLoader Instance { get; private set; }

    // Local cache folder inside the app's persistent data path
    private string CachePath => Path.Combine(Application.persistentDataPath, "FurnitureCache");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create cache folder if it does not exist
        if (!Directory.Exists(CachePath))
            Directory.CreateDirectory(CachePath);
    }
    

    // Main method to load a furniture model by filename
    // Returns the loaded GameObject or null if it failed
    public async Task<GameObject> LoadModel(string fileName)
    {
        try
        {
            string localPath = Path.Combine(CachePath, fileName);

            // Check if model is already cached locally
            if (File.Exists(localPath))
            {
                Debug.Log($"[FurnitureModelLoader] Loading {fileName} from cache.");
                return await LoadFromFile(localPath, fileName);
            }

            // Not cached, download from S3
            Debug.Log($"[FurnitureModelLoader] Downloading {fileName} from S3.");
            bool downloaded = await DownloadFromCloudFront(fileName, localPath);

            if (!downloaded)
            {
                Debug.LogError($"[FurnitureModelLoader] Failed to download {fileName}.");
                return null;
            }

            return await LoadFromFile(localPath, fileName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureModelLoader] LoadModel error: {e.Message}");
            return null;
        }
    }

    // Downloads a file from S3 and saves it to the local cache
    private async Task<bool> DownloadFromCloudFront(string fileName, string localPath)
    {
        try
        {
            string url = $"{AwsConfig.CloudFrontDomain}/{fileName}";
            Debug.Log($"[FurnitureModelLoader] Downloading {fileName} from CloudFront: {url}");

            using var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FurnitureModelLoader] CloudFront error: {request.error}");
                return false;
            }

            await File.WriteAllBytesAsync(localPath, request.downloadHandler.data);
            Debug.Log($"[FurnitureModelLoader] Downloaded {fileName} successfully.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureModelLoader] CloudFront download error: {e.Message}");
            return false;
        }
    }
    // Loads a GLB file from local path using GLTFast
    private async Task<GameObject> LoadFromFile(string localPath, string fileName)
    {
        try
        {
            var gltf = new GltfImport();
            bool success = await gltf.LoadFile(localPath);

            if (!success)
            {
                Debug.LogError($"[FurnitureModelLoader] GLTFast failed to load {fileName}.");
                return null;
            }

            // Create a parent GameObject to hold the model
            var modelRoot = new GameObject(fileName);
            success = await gltf.InstantiateMainSceneAsync(modelRoot.transform);

            if (!success)
            {
                Debug.LogError($"[FurnitureModelLoader] GLTFast failed to instantiate {fileName}.");
                Destroy(modelRoot);
                return null;
            }

            Debug.Log($"[FurnitureModelLoader] Successfully loaded {fileName}.");
            return modelRoot;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureModelLoader] LoadFromFile error: {e.Message}");
            return null;
        }
    }

    // Clears the entire local model cache
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(CachePath))
            {
                Directory.Delete(CachePath, true);
                Directory.CreateDirectory(CachePath);
                Debug.Log("[FurnitureModelLoader] Cache cleared.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FurnitureModelLoader] ClearCache error: {e.Message}");
        }
    }

    // Deletes a single model from cache
    public void DeleteFromCache(string fileName)
    {
        string localPath = Path.Combine(CachePath, fileName);

        if (File.Exists(localPath))
        {
            File.Delete(localPath);
            Debug.Log($"[FurnitureModelLoader] Deleted {fileName} from cache.");
        }
    }

    // Checks if a model is already cached
    public bool IsCached(string fileName)
    {
        return File.Exists(Path.Combine(CachePath, fileName));
    }
}