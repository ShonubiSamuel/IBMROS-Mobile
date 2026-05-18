using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SplashController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI appNameText;
    [SerializeField] private TextMeshProUGUI taglineText;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image loadingSpinner;
    [SerializeField] private GameObject loadingRow;

    private const int MinSplashDurationMs = 2000;
    private bool _awsReady = false;
    private bool _splashTimerDone = false;
    private bool _hasNavigated = false;
    private CancellationTokenSource _timerCts;

    // FIX: Subscriptions moved to OnEnable so they are registered before any Start()
    // fires. The working UI Toolkit version does the same. Subscribing in Start() risks
    // missing AwsManager.OnAwsReady if AwsManager fires it during its own Awake() —
    // which runs before any Start() but after all OnEnable() calls.
    void OnEnable()
    {
        AuthService.OnSessionRestored += OnSessionRestored;
        AuthService.OnSessionExpiredOrInvalid += OnSessionExpiredOrInvalid;
        AwsManager.OnAwsReady += OnAwsReady;
    }

    // FIX: Mirror OnEnable with OnDisable (not just OnDestroy) so stale handlers
    // cannot fire if this object is ever disabled and re-enabled.
    void OnDisable()
    {
        AuthService.OnSessionRestored -= OnSessionRestored;
        AuthService.OnSessionExpiredOrInvalid -= OnSessionExpiredOrInvalid;
        AwsManager.OnAwsReady -= OnAwsReady;

        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = null;
    }

    void OnDestroy()
    {
        // OnDisable already handles unsubscription and cancellation.
        // OnDestroy is kept for safety in case OnDisable is not called first.
        AuthService.OnSessionRestored -= OnSessionRestored;
        AuthService.OnSessionExpiredOrInvalid -= OnSessionExpiredOrInvalid;
        AwsManager.OnAwsReady -= OnAwsReady;
    }

    void Start()
    {
        Debug.Log("[SplashController_UGUI] Start called.");

        StartSplashTimer();

        // Fallback: AWS may have been fully initialized before OnEnable ran.
        // If IsInitialized is true here, the OnAwsReady event already fired and
        // our subscription missed it, so we set the flag manually.
        if (AwsManager.Instance != null && AwsManager.Instance.IsInitialized)
        {
            Debug.Log("[SplashController_UGUI] AWS already ready on Start.");
            _awsReady = true;
            TryCheckSession();
        }
    }

    // IAuthUI implementation
    public void OnScreenActivated() { }

    private async void StartSplashTimer()
    {
        Debug.Log("[SplashController_UGUI] Splash timer started.");

        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(MinSplashDurationMs, _timerCts.Token);
        }
        catch (TaskCanceledException)
        {
            Debug.Log("[SplashController_UGUI] Splash timer cancelled.");
            return;
        }

        Debug.Log("[SplashController_UGUI] Splash timer done.");
        _splashTimerDone = true;
        TryCheckSession();
    }

    private void OnAwsReady()
    {
        AwsManager.OnAwsReady -= OnAwsReady;
        Debug.Log("[SplashController_UGUI] AWS ready received.");
        _awsReady = true;
        TryCheckSession();
    }

    private async void TryCheckSession()
    {
        if (!_awsReady || !_splashTimerDone)
        {
            Debug.Log($"[SplashController_UGUI] TryCheckSession: " +
                      $"awsReady={_awsReady} timerDone={_splashTimerDone}. Waiting.");
            return;
        }

        if (_hasNavigated)
            return;

        if (AuthService.Instance == null)
        {
            Debug.LogError("[SplashController_UGUI] AuthService.Instance is null.");
            return;
        }

        Debug.Log("[SplashController_UGUI] Both conditions met. Checking session.");
        await AuthService.Instance.CheckSession();
    }

    private void OnSessionRestored()
    {
        Debug.Log("[SplashController_UGUI] Session restored. Going to MainApp.");

        if (_hasNavigated)
            return;

        _hasNavigated = true;

        if (ScreenNavigator_UGUI.Instance == null)
        {
            Debug.LogError("[SplashController_UGUI] ScreenNavigator_UGUI.Instance is null.");
            return;
        }

        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.MainApp);
    }

    private void OnSessionExpiredOrInvalid()
    {
        Debug.Log("[SplashController_UGUI] No valid session. Going to Login.");

        if (_hasNavigated)
            return;

        _hasNavigated = true;

        if (ScreenNavigator_UGUI.Instance == null)
        {
            Debug.LogError("[SplashController_UGUI] ScreenNavigator_UGUI.Instance is null.");
            return;
        }

        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    // IAUTHUI IMPLEMENTATION

    public void ShowError(string message)
    {
        if (loadingText != null)
            loadingText.text = message;
    }

    public void ShowSuccess(string message)
    {
        if (loadingText != null)
            loadingText.text = message;
    }

    public void ClearFeedback()
    {
        if (loadingText != null)
            loadingText.text = "Loading...";
    }

    public void SetLoadingState(bool isLoading)
    {
        if (loadingRow != null)
            loadingRow.SetActive(isLoading);
    }
}