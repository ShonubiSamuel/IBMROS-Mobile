using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SessionRefreshService : MonoBehaviour
{
    public static SessionRefreshService Instance { get; private set; }

    private const int CheckIntervalSeconds = 60;

    public static event Action OnSessionExpired;
    public static event Action OnSessionRefreshed;

    private CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning = false;

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

    void OnDestroy()
    {
        Stop();
    }

    // Renamed from Start() to Begin() to avoid
    // conflicting with MonoBehaviour.Start()
    public void Begin()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        Debug.Log("[SessionRefreshService] Auto refresh started.");
        _ = RunRefreshLoop(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        Debug.Log("[SessionRefreshService] Auto refresh stopped.");
    }

    public bool IsRunning => _isRunning;

    private async Task RunRefreshLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckIntervalSeconds * 1000, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            await CheckAndRefresh();
        }
    }

    private async Task CheckAndRefresh()
    {
        if (!SessionManager.Instance.IsLoggedIn)
            return;

        if (!SessionManager.Instance.IsTokenNearExpiry())
            return;

        Debug.Log("[SessionRefreshService] Token near expiry. Refreshing silently...");

        AuthResult result = await RetryHelper.ExecuteWithRetry(
            () => AuthManager.Instance.RefreshSession()
                .ContinueWith(t => t.Result),
            "SilentSessionRefresh",
            maxRetries: 3,
            initialDelayMs: 2000
        );

        if (result.IsSuccess)
        {
            Debug.Log("[SessionRefreshService] Silent refresh successful.");
            OnSessionRefreshed?.Invoke();
        }
        else
        {
            Debug.LogWarning("[SessionRefreshService] Silent refresh failed. Session expired.");
            Stop();
            OnSessionExpired?.Invoke();
        }
    }
}