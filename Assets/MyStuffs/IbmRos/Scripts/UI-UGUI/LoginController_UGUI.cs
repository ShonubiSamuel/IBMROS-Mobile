using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToSignUpButton;
    [SerializeField] private Button forgotPasswordButton;
    [SerializeField] private Button passwordToggleButton;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image feedbackIcon;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    // State
    private bool _passwordVisible = false;

    void Start()
    {
        AuthService.OnLoginSuccess += OnLoginSuccess;
        AuthService.OnLoginFailed += OnLoginFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnLoginSuccess -= OnLoginSuccess;
        AuthService.OnLoginFailed -= OnLoginFailed;
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
        PreFillLastEmail();
    }

    private void WireEvents()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);

        if (goToSignUpButton != null)
            goToSignUpButton.onClick.AddListener(OnGoToSignUpClicked);

        if (forgotPasswordButton != null)
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.AddListener(OnPasswordToggleClicked);
    }

    private void UnwireEvents()
    {
        if (loginButton != null)
            loginButton.onClick.RemoveListener(OnLoginClicked);

        if (goToSignUpButton != null)
            goToSignUpButton.onClick.RemoveListener(OnGoToSignUpClicked);

        if (forgotPasswordButton != null)
            forgotPasswordButton.onClick.RemoveListener(OnForgotPasswordClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.RemoveListener(OnPasswordToggleClicked);
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.Login)
            return;

        // OnEnable already ran OnScreenActivated when the panel was activated.
        // Only refresh feedback here to avoid double-clearing inputs.
        ClearFeedback();
    }

    // BUTTON HANDLERS

    private async void OnLoginClicked()
    {
        ClearFeedback();
        await AuthService.Instance.Login(
            emailInput?.text.Trim(),
            passwordInput?.text
        );
    }

    private void OnGoToSignUpClicked()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.SignUp);
    }

    private void OnForgotPasswordClicked()
    {
        ClearFeedback();

        string email = emailInput?.text.Trim();
        if (!string.IsNullOrEmpty(email))
        {
            PlayerPrefs.SetString("ibm_ros_pending_email", email);
            PlayerPrefs.Save();
        }

        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.ForgotPassword);
    }

    private void OnPasswordToggleClicked()
    {
        _passwordVisible = !_passwordVisible;

        if (passwordInput != null)
            passwordInput.contentType = _passwordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

        if (passwordInput != null)
            passwordInput.ForceLabelUpdate();
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnLoginSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(500);
        if (this == null) return; // Guard: object may be destroyed after await
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.MainApp);
    }

    private async void OnLoginFailed(string message, AuthError error)
    {
        ShowError(message);

        switch (error)
        {
            case AuthError.EmailNotConfirmed:
                await Task.Delay(2000);
                if (this == null) return;
                ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.ConfirmEmail);
                break;

            case AuthError.UserNotFound:
                await Task.Delay(2000);
                if (this == null) return;
                OnGoToSignUpClicked();
                break;
        }
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

        if (loginButton != null)
            loginButton.interactable = !isLoading;

        if (emailInput != null)
            emailInput.interactable = !isLoading;

        if (passwordInput != null)
            passwordInput.interactable = !isLoading;

        if (forgotPasswordButton != null)
            forgotPasswordButton.interactable = !isLoading;

        if (goToSignUpButton != null)
            goToSignUpButton.interactable = !isLoading;
    }

    // HELPERS

    private void PreFillLastEmail()
    {
        if (AuthService.Instance == null)
            return;

        string lastEmail = AuthService.Instance.GetLastEmail();

        if (!string.IsNullOrEmpty(lastEmail) && emailInput != null)
            emailInput.text = lastEmail;
    }

    private void ClearInputs()
    {
        if (emailInput != null) emailInput.text = string.Empty;
        if (passwordInput != null) passwordInput.text = string.Empty;
    }
}