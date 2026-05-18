using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class ForgotPasswordController : MonoBehaviour, IAuthUI
{
    // UI Elements
    private TextField _emailInput;
    private Button _sendCodeButton;
    private Button _backToLoginButton;
    private Button _goToLoginButton;
    private VisualElement _feedbackBanner;
    private Label _feedbackText;
    private VisualElement _loadingOverlay;
    private Label _loadingText;
    private CancellationTokenSource _feedbackCts;

    void Start()
    {
        AuthService.OnForgotPasswordSuccess += OnForgotPasswordSuccess;
        AuthService.OnForgotPasswordFailed += OnForgotPasswordFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
    }

    void OnDestroy()
    {
        AuthService.OnForgotPasswordSuccess -= OnForgotPasswordSuccess;
        AuthService.OnForgotPasswordFailed -= OnForgotPasswordFailed;
        AuthService.OnLoadingChanged -= OnLoadingStateChanged;
        _feedbackCts?.Cancel();
        _feedbackCts?.Dispose();
        UIManager.OnScreensReady -= OnScreensReady;
    }

    void OnEnable()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsReady)
        {
            QueryElements();
            WireEvents();
            ClearFeedback();
            OnScreenActivated();
        }
        else
        {
            UIManager.OnScreensReady += OnScreensReady;
        }
    }

    void OnDisable()
    {
        UIManager.OnScreensReady -= OnScreensReady;
    }

    private void OnScreensReady()
    {
        UIManager.OnScreensReady -= OnScreensReady;
        QueryElements();
        WireEvents();
        ClearFeedback();
        OnScreenActivated();
    }

    // IAuthUI implementation
    public void OnScreenActivated()
    {
        ClearInputs();
        PreFillEmail();
    }

    private void QueryElements()
    {
        var container = UIManager.Instance.GetScreenContainer(ScreenName.ForgotPassword);

        if (container == null)
        {
            Debug.LogError("[ForgotPasswordController] ForgotPassword container not found.");
            return;
        }

        _emailInput = container.Q<TextField>("EmailInput");
        _sendCodeButton = container.Q<Button>("SendCodeButton");
        _backToLoginButton = container.Q<Button>("BackToLoginButton");
        _goToLoginButton = container.Q<Button>("GoToLoginButton");
        _feedbackBanner = container.Q<VisualElement>("FeedbackBanner");
        _feedbackText = container.Q<Label>("FeedbackText");
        _loadingOverlay = container.Q<VisualElement>("LoadingOverlay");
        _loadingText = container.Q<Label>("LoadingText");
    }

    private void WireEvents()
    {
        if (_sendCodeButton != null)
            _sendCodeButton.clicked += OnSendCodeClicked;

        if (_backToLoginButton != null)
            _backToLoginButton.clicked += OnBackToLoginClicked;

        if (_goToLoginButton != null)
            _goToLoginButton.clicked += OnBackToLoginClicked;
    }

    private void PreFillEmail()
    {
        if (AuthService.Instance == null)
            return;

        string pendingEmail = AuthService.Instance.GetPendingEmail();

        if (!string.IsNullOrEmpty(pendingEmail) && _emailInput != null)
            _emailInput.value = pendingEmail;
    }

    // BUTTON HANDLERS

    private async void OnSendCodeClicked()
    {
        ClearFeedback();
        await AuthService.Instance.ForgotPassword(_emailInput?.value.Trim());
    }

    private void OnBackToLoginClicked()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator.Instance.NavigateTo(ScreenName.Login);
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnForgotPasswordSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(1500);
        ScreenNavigator.Instance.NavigateTo(ScreenName.ConfirmPassword);
    }

    private void OnForgotPasswordFailed(string message, AuthError error)
    {
        ShowError(message);
    }

    private void OnLoadingStateChanged(bool isLoading, string message)
    {
        SetLoadingState(isLoading);

        if (_loadingText != null)
            _loadingText.text = message;
    }

    // IAUTHUI IMPLEMENTATION

    public void ShowError(string message)
    {
        _feedbackCts?.Cancel();
        _feedbackCts = new CancellationTokenSource();
        _ = FeedbackHelper.ShowError(
            _feedbackBanner, _feedbackText, message, _feedbackCts.Token);
    }

    public void ShowSuccess(string message)
    {
        _feedbackCts?.Cancel();
        _feedbackCts = new CancellationTokenSource();
        _ = FeedbackHelper.ShowSuccess(
            _feedbackBanner, _feedbackText, message, _feedbackCts.Token);
    }

    public void ClearFeedback()
    {
        _feedbackCts?.Cancel();
        _feedbackCts = null;
        FeedbackHelper.Clear(_feedbackBanner, _feedbackText);
    }

    public void SetLoadingState(bool isLoading)
    {
        if (isLoading)
            _loadingOverlay?.AddToClassList("loading-overlay--visible");
        else
            _loadingOverlay?.RemoveFromClassList("loading-overlay--visible");

        _sendCodeButton?.SetEnabled(!isLoading);
        _emailInput?.SetEnabled(!isLoading);
        _backToLoginButton?.SetEnabled(!isLoading);
        _goToLoginButton?.SetEnabled(!isLoading);
    }

    // HELPERS

    private void ClearInputs()
    {
        if (_emailInput != null)
            _emailInput.value = string.Empty;
    }
}