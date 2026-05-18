using System;

public class UserModel
{
    // Core identity fields from Cognito
    public string UserId { get; set; }
    public string Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsGuest { get; set; }

    // Account status
    public bool IsLoggedIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }

    // Session tokens
    public string IdToken { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime TokenExpiryTime { get; set; }

    // App specific data
    public string DisplayName { get; set; }
    public string ProfilePictureUrl { get; set; }

    // Default constructor creates a guest user
    public UserModel()
    {
        IsGuest = true;
        IsLoggedIn = false;
        IsEmailVerified = false;
        CreatedAt = DateTime.UtcNow;
    }

    // Constructor for authenticated users
    public UserModel(string userId, string email, string idToken, string accessToken, string refreshToken, int expiresInSeconds)
    {
        UserId = userId;
        Email = email;
        IdToken = idToken;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        IsGuest = false;
        IsLoggedIn = true;
        IsEmailVerified = true;
        CreatedAt = DateTime.UtcNow;
        LastLoginAt = DateTime.UtcNow;
    }

    // Checks if the token has fully expired
    public bool IsTokenExpired()
    {
        if (TokenExpiryTime == DateTime.MinValue)
            return true;

        return DateTime.UtcNow >= TokenExpiryTime;
    }

    // Checks if the token is close to expiring
    public bool IsTokenNearExpiry()
    {
        if (TokenExpiryTime == DateTime.MinValue)
            return false;

        var timeUntilExpiry = TokenExpiryTime - DateTime.UtcNow;
        return timeUntilExpiry.TotalMinutes <= AwsConfig.TokenRefreshBufferMinutes;
    }

    // Updates tokens after a session refresh
    public void UpdateTokens(string newIdToken, string newAccessToken, int expiresInSeconds)
    {
        IdToken = newIdToken;
        AccessToken = newAccessToken;
        TokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        LastLoginAt = DateTime.UtcNow;
    }

    // Resets user back to guest state on logout
    public void ClearToGuest()
    {
        UserId = null;
        Email = null;
        IdToken = null;
        AccessToken = null;
        RefreshToken = null;
        TokenExpiryTime = DateTime.MinValue;
        IsGuest = true;
        IsLoggedIn = false;
        IsEmailVerified = false;
        DisplayName = null;
        ProfilePictureUrl = null;
    }

    public override string ToString()
    {
        return $"User[Email={Email}, IsGuest={IsGuest}, IsLoggedIn={IsLoggedIn}, TokenExpiry={TokenExpiryTime}]";
    }
}