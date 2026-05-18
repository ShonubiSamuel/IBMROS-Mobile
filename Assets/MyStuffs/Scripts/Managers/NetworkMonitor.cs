using System;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkMonitor : MonoBehaviour
{
    public static NetworkMonitor Instance { get; private set; }

    private const int CheckIntervalSeconds = 5;

    public static event Action OnConnectionLost;
    public static event Action OnConnectionRestored;

    public bool IsConnected { get; private set; } = true;
    private bool _isMonitoring = false;
    private bool _previousConnectionState = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartMonitoring();
    }

    void OnDestroy()
    {
        _isMonitoring = false;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        Debug.Log("[NetworkMonitor] Network monitoring started.");
        _ = MonitorLoop();
    }

    public void StopMonitoring()
    {
        _isMonitoring = false;
        Debug.Log("[NetworkMonitor] Network monitoring stopped.");
    }

    private async Task MonitorLoop()
    {
        while (_isMonitoring)
        {
            CheckNetworkState();
            await Task.Delay(CheckIntervalSeconds * 1000);
        }
    }

    private void CheckNetworkState()
    {
        bool currentlyConnected = Application.internetReachability
            != NetworkReachability.NotReachable;

        if (currentlyConnected == _previousConnectionState)
            return;

        IsConnected = currentlyConnected;
        _previousConnectionState = currentlyConnected;

        if (!currentlyConnected)
        {
            Debug.LogWarning("[NetworkMonitor] Connection lost.");

            // Route through UIManager so banner shows on all screens
            UIManager.Instance?.ShowNoConnectionBanner();

            // Notify any controller that subscribes
            OnConnectionLost?.Invoke();
        }
        else
        {
            Debug.Log("[NetworkMonitor] Connection restored.");

            // Hide banner globally
            UIManager.Instance?.HideNoConnectionBanner();

            // Notify any controller that subscribes
            OnConnectionRestored?.Invoke();
        }
    }

    public string GetConnectionType()
    {
        switch (Application.internetReachability)
        {
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                return "WiFi";
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                return "Mobile Data";
            default:
                return "No Connection";
        }
    }
}