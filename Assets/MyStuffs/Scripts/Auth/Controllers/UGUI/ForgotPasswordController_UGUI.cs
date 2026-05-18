// ============================================================
// ForgotPasswordController_UGUI.cs
// ============================================================

using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ForgotPasswordController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailInput;

    [Header("Buttons")]
    [SerializeField] private Button sendCodeButton;
    [SerializeField] private Button backToLoginButton;
    [SerializeField] private Button goToLoginButton;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    void Start()
    {
        AuthService.OnForgotPasswordSuccess += OnForgotPasswordSuccess;
        AuthService.OnForgotPasswordFailed += OnForgotPasswordFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnForgotPasswordSuccess -= OnForgotPasswordSuccess;
        AuthService.OnForgotPasswordFailed -= OnForgotPasswordFailed;
        AuthService.OnLoadingChanged -= OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged -= OnScreenChanged;
    }

    void OnEnable()
    {
        WireEvents();
        ClearFeedback();
        OnScreenActivated();
    }

    void OnDisable()
    {
        UnwireEvents();
    }

    // IAuthUI implementation
    public void OnScreenActivated()
    {
        ClearInputs();
        PreFillEmail();
    }

    private void WireEvents()
    {
        if (sendCodeButton != null)
            sendCodeButton.onClick.AddListener(OnSendCodeClicked);

        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(OnBackToLoginClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(OnBackToLoginClicked);
    }

    private void UnwireEvents()
    {
        if (sendCodeButton != null)
            sendCodeButton.onClick.RemoveListener(OnSendCodeClicked);

        if (backToLoginButton != null)
            backToLoginButton.onClick.RemoveListener(OnBackToLoginClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.RemoveListener(OnBackToLoginClicked);
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.ForgotPassword)
            return;

        // OnEnable already called OnScreenActivated. Only refresh feedback here.
        ClearFeedback();
    }

    private void PreFillEmail()
    {
        if (AuthService.Instance == null)
            return;

        string pendingEmail = AuthService.Instance.GetPendingEmail();

        if (!string.IsNullOrEmpty(pendingEmail) && emailInput != null)
            emailInput.text = pendingEmail;
    }

    // BUTTON HANDLERS

    private async void OnSendCodeClicked()
    {
        ClearFeedback();
        await AuthService.Instance.ForgotPassword(emailInput?.text.Trim());
    }

    private void OnBackToLoginClicked()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnForgotPasswordSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(1500);
        if (this == null) return; // Guard: object may be destroyed after await
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.ConfirmPassword);
    }

    private void OnForgotPasswordFailed(string message, AuthError error)
    {
        ShowError(message);
    }

    private void OnLoadingStateChanged(bool isLoading, string message)
    {
        SetLoadingState(isLoading);

        if (loadingText != null)
            loadingText.text = message;
    }

    // IAUTHUI IMPLEMENTATION

    public void ShowError(string message)
    {
        if (feedbackBanner != null)
            feedbackBanner.SetActive(true);

        if (feedbackText != null)
            feedbackText.text = message;
    }

    public void ShowSuccess(string message)
    {
        if (feedbackBanner != null)
            feedbackBanner.SetActive(true);

        if (feedbackText != null)
            feedbackText.text = message;
    }

    public void ClearFeedback()
    {
        if (feedbackBanner != null)
            feedbackBanner.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = string.Empty;
    }

    public void SetLoadingState(bool isLoading)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(isLoading);

        if (sendCodeButton != null)
            sendCodeButton.interactable = !isLoading;

        if (emailInput != null)
            emailInput.interactable = !isLoading;

        if (backToLoginButton != null)
            backToLoginButton.interactable = !isLoading;

        if (goToLoginButton != null)
            goToLoginButton.interactable = !isLoading;
    }

    // HELPERS

    private void ClearInputs()
    {
        if (emailInput != null)
            emailInput.text = string.Empty;
    }
}