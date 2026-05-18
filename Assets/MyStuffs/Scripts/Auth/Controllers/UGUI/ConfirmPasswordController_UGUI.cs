using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPasswordController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Code Input Fields")]
    [SerializeField] private TMP_InputField codeBox1;
    [SerializeField] private TMP_InputField codeBox2;
    [SerializeField] private TMP_InputField codeBox3;
    [SerializeField] private TMP_InputField codeBox4;
    [SerializeField] private TMP_InputField codeBox5;
    [SerializeField] private TMP_InputField codeBox6;

    [Header("Password Fields")]
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private TMP_InputField confirmNewPasswordInput;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resendCodeButton;
    [SerializeField] private Button backToForgotPasswordButton;
    [SerializeField] private Button goToLoginButton;
    [SerializeField] private Button newPasswordToggleButton;
    [SerializeField] private Button confirmNewPasswordToggleButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI emailDisplay;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Password Strength")]
    [SerializeField] private Slider strengthSlider;
    [SerializeField] private TextMeshProUGUI strengthLabel;

    // All six code boxes in order
    private TMP_InputField[] _codeBoxes;

    // State
    private bool _newPasswordVisible = false;
    private bool _confirmNewPasswordVisible = false;
    private bool _isResendOnCooldown = false;
    private const int ResendCooldownSeconds = 60;

    // FIX: CancellationTokenSource so the cooldown loop stops cleanly on navigate away.
    private CancellationTokenSource _cooldownCts;

    void Start()
    {
        AuthService.OnResetPasswordSuccess += OnResetPasswordSuccess;
        AuthService.OnResetPasswordFailed += OnResetPasswordFailed;
        AuthService.OnForgotPasswordSuccess += OnResendCodeSuccess;
        AuthService.OnForgotPasswordFailed += OnResendCodeFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnResetPasswordSuccess -= OnResetPasswordSuccess;
        AuthService.OnResetPasswordFailed -= OnResetPasswordFailed;
        AuthService.OnForgotPasswordSuccess -= OnResendCodeSuccess;
        AuthService.OnForgotPasswordFailed -= OnResendCodeFailed;
        AuthService.OnLoadingChanged -= OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged -= OnScreenChanged;

        _cooldownCts?.Cancel();
        _cooldownCts?.Dispose();
        _cooldownCts = null;
    }

    void OnEnable()
    {
        _codeBoxes = new TMP_InputField[]
        {
            codeBox1, codeBox2, codeBox3,
            codeBox4, codeBox5, codeBox6
        };

        WireEvents();
        ClearFeedback();
        OnScreenActivated();
    }

    void OnDisable()
    {
        UnwireEvents();

        _cooldownCts?.Cancel();
        _cooldownCts?.Dispose();
        _cooldownCts = null;
        _isResendOnCooldown = false;
    }

    // IAuthUI implementation
    public void OnScreenActivated()
    {
        ClearInputs();
        LoadPendingEmail();
    }

    private void WireEvents()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (resendCodeButton != null)
            resendCodeButton.onClick.AddListener(OnResendCodeClicked);

        if (backToForgotPasswordButton != null)
            backToForgotPasswordButton.onClick.AddListener(OnBackToForgotPasswordClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(OnGoToLoginClicked);

        if (newPasswordToggleButton != null)
            newPasswordToggleButton.onClick.AddListener(OnNewPasswordToggleClicked);

        if (confirmNewPasswordToggleButton != null)
            confirmNewPasswordToggleButton.onClick.AddListener(
                OnConfirmNewPasswordToggleClicked);

        if (newPasswordInput != null)
            newPasswordInput.onValueChanged.AddListener(UpdatePasswordStrength);

        if (_codeBoxes == null)
            return;

        for (int i = 0; i < _codeBoxes.Length; i++)
        {
            int index = i;
            var box = _codeBoxes[index];

            if (box == null)
                continue;

            box.onValueChanged.AddListener(value =>
                OnCodeBoxValueChanged(index, value));
        }
    }

    private void UnwireEvents()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (resendCodeButton != null)
            resendCodeButton.onClick.RemoveListener(OnResendCodeClicked);

        if (backToForgotPasswordButton != null)
            backToForgotPasswordButton.onClick.RemoveListener(
                OnBackToForgotPasswordClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.RemoveListener(OnGoToLoginClicked);

        if (newPasswordToggleButton != null)
            newPasswordToggleButton.onClick.RemoveListener(OnNewPasswordToggleClicked);

        if (confirmNewPasswordToggleButton != null)
            confirmNewPasswordToggleButton.onClick.RemoveListener(
                OnConfirmNewPasswordToggleClicked);

        if (newPasswordInput != null)
            newPasswordInput.onValueChanged.RemoveListener(UpdatePasswordStrength);

        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box != null)
                box.onValueChanged.RemoveAllListeners();
        }
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.ConfirmPassword)
            return;

        // OnEnable already called OnScreenActivated; only refresh feedback here.
        ClearFeedback();
    }

    private void LoadPendingEmail()
    {
        if (emailDisplay == null)
            return;

        if (AuthService.Instance == null)
            return;

        string pendingEmail = AuthService.Instance.GetPendingEmail();

        emailDisplay.text = string.IsNullOrEmpty(pendingEmail)
            ? "your email address"
            : AuthService.Instance.GetMaskedEmail(pendingEmail);
    }

    // CODE BOX HANDLER

    private void OnCodeBoxValueChanged(int index, string newValue)
    {
        if (_codeBoxes == null)
            return;

        var box = _codeBoxes[index];

        if (box == null)
            return;

        if (!string.IsNullOrEmpty(newValue))
        {
            string digit = newValue[newValue.Length - 1].ToString();

            if (!char.IsDigit(digit[0]))
            {
                box.SetTextWithoutNotify(string.Empty);
                return;
            }

            box.SetTextWithoutNotify(digit);

            if (index < _codeBoxes.Length - 1)
                _codeBoxes[index + 1]?.Select();
            else
                newPasswordInput?.Select();
        }
        else
        {
            // FIX: Jump back to the previous box when the current one is cleared.
            if (index > 0)
                _codeBoxes[index - 1]?.Select();
        }
    }

    // BUTTON HANDLERS

    private async void OnConfirmClicked()
    {
        ClearFeedback();

        string email = AuthService.Instance.GetPendingEmail();

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Session expired. Please restart the password reset process.");
            await Task.Delay(2000);
            if (this == null) return;
            GoBackToForgotPassword();
            return;
        }

        await AuthService.Instance.ResetPassword(
            email,
            GetFullCode(),
            newPasswordInput?.text,
            confirmNewPasswordInput?.text
        );
    }

    private async void OnResendCodeClicked()
    {
        if (_isResendOnCooldown)
            return;

        ClearFeedback();

        string email = AuthService.Instance.GetPendingEmail();

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Session expired. Please restart the password reset process.");
            await Task.Delay(2000);
            if (this == null) return;
            GoBackToForgotPassword();
            return;
        }

        await AuthService.Instance.ForgotPassword(email);
    }

    private void OnBackToForgotPasswordClicked()
    {
        GoBackToForgotPassword();
    }

    private void OnGoToLoginClicked()
    {
        GoToLoginScreen();
    }

    private void OnNewPasswordToggleClicked()
    {
        _newPasswordVisible = !_newPasswordVisible;

        if (newPasswordInput != null)
        {
            newPasswordInput.contentType = _newPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            newPasswordInput.ForceLabelUpdate();
        }
    }

    private void OnConfirmNewPasswordToggleClicked()
    {
        _confirmNewPasswordVisible = !_confirmNewPasswordVisible;

        if (confirmNewPasswordInput != null)
        {
            confirmNewPasswordInput.contentType = _confirmNewPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            confirmNewPasswordInput.ForceLabelUpdate();
        }
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnResetPasswordSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(1500);
        if (this == null) return;
        GoToLoginScreen();
    }

    private async void OnResetPasswordFailed(string message, AuthError error)
    {
        ShowError(message);

        if (error == AuthError.ExpiredConfirmationCode)
        {
            await Task.Delay(2000);
            if (this == null) return;
            GoBackToForgotPassword();
        }
    }

    private void OnResendCodeSuccess(string message)
    {
        ShowSuccess(message);
        StartResendCooldown();
    }

    private void OnResendCodeFailed(string message, AuthError error)
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

        if (confirmButton != null)
            confirmButton.interactable = !isLoading;

        if (resendCodeButton != null)
            resendCodeButton.interactable = !isLoading && !_isResendOnCooldown;

        if (backToForgotPasswordButton != null)
            backToForgotPasswordButton.interactable = !isLoading;

        if (goToLoginButton != null)
            goToLoginButton.interactable = !isLoading;

        if (newPasswordInput != null)
            newPasswordInput.interactable = !isLoading;

        if (confirmNewPasswordInput != null)
            confirmNewPasswordInput.interactable = !isLoading;

        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box != null)
                box.interactable = !isLoading;
        }
    }

    // PASSWORD STRENGTH

    private void UpdatePasswordStrength(string password)
    {
        bool hasLength = password.Length >= 8;
        bool hasUpper = System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]");
        bool hasDigit = System.Text.RegularExpressions.Regex.IsMatch(password, @"\d");
        bool hasSpecial = System.Text.RegularExpressions.Regex
            .IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]");

        int score = 0;
        if (hasLength) score++;
        if (hasUpper) score++;
        if (hasDigit) score++;
        if (hasSpecial) score++;

        if (strengthSlider != null)
            strengthSlider.value = score / 4f;

        if (strengthLabel == null)
            return;

        switch (score)
        {
            case 1: strengthLabel.text = "WEAK"; break;
            case 2: strengthLabel.text = "FAIR"; break;
            case 3: strengthLabel.text = "GOOD"; break;
            case 4: strengthLabel.text = "STRONG"; break;
            default: strengthLabel.text = string.Empty; break;
        }
    }

    // HELPERS

    private string GetFullCode()
    {
        string code = string.Empty;

        if (_codeBoxes == null)
            return code;

        foreach (var box in _codeBoxes)
            code += box?.text ?? string.Empty;

        return code;
    }

    // FIX: Cancellable cooldown loop. OnDisable cancels this so it does not run
    // after the screen has been navigated away from.
    private async void StartResendCooldown()
    {
        _cooldownCts?.Cancel();
        _cooldownCts?.Dispose();
        _cooldownCts = new CancellationTokenSource();

        var token = _cooldownCts.Token;

        _isResendOnCooldown = true;

        if (resendCodeButton != null)
            resendCodeButton.interactable = false;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        int remaining = ResendCooldownSeconds;

        while (remaining > 0 && !token.IsCancellationRequested)
        {
            if (countdownText != null)
                countdownText.text = $"Resend code in {remaining}s";

            try
            {
                await Task.Delay(1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            remaining--;
        }

        if (token.IsCancellationRequested)
            return;

        _isResendOnCooldown = false;

        if (resendCodeButton != null)
            resendCodeButton.interactable = true;

        if (countdownText != null)
        {
            countdownText.text = string.Empty;
            countdownText.gameObject.SetActive(false);
        }
    }

    private void GoToLoginScreen()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private void GoBackToForgotPassword()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.ForgotPassword);
    }

    private void ClearInputs()
    {
        if (newPasswordInput != null) newPasswordInput.text = string.Empty;
        if (confirmNewPasswordInput != null) confirmNewPasswordInput.text = string.Empty;

        if (strengthSlider != null) strengthSlider.value = 0;
        if (strengthLabel != null) strengthLabel.text = string.Empty;

        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box != null)
                box.SetTextWithoutNotify(string.Empty);
        }
    }
}