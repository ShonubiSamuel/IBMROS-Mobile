using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("Buttons")]
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button goToLoginButton;
    [SerializeField] private Button passwordToggleButton;
    [SerializeField] private Button confirmPasswordToggleButton;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Password Strength")]
    [SerializeField] private Slider strengthSlider;
    [SerializeField] private TextMeshProUGUI strengthLabel;
    [SerializeField] private TextMeshProUGUI req1Text;
    [SerializeField] private TextMeshProUGUI req2Text;
    [SerializeField] private TextMeshProUGUI req3Text;
    [SerializeField] private TextMeshProUGUI req4Text;
    [SerializeField] private Image req1Icon;
    [SerializeField] private Image req2Icon;
    [SerializeField] private Image req3Icon;
    [SerializeField] private Image req4Icon;

    [Header("Requirement Colors")]
    [SerializeField] private Color metColor = Color.green;
    [SerializeField] private Color unmetColor = Color.gray;

    // State
    private bool _passwordVisible = false;
    private bool _confirmPasswordVisible = false;

    void Start()
    {
        AuthService.OnSignUpSuccess += OnSignUpSuccess;
        AuthService.OnSignUpFailed += OnSignUpFailed;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnSignUpSuccess -= OnSignUpSuccess;
        AuthService.OnSignUpFailed -= OnSignUpFailed;
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
    }

    private void WireEvents()
    {
        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(OnGoToLoginClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.AddListener(OnPasswordToggleClicked);

        if (confirmPasswordToggleButton != null)
            confirmPasswordToggleButton.onClick.AddListener(OnConfirmPasswordToggleClicked);

        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(UpdatePasswordStrength);
    }

    private void UnwireEvents()
    {
        if (signUpButton != null)
            signUpButton.onClick.RemoveListener(OnSignUpClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.RemoveListener(OnGoToLoginClicked);

        if (passwordToggleButton != null)
            passwordToggleButton.onClick.RemoveListener(OnPasswordToggleClicked);

        if (confirmPasswordToggleButton != null)
            confirmPasswordToggleButton.onClick.RemoveListener(OnConfirmPasswordToggleClicked);

        if (passwordInput != null)
            passwordInput.onValueChanged.RemoveListener(UpdatePasswordStrength);
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.SignUp)
            return;

        // OnEnable already ran OnScreenActivated. Only refresh feedback here.
        ClearFeedback();
    }

    // BUTTON HANDLERS

    private async void OnSignUpClicked()
    {
        ClearFeedback();
        await AuthService.Instance.SignUp(
            emailInput?.text.Trim(),
            passwordInput?.text,
            confirmPasswordInput?.text
        );
    }

    private void OnGoToLoginClicked()
    {
        ClearFeedback();
        ClearInputs();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
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

    private void OnConfirmPasswordToggleClicked()
    {
        _confirmPasswordVisible = !_confirmPasswordVisible;

        if (confirmPasswordInput != null)
        {
            confirmPasswordInput.contentType = _confirmPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            confirmPasswordInput.ForceLabelUpdate();
        }
    }

    // AUTHSERVICE EVENT HANDLERS

    private async void OnSignUpSuccess(string message)
    {
        ShowSuccess(message);
        await Task.Delay(1500);
        if (this == null) return; // Guard: object may be destroyed after await
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.ConfirmEmail);
    }

    private async void OnSignUpFailed(string message, AuthError error)
    {
        ShowError(message);

        if (error == AuthError.EmailAlreadyExists)
        {
            await Task.Delay(2000);
            if (this == null) return;
            OnGoToLoginClicked();
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

        if (signUpButton != null)
            signUpButton.interactable = !isLoading;

        if (emailInput != null)
            emailInput.interactable = !isLoading;

        if (passwordInput != null)
            passwordInput.interactable = !isLoading;

        if (confirmPasswordInput != null)
            confirmPasswordInput.interactable = !isLoading;
    }

    // PASSWORD STRENGTH

    private void UpdatePasswordStrength(string password)
    {
        bool hasLength = password.Length >= 8;
        bool hasUpper = System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]");
        bool hasDigit = System.Text.RegularExpressions.Regex.IsMatch(password, @"\d");
        bool hasSpecial = System.Text.RegularExpressions.Regex
            .IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]");

        SetRequirementMet(req1Icon, req1Text, hasLength);
        SetRequirementMet(req2Icon, req2Text, hasUpper);
        SetRequirementMet(req3Icon, req3Text, hasDigit);
        SetRequirementMet(req4Icon, req4Text, hasSpecial);

        int score = 0;
        if (hasLength) score++;
        if (hasUpper) score++;
        if (hasDigit) score++;
        if (hasSpecial) score++;

        UpdateStrengthBar(score);
    }

    private void SetRequirementMet(Image icon, TextMeshProUGUI text, bool met)
    {
        if (icon != null)
            icon.color = met ? metColor : unmetColor;

        if (text != null)
            text.color = met ? metColor : unmetColor;
    }

    private void UpdateStrengthBar(int score)
    {
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

    private void ClearInputs()
    {
        if (emailInput != null) emailInput.text = string.Empty;
        if (passwordInput != null) passwordInput.text = string.Empty;
        if (confirmPasswordInput != null) confirmPasswordInput.text = string.Empty;

        if (strengthSlider != null) strengthSlider.value = 0;
        if (strengthLabel != null) strengthLabel.text = string.Empty;

        SetRequirementMet(req1Icon, req1Text, false);
        SetRequirementMet(req2Icon, req2Text, false);
        SetRequirementMet(req3Icon, req3Text, false);
        SetRequirementMet(req4Icon, req4Text, false);
    }
}