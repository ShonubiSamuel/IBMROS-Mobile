using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeleteAccountController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backToMainButton;
    [SerializeField] private Button contactSupportButton;
    [SerializeField] private Button passwordToggleButton;
    [SerializeField] private Button confirmCheckbox;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI accountEmail;
    [SerializeField] private TextMeshProUGUI accountInitials;
    [SerializeField] private TextMeshProUGUI checkboxIcon;
    [SerializeField] private TextMeshProUGUI passwordError;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    // State
    private bool _passwordVisible = false;
    private bool _checkboxChecked = false;

    void Start()
    {
        AuthService.OnDeleteAccountSuccess += OnDeleteAccountSuccess;
        AuthService.OnDeleteAccountFailed += OnDeleteAccountFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnDeleteAccountSuccess -= OnDeleteAccountSuccess;
        AuthService.OnDeleteAccountFailed -= OnDeleteAccountFailed;
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
        PopulateAccountInfo();
    }

    private void WireEvents()
    {
        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (backToMainButton != null)
            backToMainButton.onClick.AddListener(OnCancelClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.AddListener(OnPasswordToggleClicked);

        if (confirmCheckbox != null)
            confirmCheckbox.onClick.AddListener(OnCheckboxClicked);

        if (contactSupportButton != null)
            contactSupportButton.onClick.AddListener(OnContactSupportClicked);
    }

    private void UnwireEvents()
    {
        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.RemoveListener(OnConfirmDeleteClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClicked);

        if (backToMainButton != null)
            backToMainButton.onClick.RemoveListener(OnCancelClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.RemoveListener(OnPasswordToggleClicked);

        if (confirmCheckbox != null)
            confirmCheckbox.onClick.RemoveListener(OnCheckboxClicked);

        if (contactSupportButton != null)
            contactSupportButton.onClick.RemoveListener(OnContactSupportClicked);
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.DeleteAccount)
            return;

        // OnEnable already called OnScreenActivated. Only refresh feedback here.
        ClearFeedback();
    }

    private void PopulateAccountInfo()
    {
        if (SessionManager.Instance == null)
            return;

        string email = SessionManager.Instance.Email ?? string.Empty;

        if (accountEmail != null)
            accountEmail.text = email;

        if (accountInitials != null)
            accountInitials.text = GetInitials(email);

        UpdateDeleteButtonState();
    }

    private string GetInitials(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "?";

        string name = email.Contains("@") ? email.Split('@')[0] : email;
        return name.Length >= 2
            ? name.Substring(0, 2).ToUpper()
            : name.ToUpper();
    }

    private void OnCheckboxClicked()
    {
        _checkboxChecked = !_checkboxChecked;

        if (checkboxIcon != null)
            checkboxIcon.text = _checkboxChecked ? "\u2713" : string.Empty;

        UpdateDeleteButtonState();
    }

    private void UpdateDeleteButtonState()
    {
        if (confirmDeleteButton != null)
            confirmDeleteButton.interactable = _checkboxChecked;
    }

    // BUTTON HANDLERS

    private async void OnConfirmDeleteClicked()
    {
        ClearFeedback();

        if (!_checkboxChecked)
        {
            ShowError("Please confirm you understand this action is permanent.");
            return;
        }

        string password = passwordInput?.text;

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowPasswordError("Password is required to delete your account.");
            return;
        }

        string email = SessionManager.Instance.Email;
        await AuthService.Instance.DeleteAccount(email, password);
    }

    private void OnCancelClicked()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.MainApp);
    }

    private void OnPasswordToggleClicked()
    {
        _passwordVisible = !_passwordVisible;

        if (passwordInput != null)
        {
            passwordInput.contentType = _passwordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }
    }

    private void OnContactSupportClicked()
    {
        Application.OpenURL(
            "mailto:support@ibmros.com?subject=Account%20Deactivation%20Request");
    }

    // AUTHSERVICE EVENT HANDLERS

    private void OnDeleteAccountSuccess(string message)
    {
        Debug.Log("[DeleteAccountController_UGUI] Account deleted. Navigating to Login.");
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnDeleteAccountFailed(string message, AuthError error)
    {
        ShowError(message);

        if (error == AuthError.WrongEmailOrPassword)
        {
            if (passwordInput != null)
                passwordInput.text = string.Empty;

            passwordInput?.Select();
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

        if (passwordError != null)
            passwordError.text = string.Empty;
    }

    public void SetLoadingState(bool isLoading)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(isLoading);

        if (confirmDeleteButton != null)
            confirmDeleteButton.interactable = !isLoading && _checkboxChecked;

        if (cancelButton != null)
            cancelButton.interactable = !isLoading;

        if (backToMainButton != null)
            backToMainButton.interactable = !isLoading;

        if (passwordInput != null)
            passwordInput.interactable = !isLoading;

        if (confirmCheckbox != null)
            confirmCheckbox.interactable = !isLoading;
    }

    // HELPERS

    private void ShowPasswordError(string message)
    {
        if (passwordError != null)
            passwordError.text = message;
    }

    private void ClearInputs()
    {
        if (passwordInput != null)
            passwordInput.text = string.Empty;

        _checkboxChecked = false;

        if (checkboxIcon != null)
            checkboxIcon.text = string.Empty;

        _passwordVisible = false;

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        UpdateDeleteButtonState();
    }
}