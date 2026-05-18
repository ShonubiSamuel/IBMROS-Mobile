using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainAppController_UGUI : MonoBehaviour, IAuthUI
{
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI greetingText;
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private Button notificationButton;
    [SerializeField] private Button profileButton;
    [SerializeField] private TextMeshProUGUI profileInitials;

    [Header("Profile Panel")]
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private Button profilePanelBackdrop;
    [SerializeField] private TextMeshProUGUI profilePanelInitials;
    [SerializeField] private TextMeshProUGUI profilePanelName;
    [SerializeField] private TextMeshProUGUI profilePanelEmail;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private Button profileSettingsButton;
    [SerializeField] private Button profileWishlistButton;
    [SerializeField] private Button profileOrdersButton;
    
    [Header("Rooms")]
    [SerializeField] private Button addRoomButton;

    [Header("No Connection Banner")]
    [SerializeField] private GameObject noConnectionBanner;
    [SerializeField] private Button retryConnectionButton;
    
    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TextMeshProUGUI loadingText;

    // State
    private bool _profilePanelOpen = false;

    void Start()
    {
        AuthService.OnLogoutSuccess += OnLogoutSuccess;
        AuthService.OnDeleteAccountSuccess += OnDeleteAccountSuccess;
        AuthService.OnSessionExpiredOrInvalid += OnSessionExpiredOrInvalid;
        AuthService.OnNetworkLost += OnNetworkLost;
        AuthService.OnNetworkRestored += OnNetworkRestored;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnLogoutSuccess -= OnLogoutSuccess;
        AuthService.OnDeleteAccountSuccess -= OnDeleteAccountSuccess;
        AuthService.OnSessionExpiredOrInvalid -= OnSessionExpiredOrInvalid;
        AuthService.OnNetworkLost -= OnNetworkLost;
        AuthService.OnNetworkRestored -= OnNetworkRestored;
        AuthService.OnLoadingChanged -= OnLoadingStateChanged;
        ScreenNavigator_UGUI.OnScreenChanged -= OnScreenChanged;
    }

    void OnEnable()
    {
        WireEvents();
        OnScreenActivated();
    }

    void OnDisable()
    {
        UnwireEvents();
        CloseProfilePanel();
    }

    // IAuthUI implementation
    public void OnScreenActivated()
    {
        PopulateUserInfo();
    }

    private void WireEvents()
    {
        if (profileButton != null)
            profileButton.onClick.AddListener(OnProfileButtonClicked);

        if (profilePanelBackdrop != null)
            profilePanelBackdrop.onClick.AddListener(CloseProfilePanel);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);

        if (deleteAccountButton != null)
            deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);

        if (profileSettingsButton != null)
            profileSettingsButton.onClick.AddListener(OnSettingsClicked);

        if (profileWishlistButton != null)
            profileWishlistButton.onClick.AddListener(OnWishlistClicked);

        if (profileOrdersButton != null)
            profileOrdersButton.onClick.AddListener(OnOrdersClicked);

        if (retryConnectionButton != null)
            retryConnectionButton.onClick.AddListener(OnRetryConnectionClicked);
        
        if (addRoomButton != null)
            addRoomButton.onClick.AddListener(OnAddRoomClicked);
        
    }

    private void UnwireEvents()
    {
        if (profileButton != null)
            profileButton.onClick.RemoveListener(OnProfileButtonClicked);

        if (profilePanelBackdrop != null)
            profilePanelBackdrop.onClick.RemoveListener(CloseProfilePanel);

        if (logoutButton != null)
            logoutButton.onClick.RemoveListener(OnLogoutClicked);

        if (deleteAccountButton != null)
            deleteAccountButton.onClick.RemoveListener(OnDeleteAccountClicked);

        if (profileSettingsButton != null)
            profileSettingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (profileWishlistButton != null)
            profileWishlistButton.onClick.RemoveListener(OnWishlistClicked);

        if (profileOrdersButton != null)
            profileOrdersButton.onClick.RemoveListener(OnOrdersClicked);

        if (retryConnectionButton != null)
            retryConnectionButton.onClick.RemoveListener(OnRetryConnectionClicked);
        
        if (addRoomButton != null)
            addRoomButton.onClick.RemoveListener(OnAddRoomClicked);
    }
    
    // POPULATE USER INFO

    private void PopulateUserInfo()
    {
        if (SessionManager.Instance == null)
            return;

        string email = SessionManager.Instance.Email ?? string.Empty;
        string initials = GetInitials(email);
        string greeting = GetTimeOfDayGreeting();
        string displayName = email.Contains("@")
            ? email.Split('@')[0]
            : email;

        if (greetingText != null)
            greetingText.text = greeting;

        if (userNameText != null)
            userNameText.text = displayName;

        if (profileInitials != null)
            profileInitials.text = initials;

        if (profilePanelInitials != null)
            profilePanelInitials.text = initials;

        if (profilePanelName != null)
            profilePanelName.text = displayName;

        if (profilePanelEmail != null)
            profilePanelEmail.text = email;
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

    private string GetTimeOfDayGreeting()
    {
        int hour = DateTime.Now.Hour;

        if (hour >= 5 && hour < 12)
            return "Good morning";
        if (hour >= 12 && hour < 17)
            return "Good afternoon";
        if (hour >= 17 && hour < 21)
            return "Good evening";

        return "Good night";
    }

    // PROFILE PANEL

    private void OnProfileButtonClicked()
    {
        if (_profilePanelOpen)
            CloseProfilePanel();
        else
            OpenProfilePanel();
    }

    private void OpenProfilePanel()
    {
        if (profilePanel != null)
            profilePanel.SetActive(true);

        if (profilePanelBackdrop != null)
            profilePanelBackdrop.gameObject.SetActive(true);

        _profilePanelOpen = true;
    }

    private void CloseProfilePanel()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);

        if (profilePanelBackdrop != null)
            profilePanelBackdrop.gameObject.SetActive(false);

        _profilePanelOpen = false;
    }
    
    // BUTTON HANDLERS

    private async void OnLogoutClicked()
    {
        CloseProfilePanel();
        await AuthService.Instance.Logout();
    }

    private void OnDeleteAccountClicked()
    {
        CloseProfilePanel();
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.DeleteAccount);
    }

    private void OnSettingsClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController_UGUI] Settings tapped. Coming soon.");
    }

    private void OnWishlistClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController_UGUI] Wishlist tapped. Coming soon.");
    }

    private void OnOrdersClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController_UGUI] Orders tapped. Coming soon.");
    }

    private void OnRetryConnectionClicked()
    {
        bool isConnected = Application.internetReachability
            != NetworkReachability.NotReachable;

        if (!isConnected)
            Debug.Log("[MainAppController_UGUI] Still no connection.");
    }
    
    private void OnAddRoomClicked()
    {
        Debug.Log("[MainAppController] Loading Room scene.");

        // Hide auth UI before leaving
        if (UIManager.Instance != null)
            UIManager.Instance.gameObject.SetActive(false);

        SceneManager.LoadScene("Room");
    }

    private void OnLaunchArClicked()
    {
        Debug.Log("[MainAppController_UGUI] AR launched.");
    }

    // AUTHSERVICE EVENT HANDLERS

    private void OnLogoutSuccess(string message)
    {
        Debug.Log("[MainAppController_UGUI] Logout successful. Navigating to Login.");
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnDeleteAccountSuccess(string message)
    {
        Debug.Log("[MainAppController_UGUI] Account deleted. Navigating to Login.");
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnSessionExpiredOrInvalid()
    {
        Debug.Log("[MainAppController_UGUI] Session expired. Navigating to Login.");
        ScreenNavigator_UGUI.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnNetworkLost()
    {
        if (noConnectionBanner != null)
            noConnectionBanner.SetActive(true);
    }

    private void OnNetworkRestored()
    {
        if (noConnectionBanner != null)
            noConnectionBanner.SetActive(false);
    }

    private void OnLoadingStateChanged(bool isLoading, string message)
    {
        SetLoadingState(isLoading);

        if (loadingText != null)
            loadingText.text = message;
    }

    private void OnScreenChanged(ScreenName screen)
    {
        if (screen != ScreenName.MainApp)
        {
            CloseProfilePanel();
            return;
        }

        PopulateUserInfo();
    }

    // IAUTHUI IMPLEMENTATION

    public void ShowError(string message)
    {
        Debug.LogWarning($"[MainAppController_UGUI] Error: {message}");
    }

    public void ShowSuccess(string message)
    {
        Debug.Log($"[MainAppController_UGUI] Success: {message}");
    }

    public void ClearFeedback() { }

    public void SetLoadingState(bool isLoading)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(isLoading);
    }
}