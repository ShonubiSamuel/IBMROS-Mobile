using System;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // Current user data
    public string Email { get; private set; }
    public string IdToken { get; private set; }
    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime TokenExpiryTime { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(IdToken);

    // PlayerPrefs keys for persisting session across app restarts
    private const string KeyEmail = "ibm_ros_email";
    private const string KeyIdToken = "ibm_ros_id_token";
    private const string KeyAccessToken = "ibm_ros_access_token";
    private const string KeyRefreshToken = "ibm_ros_refresh_token";
    private const string KeyTokenExpiry = "ibm_ros_token_expiry";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Try to restore a previous session on app start
        LoadPersistedSession();
    }

    // Saves all tokens after successful login
    public void SaveSession(string email, string idToken, string accessToken, string refreshToken, int expiresInSeconds)
    {
        Email = email;
        IdToken = idToken;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);

        // Persist to device storage so session survives app restarts
        PlayerPrefs.SetString(KeyEmail, email);
        PlayerPrefs.SetString(KeyIdToken, idToken);
        PlayerPrefs.SetString(KeyAccessToken, accessToken);
        PlayerPrefs.SetString(KeyRefreshToken, refreshToken);
        PlayerPrefs.SetString(KeyTokenExpiry, TokenExpiryTime.ToString("o"));
        PlayerPrefs.Save();

        Debug.Log("[SessionManager] Session saved.");
    }

    // Updates only the access and id tokens after a refresh
    // Refresh token stays the same
    public void UpdateTokens(string newIdToken, string newAccessToken, int expiresInSeconds)
    {
        IdToken = newIdToken;
        AccessToken = newAccessToken;
        TokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);

        PlayerPrefs.SetString(KeyIdToken, newIdToken);
        PlayerPrefs.SetString(KeyAccessToken, newAccessToken);
        PlayerPrefs.SetString(KeyTokenExpiry, TokenExpiryTime.ToString("o"));
        PlayerPrefs.Save();

        Debug.Log("[SessionManager] Tokens updated.");
    }

    // Clears all session data on logout
    public void ClearSession()
    {
        Email = null;
        IdToken = null;
        AccessToken = null;
        RefreshToken = null;
        TokenExpiryTime = DateTime.MinValue;

        PlayerPrefs.DeleteKey(KeyEmail);
        PlayerPrefs.DeleteKey(KeyIdToken);
        PlayerPrefs.DeleteKey(KeyAccessToken);
        PlayerPrefs.DeleteKey(KeyRefreshToken);
        PlayerPrefs.DeleteKey(KeyTokenExpiry);
        PlayerPrefs.Save();

        Debug.Log("[SessionManager] Session cleared.");
    }

    // Checks if the current token is close to expiring
    // Uses the buffer from AwsConfig to refresh early
    public bool IsTokenNearExpiry()
    {
        if (TokenExpiryTime == DateTime.MinValue)
            return false;

        var timeUntilExpiry = TokenExpiryTime - DateTime.UtcNow;
        return timeUntilExpiry.TotalMinutes <= AwsConfig.TokenRefreshBufferMinutes;
    }

    // Checks if the token has fully expired
    public bool IsTokenExpired()
    {
        if (TokenExpiryTime == DateTime.MinValue)
            return true;

        return DateTime.UtcNow >= TokenExpiryTime;
    }

    // Restores a previous session from device storage on app start
    private void LoadPersistedSession()
    {
        string savedEmail = PlayerPrefs.GetString(KeyEmail, null);
        string savedIdToken = PlayerPrefs.GetString(KeyIdToken, null);
        string savedAccessToken = PlayerPrefs.GetString(KeyAccessToken, null);
        string savedRefreshToken = PlayerPrefs.GetString(KeyRefreshToken, null);
        string savedExpiry = PlayerPrefs.GetString(KeyTokenExpiry, null);

        if (string.IsNullOrEmpty(savedIdToken))
        {
            Debug.Log("[SessionManager] No saved session found.");
            return;
        }

        Email = savedEmail;
        IdToken = savedIdToken;
        AccessToken = savedAccessToken;
        RefreshToken = savedRefreshToken;

        if (DateTime.TryParse(savedExpiry, out DateTime parsedExpiry))
            TokenExpiryTime = parsedExpiry;

        Debug.Log($"[SessionManager] Session restored for {Email}.");
    }
}