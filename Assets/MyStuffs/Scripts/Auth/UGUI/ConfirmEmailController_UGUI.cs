using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmEmailController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Code Input Fields")]
    [SerializeField] private TMP_InputField codeBox1;
    [SerializeField] private TMP_InputField codeBox2;
    [SerializeField] private TMP_InputField codeBox3;
    [SerializeField] private TMP_InputField codeBox4;
    [SerializeField] private TMP_InputField codeBox5;
    [SerializeField] private TMP_InputField codeBox6;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resendCodeButton;
    [SerializeField] private Button backToSignUpButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI emailDisplay;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    // All six boxes in order
    private TMP_InputField[] _codeBoxes;

    // State
    private bool _isResendOnCooldown = false;
    private const int ResendCooldownSeconds = 60;

    // FIX: CancellationTokenSource so the cooldown loop can be cleanly stopped
    // if the user navigates away before the countdown finishes.
    private CancellationTokenSource _cooldownCts;

    void Start()
    {
        AuthService.OnConfirmEmailSuccess += OnConfirmEmailSuccess;
        AuthService.OnConfirmEmailFailed += OnConfirmEmailFailed;
        AuthService.OnResendCodeSuccess += OnResendCodeSuccess;
        AuthService.OnResendCodeFailed += OnResendCodeFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnConfirmEmailSuccess -= OnConfirmEmailSuccess;
        AuthService.OnConfirmEmailFailed -= OnConfirmEmailFailed;
        AuthService.OnResendCodeSuccess -= OnResendCodeSuccess;
        AuthService.OnResendCodeFailed -= OnResendCodeFailed;
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

        // Cancel any running cooldown so it does not bleed into the next visit.
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

        if (backToSignUpButton != null)
            backToSignUpButton.onClick.AddListener(OnBackToSignUpClicked);

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

        if (backToSignUpButton != null)
            backToSignUpButton.onClick.RemoveListener(OnBackToSignUpClicked);

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
        if (screen != ScreenName.ConfirmEmail)
            return;

        // Panel was re-activated via ScreenNavigator; OnEnable already ran and
        // called OnScreenActivated, so we only need to refresh feedback here.
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

    // CODE BOX HANDLERS

    private void OnCodeBoxValueChanged(int index, string newValue)
    {
        if (_codeBoxes == null)
            return;

        var box = _codeBoxes[index];

        if (box == null)
            return;

        if (!string.IsNullOrEmpty(newValue))
        {
            // Keep only the last character entered
            string digit = newValue[newValue.Length - 1].ToString();

            if (!char.IsDigit(digit[0]))
            {
                box.SetTextWithoutNotify(string.Empty);
                return;
            }

            box.SetTextWithoutNotify(digit);

            // Advance to next box automatically
            if (index < _codeBoxes.Length - 1)
                _codeBoxes[index + 1]?.Select();
            else
                confirmButton?.Select();
        }
        else
        {
            // FIX: Jump back to the previous box when the current one is cleared
            // (mirrors the UI Toolkit OnCodeBoxKeyDown backspace behaviour).
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
            ShowError("Session expired. Please sign up again.");
            await Task.Delay(2000);
            if (this == null) return; // Guard: object may be destroyed after await
            GoBackToSignUp();
            return;
        }

        await AuthService.Instance.ConfirmEmail(email, GetFullCode());
    }

    private async void OnResendCodeClicked()
    {
        if (_isResendOnCooldown)
            return;

        ClearFeedback();

        string email = AuthService.Instance.GetPendingEmail();

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Session expired. Please sign up again.");
            await Task.Delay(2000);
            if (this == null) return;
            GoBackToSignUp();
            return;
        }

        await AuthService.Instance.ResendConfirmationCode(email);
    }

    private void OnBackToSignUpClicked()
    {
        GoBackToSignUp();
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnConfirmEmailSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(1500);
        if (this == null) return;
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private async void OnConfirmEmailFailed(string message, AuthError error)
    {
        ShowError(message);

        if (error == AuthError.ExpiredConfirmationCode)
        {
            await Task.Delay(2000);
            if (this == null) return;
            GoBackToSignUp();
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

        if (backToSignUpButton != null)
            backToSignUpButton.interactable = !isLoading;

        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box != null)
                box.interactable = !isLoading;
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

    // FIX: Cooldown uses a CancellationTokenSource so OnDisable can stop the loop
    // immediately instead of letting it run indefinitely on a disabled object.
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

    private void GoBackToSignUp()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.SignUp);
    }

    private void ClearInputs()
    {
        if (_codeBoxes == null)
            return;

        foreach (var box in _codeBoxes)
        {
            if (box != null)
                box.SetTextWithoutNotify(string.Empty);
        }
    }
}