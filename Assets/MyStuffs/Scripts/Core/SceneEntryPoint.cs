using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEntryPoint : MonoBehaviour
{
    void OnEnable()
    {
        if (!SceneTransition.SkipSplash)
            return;

        Debug.Log("[SceneEntryPoint] Skip splash detected.");

        if (UIManager.Instance != null && UIManager.Instance.IsReady)
        {
            SceneTransition.SetSkipSplash(false);
            NavigateToMainApp();
        }
        else
            UIManager.OnScreensReady += OnScreensReady;
    }

    private void OnScreensReady()
    {
        UIManager.OnScreensReady -= OnScreensReady;
        SceneTransition.SetSkipSplash(false);
        Debug.Log("[SceneEntryPoint] UIManager ready. Navigating to MainApp.");
        NavigateToMainApp();
    }

    void OnDisable()
    {
        UIManager.OnScreensReady -= OnScreensReady;
    }
    

    private void NavigateToMainApp()
    {
        if (ScreenNavigator.Instance == null)
        {
            Debug.LogError("[SceneEntryPoint] ScreenNavigator.Instance is null.");
            return;
        }

        ScreenNavigator.Instance.NavigateToImmediate(ScreenName.MainApp);
    }
}