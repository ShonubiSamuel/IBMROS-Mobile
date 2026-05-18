using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainAppController : MonoBehaviour, IAuthUI
{
    // Header elements
    private Label _greetingText;
    private Label _userNameText;
    private Button _notificationButton;
    private Button _profileButton;
    private VisualElement _profileAvatar;
    private Label _profileInitials;

    // No connection banner
    private VisualElement _noConnectionBanner;
    private Button _retryConnectionButton;

    // Profile panel
    private VisualElement _profilePanel;
    private VisualElement _profilePanelBackdrop;
    private Label _profilePanelInitials;
    private Label _profilePanelName;
    private Label _profilePanelEmail;
    private Button _logoutButton;
    private Button _deleteAccountButton;
    private Button _profileSettingsButton;
    private Button _profileWishlistButton;
    private Button _profileOrdersButton;

    // Bottom navigation
    private Button _navHomeButton;
    private Button _navSearchButton;
    private Button _navArButton;
    private Button _navRoomsButton;
    private Button _navProfileButton;

    // AR banner
    private Button _launchArButton;

    // Rooms section
    private Button _addRoomButton;
    private Button _seeAllRoomsButton;

    // Furniture section
    private Button _seeAllFurnitureButton;
    private Button _categoryAll;
    private Button _categorySofa;
    private Button _categoryChair;
    private Button _categoryTable;
    private Button _categoryBed;
    private Button _categoryStorage;
    private Button _categoryLighting;

    // Loading overlay
    private VisualElement _loadingOverlay;
    private Label _loadingText;

    // State
    private bool _profilePanelOpen = false;
    private CancellationTokenSource _feedbackCts;

    void Start()
    {
        Debug.Log("[MainAppController] Start called.");
        AuthService.OnLogoutSuccess += OnLogoutSuccess;
        AuthService.OnDeleteAccountSuccess += OnDeleteAccountSuccess;
        AuthService.OnSessionExpiredOrInvalid += OnSessionExpiredOrInvalid;
        AuthService.OnNetworkLost += OnNetworkLost;
        AuthService.OnNetworkRestored += OnNetworkRestored;
        AuthService.OnLoadingChanged += OnLoadingStateChanged;
        ScreenNavigator.OnScreenChanged += OnScreenChanged;
    }

    void OnDestroy()
    {
        AuthService.OnLogoutSuccess -= OnLogoutSuccess;
        AuthService.OnDeleteAccountSuccess -= OnDeleteAccountSuccess;
        AuthService.OnSessionExpiredOrInvalid -= OnSessionExpiredOrInvalid;
        AuthService.OnNetworkLost -= OnNetworkLost;
        AuthService.OnNetworkRestored -= OnNetworkRestored;
        AuthService.OnLoadingChanged -= OnLoadingStateChanged;
        ScreenNavigator.OnScreenChanged -= OnScreenChanged;
        UIManager.OnScreensReady -= OnScreensReady;
        _feedbackCts?.Cancel();
        _feedbackCts?.Dispose();
    }

    void OnEnable()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsReady)
        {
            QueryElements();
            WireEvents();
            PopulateUserInfo();

            if (SessionRefreshService.Instance != null)
                SessionRefreshService.Instance.Begin();
        }
        else
        {
            UIManager.OnScreensReady += OnScreensReady;
        }
    }

    void OnDisable()
    {
        UIManager.OnScreensReady -= OnScreensReady;

        if (SessionRefreshService.Instance != null)
            SessionRefreshService.Instance.Stop();

        CloseProfilePanel();
    }

    private void OnScreensReady()
    {
        UIManager.OnScreensReady -= OnScreensReady;
        QueryElements();
        WireEvents();
        PopulateUserInfo();

        if (SessionRefreshService.Instance != null)
            SessionRefreshService.Instance.Begin();
    }

    // IAuthUI implementation
    public void OnScreenActivated()
    {
        PopulateUserInfo();
    }

    private void QueryElements()
    {
        var container = UIManager.Instance.GetScreenContainer(ScreenName.MainApp);

        if (container == null)
        {
            Debug.LogError("[MainAppController] MainApp container not found.");
            return;
        }

        Debug.Log("[MainAppController] QueryElements called. Container found.");

        _greetingText = container.Q<Label>("GreetingText");
        _userNameText = container.Q<Label>("UserNameText");
        _notificationButton = container.Q<Button>("NotificationButton");
        _profileButton = container.Q<Button>("ProfileButton");
        _profileAvatar = container.Q<VisualElement>("ProfileAvatar");
        _profileInitials = container.Q<Label>("ProfileInitials");
        _noConnectionBanner = container.Q<VisualElement>("NoConnectionBanner");
        _retryConnectionButton = container.Q<Button>("RetryConnectionButton");
        _profilePanel = container.Q<VisualElement>("ProfilePanel");
        _profilePanelBackdrop = container.Q<VisualElement>("ProfilePanelBackdrop");
        _profilePanelInitials = container.Q<Label>("ProfilePanelInitials");
        _profilePanelName = container.Q<Label>("ProfilePanelName");
        _profilePanelEmail = container.Q<Label>("ProfilePanelEmail");
        _logoutButton = container.Q<Button>("LogoutButton");
        _deleteAccountButton = container.Q<Button>("DeleteAccountButton");
        _profileSettingsButton = container.Q<Button>("ProfileSettingsButton");
        _profileWishlistButton = container.Q<Button>("ProfileWishlistButton");
        _profileOrdersButton = container.Q<Button>("ProfileOrdersButton");
        _navHomeButton = container.Q<Button>("NavHomeButton");
        _navSearchButton = container.Q<Button>("NavSearchButton");
        _navArButton = container.Q<Button>("NavArButton");
        _navRoomsButton = container.Q<Button>("NavRoomsButton");
        _navProfileButton = container.Q<Button>("NavProfileButton");
        _launchArButton = container.Q<Button>("LaunchArButton");
        _addRoomButton = container.Q<Button>("AddRoomButton");
        _seeAllRoomsButton = container.Q<Button>("SeeAllRoomsButton");
        _seeAllFurnitureButton = container.Q<Button>("SeeAllFurnitureButton");
        _categoryAll = container.Q<Button>("CategoryAll");
        _categorySofa = container.Q<Button>("CategorySofa");
        _categoryChair = container.Q<Button>("CategoryChair");
        _categoryTable = container.Q<Button>("CategoryTable");
        _categoryBed = container.Q<Button>("CategoryBed");
        _categoryStorage = container.Q<Button>("CategoryStorage");
        _categoryLighting = container.Q<Button>("CategoryLighting");
        _loadingOverlay = container.Q<VisualElement>("LoadingOverlay");
        _loadingText = container.Q<Label>("LoadingText");

        Debug.Log($"[MainAppController] LogoutButton found: {_logoutButton != null}");
        Debug.Log($"[MainAppController] ProfileButton found: {_profileButton != null}");
        Debug.Log($"[MainAppController] ProfilePanel found: {_profilePanel != null}");
    }

    private void WireEvents()
    {
        Debug.Log("[MainAppController] WireEvents called.");

        if (_profileButton != null)
            _profileButton.clicked += OnProfileButtonClicked;

        if (_profilePanelBackdrop != null)
            _profilePanelBackdrop.RegisterCallback<ClickEvent>(evt => OnBackdropClicked());

        if (_logoutButton != null)
        {
            Debug.Log("[MainAppController] Logout button found and wired.");
            _logoutButton.clicked += OnLogoutClicked;
        }
        else
            Debug.LogError("[MainAppController] _logoutButton is null.");

        if (_deleteAccountButton != null)
            _deleteAccountButton.clicked += OnDeleteAccountClicked;

        if (_profileSettingsButton != null)
            _profileSettingsButton.clicked += OnSettingsClicked;

        if (_profileWishlistButton != null)
            _profileWishlistButton.clicked += OnWishlistClicked;

        if (_profileOrdersButton != null)
            _profileOrdersButton.clicked += OnOrdersClicked;

        if (_retryConnectionButton != null)
            _retryConnectionButton.clicked += OnRetryConnectionClicked;

        if (_navHomeButton != null)
            _navHomeButton.clicked += () => SetActiveNavTab(_navHomeButton);

        if (_navSearchButton != null)
            _navSearchButton.clicked += () => SetActiveNavTab(_navSearchButton);

        if (_navArButton != null)
            _navArButton.clicked += OnLaunchArClicked;

        if (_navRoomsButton != null)
            _navRoomsButton.clicked += () => SetActiveNavTab(_navRoomsButton);

        if (_navProfileButton != null)
            _navProfileButton.clicked += OnProfileButtonClicked;

        if (_launchArButton != null)
            _launchArButton.clicked += OnLaunchArClicked;
        
        if (_addRoomButton != null)
            _addRoomButton.clicked += OnAddRoomClicked;

        WireCategoryChip(_categoryAll);
        WireCategoryChip(_categorySofa);
        WireCategoryChip(_categoryChair);
        WireCategoryChip(_categoryTable);
        WireCategoryChip(_categoryBed);
        WireCategoryChip(_categoryStorage);
        WireCategoryChip(_categoryLighting);
    }

    private void WireCategoryChip(Button chip)
    {
        if (chip == null)
            return;

        chip.clicked += () => SetActiveCategory(chip);
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

        if (_greetingText != null)
            _greetingText.text = greeting;

        if (_userNameText != null)
            _userNameText.text = displayName;

        if (_profileInitials != null)
            _profileInitials.text = initials;

        if (_profilePanelInitials != null)
            _profilePanelInitials.text = initials;

        if (_profilePanelName != null)
            _profilePanelName.text = displayName;

        if (_profilePanelEmail != null)
            _profilePanelEmail.text = email;
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
        _profilePanel?.AddToClassList("main-profile-panel--visible");
        _profilePanelBackdrop?.AddToClassList("main-profile-backdrop--visible");
        _profilePanelOpen = true;
    }

    private void CloseProfilePanel()
    {
        _profilePanel?.RemoveFromClassList("main-profile-panel--visible");
        _profilePanelBackdrop?.RemoveFromClassList("main-profile-backdrop--visible");
        _profilePanelOpen = false;
    }

    private void OnBackdropClicked()
    {
        CloseProfilePanel();
    }

    // BOTTOM NAV

    private void SetActiveNavTab(Button activeTab)
    {
        Button[] tabs = new Button[]
        {
            _navHomeButton,
            _navSearchButton,
            _navRoomsButton,
            _navProfileButton
        };

        foreach (var tab in tabs)
        {
            if (tab == null)
                continue;

            if (tab == activeTab)
                tab.AddToClassList("main-nav-item--active");
            else
                tab.RemoveFromClassList("main-nav-item--active");
        }
    }

    // CATEGORY CHIPS

    private void SetActiveCategory(Button activeChip)
    {
        Button[] chips = new Button[]
        {
            _categoryAll,
            _categorySofa,
            _categoryChair,
            _categoryTable,
            _categoryBed,
            _categoryStorage,
            _categoryLighting
        };

        foreach (var chip in chips)
        {
            if (chip == null)
                continue;

            if (chip == activeChip)
                chip.AddToClassList("main-category-chip--active");
            else
                chip.RemoveFromClassList("main-category-chip--active");
        }
    }

    // AR

    private void OnLaunchArClicked()
    {
        Debug.Log("[MainAppController] AR launched.");
    }
    
    private void OnAddRoomClicked()
    {
        Debug.Log("[MainAppController] Loading Room scene.");
        SceneManager.LoadScene("Room");
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
        ScreenNavigator.Instance.NavigateTo(ScreenName.DeleteAccount);
    }

    private void OnSettingsClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController] Settings tapped. Coming soon.");
    }

    private void OnWishlistClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController] Wishlist tapped. Coming soon.");
    }

    private void OnOrdersClicked()
    {
        CloseProfilePanel();
        Debug.Log("[MainAppController] Orders tapped. Coming soon.");
    }

    private void OnRetryConnectionClicked()
    {
        bool isConnected = Application.internetReachability
            != NetworkReachability.NotReachable;

        if (!isConnected)
            Debug.Log("[MainAppController] Still no connection.");
    }

    // AUTHSERVICE EVENT HANDLERS

    private void OnLogoutSuccess(string message)
    {
        Debug.Log("[MainAppController] Logout successful. Navigating to Login.");
        ScreenNavigator.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnDeleteAccountSuccess(string message)
    {
        Debug.Log("[MainAppController] Account deleted. Navigating to Login.");
        ScreenNavigator.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnSessionExpiredOrInvalid()
    {
        Debug.Log("[MainAppController] Session expired. Navigating to Login.");
        ScreenNavigator.Instance.NavigateTo(ScreenName.Login);
    }

    private void OnNetworkLost()
    {
        UIManager.Instance.ShowNoConnectionBanner();
    }

    private void OnNetworkRestored()
    {
        UIManager.Instance.HideNoConnectionBanner();
    }

    private void OnLoadingStateChanged(bool isLoading, string message)
    {
        SetLoadingState(isLoading, message);
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
        Debug.LogWarning($"[MainAppController] Error: {message}");
    }

    public void ShowSuccess(string message)
    {
        Debug.Log($"[MainAppController] Success: {message}");
    }

    public void ClearFeedback()
    {
        _feedbackCts?.Cancel();
        _feedbackCts = null;
    }

    public void SetLoadingState(bool isLoading, string message = "Please wait...")
    {
        if (_loadingText != null)
            _loadingText.text = message;

        if (isLoading)
            _loadingOverlay?.AddToClassList("loading-overlay--visible");
        else
            _loadingOverlay?.RemoveFromClassList("loading-overlay--visible");
    }

    // IAuthUI requires this signature
    public void SetLoadingState(bool isLoading)
    {
        SetLoadingState(isLoading, "Please wait...");
    }
}