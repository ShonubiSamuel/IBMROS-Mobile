using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenNavigator_UGUI : MonoBehaviour, INavigator
{
    public static ScreenNavigator_UGUI Instance { get; private set; }

    public static event Action<ScreenName> OnScreenChanged;

    public ScreenName CurrentScreen { get; private set; } = ScreenName.Splash;

    [Header("Panels")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signUpPanel;
    [SerializeField] private GameObject confirmEmailPanel;
    [SerializeField] private GameObject forgotPasswordPanel;
    [SerializeField] private GameObject confirmPasswordPanel;
    [SerializeField] private GameObject mainAppPanel;
    [SerializeField] private GameObject deleteAccountPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // FIX: Subscribe to sceneLoaded so we can re-acquire panel references
        // after returning from a sub-scene (e.g. "Room"). DontDestroyOnLoad keeps
        // this navigator alive, but the serialized panel references belong to the
        // original auth scene and become null/destroyed after a scene reload.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        ShowOnlyPanel(splashPanel);
    }

    // FIX: When the auth scene is reloaded, find the new panel instances by name
    // so navigation continues to work correctly.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only re-acquire panels when returning to the auth/main scene.
        // Adjust the scene name to match your actual auth scene name.
        if (scene.name != "AuthScene" && scene.name != gameObject.scene.name)
            return;

        Debug.Log("[ScreenNavigator_UGUI] Auth scene reloaded — re-acquiring panel references.");

        splashPanel         = FindPanelByName("SplashPanel");
        loginPanel          = FindPanelByName("LoginPanel");
        signUpPanel         = FindPanelByName("SignUpPanel");
        confirmEmailPanel   = FindPanelByName("ConfirmEmailPanel");
        forgotPasswordPanel = FindPanelByName("ForgotPasswordPanel");
        confirmPasswordPanel= FindPanelByName("ConfirmPasswordPanel");
        mainAppPanel        = FindPanelByName("MainAppPanel");
        deleteAccountPanel  = FindPanelByName("DeleteAccountPanel");

        // Reset so the navigator reflects the fresh scene state
        CurrentScreen = ScreenName.Splash;
        ShowOnlyPanel(splashPanel);
    }

    private GameObject FindPanelByName(string panelName)
    {
        var found = GameObject.Find(panelName);
        if (found == null)
            Debug.LogWarning($"[ScreenNavigator_UGUI] Panel not found in scene: {panelName}");
        return found;
    }

    public void NavigateTo(ScreenName screen)
    {
        if (screen == CurrentScreen)
            return;

        GameObject target = GetPanel(screen);

        if (target == null)
        {
            Debug.LogError($"[ScreenNavigator_UGUI] Panel for {screen} is null. " +
                           "Check Inspector assignments or re-acquisition after scene reload.");
            return;
        }

        ShowOnlyPanel(target);
        CurrentScreen = screen;
        OnScreenChanged?.Invoke(CurrentScreen);
        Debug.Log($"[ScreenNavigator_UGUI] Navigated to {screen}");
    }

    public void NavigateToImmediate(ScreenName screen)
    {
        NavigateTo(screen);
    }

    private void ShowOnlyPanel(GameObject target)
    {
        if (target == null)
            return;

        GameObject[] allPanels = new GameObject[]
        {
            splashPanel,
            loginPanel,
            signUpPanel,
            confirmEmailPanel,
            forgotPasswordPanel,
            confirmPasswordPanel,
            mainAppPanel,
            deleteAccountPanel
        };

        foreach (var panel in allPanels)
        {
            if (panel == null)
                continue;

            panel.SetActive(panel == target);
        }
    }

    private GameObject GetPanel(ScreenName screen)
    {
        switch (screen)
        {
            case ScreenName.Splash:          return splashPanel;
            case ScreenName.Login:           return loginPanel;
            case ScreenName.SignUp:          return signUpPanel;
            case ScreenName.ConfirmEmail:    return confirmEmailPanel;
            case ScreenName.ForgotPassword:  return forgotPasswordPanel;
            case ScreenName.ConfirmPassword: return confirmPasswordPanel;
            case ScreenName.MainApp:         return mainAppPanel;
            case ScreenName.DeleteAccount:   return deleteAccountPanel;
            default:                         return null;
        }
    }
}