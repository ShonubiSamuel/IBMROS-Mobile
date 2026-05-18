using UnityEngine;
using UnityEngine.UIElements;

public class LoadingHandler : MonoBehaviour
{
    public static LoadingHandler Instance { get; private set; }

    private int _activeLoadCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Show(LoadingMessage message = LoadingMessage.Default)
    {
        _activeLoadCount++;
        string text = GetMessageText(message);
        UIManager.Instance.ShowGlobalLoading(text);
    }

    public void ShowWithMessage(string customMessage)
    {
        _activeLoadCount++;
        UIManager.Instance.ShowGlobalLoading(customMessage);
    }

    public void Hide()
    {
        if (_activeLoadCount > 0)
            _activeLoadCount--;

        if (_activeLoadCount == 0)
            UIManager.Instance.HideGlobalLoading();
    }

    public void HideImmediately()
    {
        _activeLoadCount = 0;
        UIManager.Instance.HideGlobalLoading();
    }

    public bool IsLoading => _activeLoadCount > 0;

    private string GetMessageText(LoadingMessage message)
    {
        switch (message)
        {
            case LoadingMessage.SigningUp:          return "Creating your account...";
            case LoadingMessage.LoggingIn:          return "Signing in...";
            case LoadingMessage.LoggingOut:         return "Signing out...";
            case LoadingMessage.SendingCode:        return "Sending verification code...";
            case LoadingMessage.VerifyingCode:      return "Verifying code...";
            case LoadingMessage.ResettingPassword:  return "Resetting password...";
            case LoadingMessage.RefreshingSession:  return "Refreshing session...";
            case LoadingMessage.LoadingData:        return "Loading...";
            case LoadingMessage.SavingData:         return "Saving...";
            case LoadingMessage.UploadingFile:      return "Uploading...";
            case LoadingMessage.DownloadingFile:    return "Downloading...";
            default:                                return "Please wait...";
        }
    }
}

// All loading message types used across the app
public enum LoadingMessage
{
    Default,
    SigningUp,
    LoggingIn,
    LoggingOut,
    SendingCode,
    VerifyingCode,
    ResettingPassword,
    RefreshingSession,
    LoadingData,
    SavingData,
    UploadingFile,
    DownloadingFile
}