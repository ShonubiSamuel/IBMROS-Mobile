using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class ConfirmPasswordController : MonoBehaviour, IAuthUI
{
    // UI Elements
    private TextField _codeBox1;
    private TextField _codeBox2;
    private TextField _codeBox3;
    private TextField _codeBox4;
    private TextField _codeBox5;
    private TextField _codeBox6;
    private TextField _newPasswordInput;
    private TextField _confirmNewPasswordInput;
    private Button _confirmButton;
    private Button _resendCodeButton;
    private Button _backToForgotPasswordButton;
    private Button _goToLoginButton;
    private Button _newPasswordToggle;
    private Button _confirmNewPasswordToggle;
    private VisualElement _newPasswordInputRow;
    private VisualElement _confirmNewPasswordInputRow;
    private Label _emailDisplay;
    private Label _codeError;
    private Label _newPasswordError;
    private Label _confirmNewPasswordError;
    private Label _countdownText;
    private VisualElement _strengthFill;
    private Label _strengthLabel;
    private VisualElement _feedbackBanner;
    private Label _feedbackText;
    private VisualElement _loadingOverlay;
    private Label _loadingText;
    private CancellationTokenSource _feedbackCts;

    // All six code boxes in order
    private TextField[] _codeBoxes;

    // State
    private bool _newPasswordVisible = false;
    private bool _confirmNewPasswordVisible = false;
    private bool _isResendOnCooldown = false;
    private const int ResendCooldownSeconds = 60;

    void Start()
    {
        AuthService.OnResetPasswordSuccess += OnResetPasswordSuccess;
        AuthService.OnResetPasswordFailed += OnResetPasswordFailed;
        AuthService.OnForgotPasswordSuccess += OnResendCodeSuccess;
        AuthService.OnForgotPasswordFailed += OnResendCodeFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
    }

    void OnDestroy()
    {
        AuthService.OnResetPasswordSuccess -= OnResetPasswordSuccess;
        AuthService.OnResetPasswordFailed -= OnResetPasswordFailed;
        AuthService.OnForgotPasswordSuccess -= OnResendCodeSuccess;
        AuthService.OnForgotPasswordFailed -= OnResendCodeFailed;
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
        LoadPendingEmail();
    }

    private void QueryElements()
    {
        var container = UIManager.Instance.GetScreenContainer(ScreenName.ConfirmPassword);

        if (container == null)
        {
            Debug.LogError("[ConfirmPasswordController] ConfirmPassword container not found.");
            return;
        }

        _codeBox1 = container.Q<TextField>("CodeBox1");
        _codeBox2 = container.Q<TextField>("CodeBox2");
        _codeBox3 = container.Q<TextField>("CodeBox3");
        _codeBox4 = container.Q<TextField>("CodeBox4");
        _codeBox5 = container.Q<TextField>("CodeBox5");
        _codeBox6 = container.Q<TextField>("CodeBox6");

        _codeBoxes = new TextField[]
        {
            _codeBox1, _codeBox2, _codeBox3,
            _codeBox4, _codeBox5, _codeBox6
        };

        _newPasswordInput = container.Q<TextField>("NewPasswordInput");
        _confirmNewPasswordInput = container.Q<TextField>("ConfirmNewPasswordInput");
        _newPasswordInputRow = container.Q<VisualElement>("NewPasswordInputRow");
        _confirmNewPasswordInputRow = container.Q<VisualElement>("ConfirmNewPasswordInputRow");

        _confirmButton = container.Q<Button>("ConfirmButton");
        _resendCodeButton = container.Q<Button>("ResendCodeButton");
        _backToForgotPasswordButton = container.Q<Button>("BackToForgotPasswordButton");
        _goToLoginButton = container.Q<Button>("GoToLoginButton");
        _newPasswordToggle = container.Q<Button>("NewPasswordToggle");
        _confirmNewPasswordToggle = container.Q<Button>("ConfirmNewPasswordToggle");

        _feedbackBanner = container.Q<VisualElement>("FeedbackBanner");
        _feedbackText = container.Q<Label>("FeedbackText");
        _emailDisplay = container.Q<Label>("EmailDisplay");
        _codeError = container.Q<Label>("CodeError");
        _newPasswordError = container.Q<Label>("NewPasswordError");
        _confirmNewPasswordError = container.Q<Label>("ConfirmNewPasswordError");
        _countdownText = container.Q<Label>("CountdownText");
        _loadingOverlay = container.Q<VisualElement>("LoadingOverlay");
        _loadingText = container.Q<Label>("LoadingText");

        _strengthFill = container.Q<VisualElement>("StrengthFill");
        _strengthLabel = container.Q<Label>("StrengthLabel");
    }

    private void WireEvents()
    {
        if (_confirmButton != null)
            _confirmButton.clicked += OnConfirmClicked;

        if (_resendCodeButton != null)
            _resendCodeButton.clicked += OnResendCodeClicked;

        if (_backToForgotPasswordButton != null)
            _backToForgotPasswordButton.clicked += OnBackToForgotPasswordClicked;

        if (_goToLoginButton != null)
            _goToLoginButton.clicked += OnGoToLoginClicked;

        if (_newPasswordToggle != null)
            _newPasswordToggle.clicked += OnNewPasswordToggleClicked;

        if (_confirmNewPasswordToggle != null)
            _confirmNewPasswordToggle.clicked += OnConfirmNewPasswordToggleClicked;

        if (_newPasswordInput != null)
        {
            _newPasswordInput.RegisterCallback<FocusInEvent>(evt =>
                _newPasswordInputRow?.AddToClassList("input-password-row--focused"));

            _newPasswordInput.RegisterCallback<FocusOutEvent>(evt =>
                _newPasswordInputRow?.RemoveFromClassList("input-password-row--focused"));

            _newPasswordInput.RegisterValueChangedCallback(evt =>
                UpdatePasswordStrength(evt.newValue));
        }

        if (_confirmNewPasswordInput != null)
        {
            _confirmNewPasswordInput.RegisterCallback<FocusInEvent>(evt =>
                _confirmNewPasswordInputRow?.AddToClassList("input-password-row--focused"));

            _confirmNewPasswordInput.RegisterCallback<FocusOutEvent>(evt =>
                _confirmNewPasswordInputRow?.RemoveFromClassList("input-password-row--focused"));
        }

        for (int i = 0; i < _codeBoxes.Length; i++)
        {
            int index = i;
            var box = _codeBoxes[index];

            if (box == null)
                continue;

            box.RegisterValueChangedCallback(evt =>
                OnCodeBoxValueChanged(index, evt.newValue));

            box.RegisterCallback<FocusInEvent>(evt =>
                SetCodeBoxFocused(index, true));

            box.RegisterCallback<FocusOutEvent>(evt =>
                SetCodeBoxFocused(index, false));

            box.RegisterCallback<KeyDownEvent>(evt =>
                OnCodeBoxKeyDown(index, evt));
        }
    }

    private void LoadPendingEmail()
    {
        if (_emailDisplay == null)
            return;

        if (AuthService.Instance == null)
            return;

        string pendingEmail = AuthService.Instance.GetPendingEmail();

        _emailDisplay.text = string.IsNullOrEmpty(pendingEmail)
            ? "your email address"
            : AuthService.Instance.GetMaskedEmail(pendingEmail);
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
            GoBackToForgotPassword();
            return;
        }

        await AuthService.Instance.ResetPassword(
            email,
            GetFullCode(),
            _newPasswordInput?.value,
            _confirmNewPasswordInput?.value
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

        if (_newPasswordInput != null)
            _newPasswordInput.isPasswordField = !_newPasswordVisible;

        if (_newPasswordToggle != null)
            _newPasswordToggle.text = _newPasswordVisible ? "\U0001F440" : "\U0001F441";
    }

    private void OnConfirmNewPasswordToggleClicked()
    {
        _confirmNewPasswordVisible = !_confirmNewPasswordVisible;

        if (_confirmNewPasswordInput != null)
            _confirmNewPasswordInput.isPasswordField = !_confirmNewPasswordVisible;

        if (_confirmNewPasswordToggle != null)
            _confirmNewPasswordToggle.text = _confirmNewPasswordVisible
                ? "\U0001F440"
                : "\U0001F441";
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnResetPasswordSuccess(string message)
    {
        SetAllBoxesSuccess();
        ShowSuccess(message);
        await Task.Delay(1500);
        GoToLoginScreen();
    }

    private async void OnResetPasswordFailed(string message, AuthError error)
    {
        ShowError(message);
        SetAllBoxesError();

        if (error == AuthError.ExpiredConfirmationCode)
        {
            await Task.Delay(2000);
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

        if (_codeError != null)
        {
            _codeError.text = string.Empty;
            _codeError.RemoveFromClassList("input-error-text--visible");
        }

        if (_newPasswordError != null)
        {
            _newPasswordError.text = string.Empty;
            _newPasswordError.RemoveFromClassList("input-error-text--visible");
        }

        if (_confirmNewPasswordError != null)
        {
            _confirmNewPasswordError.text = string.Empty;
            _confirmNewPasswordError.RemoveFromClassList("input-error-text--visible");
        }
    }

    public void SetLoadingState(bool isLoading)
    {
        if (isLoading)
            _loadingOverlay?.AddToClassList("loading-overlay--visible");
        else
            _loadingOverlay?.RemoveFromClassList("loading-overlay--visible");

        _confirmButton?.SetEnabled(!isLoading);
        _resendCodeButton?.SetEnabled(!isLoading && !_isResendOnCooldown);
        _backToForgotPasswordButton?.SetEnabled(!isLoading);
        _goToLoginButton?.SetEnabled(!isLoading);
        _newPasswordInput?.SetEnabled(!isLoading);
        _confirmNewPasswordInput?.SetEnabled(!isLoading);

        foreach (var box in _codeBoxes)
            box?.SetEnabled(!isLoading);
    }

    // CODE BOX HELPERS

    private void OnCodeBoxValueChanged(int index, string newValue)
    {
        var box = _codeBoxes[index];

        if (box == null)
            return;

        if (!string.IsNullOrEmpty(newValue))
        {
            string digit = newValue[newValue.Length - 1].ToString();

            if (!char.IsDigit(digit[0]))
            {
                box.SetValueWithoutNotify(string.Empty);
                return;
            }

            box.SetValueWithoutNotify(digit);
            box.AddToClassList("confirm-password-code-box--filled");
            box.RemoveFromClassList("confirm-password-code-box--error");

            if (index < _codeBoxes.Length - 1)
                _codeBoxes[index + 1]?.Focus();
            else
                _newPasswordInput?.Focus();
        }
        else
        {
            box.RemoveFromClassList("confirm-password-code-box--filled");
        }
    }

    private void OnCodeBoxKeyDown(int index, KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Backspace)
        {
            var box = _codeBoxes[index];

            if (box != null && string.IsNullOrEmpty(box.value) && index > 0)
            {
                _codeBoxes[index - 1]?.Focus();
                evt.StopPropagation();
            }
        }
    }

    private void SetCodeBoxFocused(int index, bool focused)
    {
        var box = _codeBoxes[index];

        if (box == null)
            return;

        if (focused)
            box.AddToClassList("confirm-password-code-box--focused");
        else
            box.RemoveFromClassList("confirm-password-code-box--focused");
    }

    private string GetFullCode()
    {
        string code = string.Empty;

        foreach (var box in _codeBoxes)
            code += box?.value ?? string.Empty;

        return code;
    }

    private void SetAllBoxesError()
    {
        foreach (var box in _codeBoxes)
        {
            box?.RemoveFromClassList("confirm-password-code-box--filled");
            box?.RemoveFromClassList("confirm-password-code-box--focused");
            box?.AddToClassList("confirm-password-code-box--error");
        }
    }

    private void SetAllBoxesSuccess()
    {
        foreach (var box in _codeBoxes)
        {
            box?.RemoveFromClassList("confirm-password-code-box--filled");
            box?.RemoveFromClassList("confirm-password-code-box--error");
            box?.AddToClassList("confirm-password-code-box--success");
        }
    }

    private async void StartResendCooldown()
    {
        _isResendOnCooldown = true;
        _resendCodeButton?.SetEnabled(false);
        _countdownText?.AddToClassList("countdown-text--visible");

        int remaining = ResendCooldownSeconds;

        while (remaining > 0)
        {
            if (_countdownText != null)
                _countdownText.text = $"Resend code in {remaining}s";

            await Task.Delay(1000);
            remaining--;
        }

        _isResendOnCooldown = false;
        _resendCodeButton?.SetEnabled(true);
        _countdownText?.RemoveFromClassList("countdown-text--visible");

        if (_countdownText != null)
            _countdownText.text = string.Empty;
    }

    private void GoToLoginScreen()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator.Instance.NavigateTo(ScreenName.Login);
    }

    private void GoBackToForgotPassword()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator.Instance.NavigateTo(ScreenName.ForgotPassword);
    }

    // PASSWORD STRENGTH

    private void UpdatePasswordStrength(string password)
    {
        bool hasLength = password.Length >= 8;
        bool hasUpper = Regex.IsMatch(password, @"[A-Z]");
        bool hasDigit = Regex.IsMatch(password, @"\d");
        bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]");

        int score = 0;
        if (hasLength) score++;
        if (hasUpper) score++;
        if (hasDigit) score++;
        if (hasSpecial) score++;

        UpdateStrengthBar(score);
    }

    private void UpdateStrengthBar(int score)
    {
        if (_strengthFill == null || _strengthLabel == null)
            return;

        _strengthFill.RemoveFromClassList("confirm-password-strength-fill--weak");
        _strengthFill.RemoveFromClassList("confirm-password-strength-fill--fair");
        _strengthFill.RemoveFromClassList("confirm-password-strength-fill--good");
        _strengthFill.RemoveFromClassList("confirm-password-strength-fill--strong");

        _strengthLabel.RemoveFromClassList("confirm-password-strength-label--weak");
        _strengthLabel.RemoveFromClassList("confirm-password-strength-label--fair");
        _strengthLabel.RemoveFromClassList("confirm-password-strength-label--good");
        _strengthLabel.RemoveFromClassList("confirm-password-strength-label--strong");

        switch (score)
        {
            case 1:
                _strengthFill.AddToClassList("confirm-password-strength-fill--weak");
                _strengthLabel.AddToClassList("confirm-password-strength-label--weak");
                _strengthLabel.text = "WEAK";
                break;
            case 2:
                _strengthFill.AddToClassList("confirm-password-strength-fill--fair");
                _strengthLabel.AddToClassList("confirm-password-strength-label--fair");
                _strengthLabel.text = "FAIR";
                break;
            case 3:
                _strengthFill.AddToClassList("confirm-password-strength-fill--good");
                _strengthLabel.AddToClassList("confirm-password-strength-label--good");
                _strengthLabel.text = "GOOD";
                break;
            case 4:
                _strengthFill.AddToClassList("confirm-password-strength-fill--strong");
                _strengthLabel.AddToClassList("confirm-password-strength-label--strong");
                _strengthLabel.text = "STRONG";
                break;
            default:
                _strengthLabel.text = string.Empty;
                break;
        }
    }

    // HELPERS

    private void ClearInputs()
    {
        if (_newPasswordInput != null) _newPasswordInput.value = string.Empty;
        if (_confirmNewPasswordInput != null) _confirmNewPasswordInput.value = string.Empty;

        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box == null)
                continue;

            box.SetValueWithoutNotify(string.Empty);
            box.RemoveFromClassList("confirm-password-code-box--filled");
            box.RemoveFromClassList("confirm-password-code-box--error");
            box.RemoveFromClassList("confirm-password-code-box--success");
            box.RemoveFromClassList("confirm-password-code-box--focused");
        }

        if (_strengthFill != null)
        {
            _strengthFill.RemoveFromClassList("confirm-password-strength-fill--weak");
            _strengthFill.RemoveFromClassList("confirm-password-strength-fill--fair");
            _strengthFill.RemoveFromClassList("confirm-password-strength-fill--good");
            _strengthFill.RemoveFromClassList("confirm-password-strength-fill--strong");
        }

        if (_strengthLabel != null)
            _strengthLabel.text = string.Empty;
    }
}