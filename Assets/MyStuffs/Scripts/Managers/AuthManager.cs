using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using UnityEngine;


public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

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

    // SIGN UP
    // Creates a new user account in the Cognito User Pool
    // Returns AuthResult with success or failure details
    public async Task<AuthResult> SignUp(string email, string password)
    {
        try
        {
            var request = new SignUpRequest
            {
                ClientId = AwsConfig.AppClientId,
                Username = email,
                Password = password,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = email }
                }
            };

            await AwsManager.Instance.CognitoProvider.SignUpAsync(request);

            Debug.Log($"[AuthManager] Sign up successful for {email}.");
            return AuthResult.Success("Sign up successful. Check your email for a verification code.");
        }
        catch (UsernameExistsException)
        {
            return AuthResult.Failure(AuthError.EmailAlreadyExists);
        }
        catch (InvalidPasswordException)
        {
            return AuthResult.Failure(AuthError.WeakPassword);
        }
        catch (InvalidParameterException e)
        {
            return AuthResult.Failure(AuthError.InvalidInput, e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Sign up error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // CONFIRM EMAIL
    // Verifies the confirmation code sent to the user email after signup
    public async Task<AuthResult> ConfirmEmail(string email, string confirmationCode)
    {
        try
        {
            var request = new ConfirmSignUpRequest
            {
                ClientId = AwsConfig.AppClientId,
                Username = email,
                ConfirmationCode = confirmationCode.Trim()
            };

            await AwsManager.Instance.CognitoProvider.ConfirmSignUpAsync(request);

            Debug.Log($"[AuthManager] Email confirmed for {email}.");
            return AuthResult.Success("Email verified. You can now log in.");
        }
        catch (CodeMismatchException)
        {
            return AuthResult.Failure(AuthError.InvalidConfirmationCode);
        }
        catch (ExpiredCodeException)
        {
            return AuthResult.Failure(AuthError.ExpiredConfirmationCode);
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Confirm email error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // RESEND CONFIRMATION CODE
    // Use this if the user did not receive or lost their verification email
    public async Task<AuthResult> ResendConfirmationCode(string email)
    {
        try
        {
            var request = new ResendConfirmationCodeRequest
            {
                ClientId = AwsConfig.AppClientId,
                Username = email
            };

            await AwsManager.Instance.CognitoProvider.ResendConfirmationCodeAsync(request);

            Debug.Log($"[AuthManager] Confirmation code resent to {email}.");
            return AuthResult.Success("Verification code resent. Check your email.");
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (LimitExceededException)
        {
            return AuthResult.Failure(AuthError.TooManyRequests);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Resend code error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // LOGIN
    // Authenticates the user and upgrades credentials from guest to authenticated
    public async Task<AuthResult> Login(string email, string password)
    {
        Debug.Log($"[AuthManager] Network reachability: {Application.internetReachability}");
        try
        {
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = AwsConfig.AppClientId,
                AuthParameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            // RetryHelper wraps the AWS call with automatic retry on network errors
            var response = await RetryHelper.ExecuteWithRetry(
                () => AwsManager.Instance.CognitoProvider.InitiateAuthAsync(request),
                "Login",
                maxRetries: 3,
                initialDelayMs: 1000
            );

            var result = response.AuthenticationResult;

            SessionManager.Instance.SaveSession(
                email,
                result.IdToken,
                result.AccessToken,
                result.RefreshToken,
                result.ExpiresIn ?? 3600
            );

            AwsManager.Instance.UpgradeToAuthenticated(result.IdToken);

            Debug.Log($"[AuthManager] Login successful for {email}.");
            return AuthResult.Success("Login successful.");
        }
        catch (UserNotConfirmedException)
        {
            return AuthResult.Failure(AuthError.EmailNotConfirmed);
        }
        catch (NotAuthorizedException)
        {
            return AuthResult.Failure(AuthError.WrongEmailOrPassword);
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Login error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }
    
    // LOGOUT
    // Signs the user out and drops back to guest credentials
    public async Task<AuthResult> Logout()
    {
        try
        {
            var accessToken = SessionManager.Instance.AccessToken;

            if (!string.IsNullOrEmpty(accessToken))
            {
                var request = new GlobalSignOutRequest
                {
                    AccessToken = accessToken
                };

                // GlobalSignOut invalidates all tokens for this user on all devices
                await AwsManager.Instance.CognitoProvider.GlobalSignOutAsync(request);
            }

            SessionManager.Instance.ClearSession();
            AwsManager.Instance.DowngradeToGuest();
            
            // Clear any pending email from previous flows
            PlayerPrefs.DeleteKey("ibm_ros_pending_email");
            PlayerPrefs.Save();

            Debug.Log("[AuthManager] Logout successful.");
            return AuthResult.Success("Logged out successfully.");
        }
        catch (NotAuthorizedException)
        {
            // Token already expired, safe to clear session anyway
            SessionManager.Instance.ClearSession();
            AwsManager.Instance.DowngradeToGuest();
            return AuthResult.Success("Logged out successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Logout error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // FORGOT PASSWORD
    // Sends a password reset code to the user email
    public async Task<AuthResult> ForgotPassword(string email)
    {
        try
        {
            var request = new ForgotPasswordRequest
            {
                ClientId = AwsConfig.AppClientId,
                Username = email
            };

            await AwsManager.Instance.CognitoProvider.ForgotPasswordAsync(request);

            Debug.Log($"[AuthManager] Password reset code sent to {email}.");
            return AuthResult.Success("Password reset code sent. Check your email.");
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (LimitExceededException)
        {
            return AuthResult.Failure(AuthError.TooManyRequests);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Forgot password error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // CONFIRM NEW PASSWORD
    // Resets the password using the code sent to the user email
    public async Task<AuthResult> ConfirmNewPassword(string email, string confirmationCode, string newPassword)
    {
        try
        {
            var request = new ConfirmForgotPasswordRequest
            {
                ClientId = AwsConfig.AppClientId,
                Username = email,
                ConfirmationCode = confirmationCode.Trim(),
                Password = newPassword
            };

            await AwsManager.Instance.CognitoProvider.ConfirmForgotPasswordAsync(request);

            Debug.Log($"[AuthManager] Password reset successful for {email}.");
            return AuthResult.Success("Password reset successful. You can now log in.");
        }
        catch (CodeMismatchException)
        {
            return AuthResult.Failure(AuthError.InvalidConfirmationCode);
        }
        catch (ExpiredCodeException)
        {
            return AuthResult.Failure(AuthError.ExpiredConfirmationCode);
        }
        catch (InvalidPasswordException)
        {
            return AuthResult.Failure(AuthError.WeakPassword);
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Confirm new password error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }

    // REFRESH SESSION
    // Uses the refresh token to get new access and id tokens without re-login
    public async Task<AuthResult> RefreshSession()
    {
        try
        {
            var refreshToken = SessionManager.Instance.RefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
                return AuthResult.Failure(AuthError.NoActiveSession);

            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                ClientId = AwsConfig.AppClientId,
                AuthParameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            };

            var response = await RetryHelper.ExecuteWithRetry(
                () => AwsManager.Instance.CognitoProvider.InitiateAuthAsync(request),
                "RefreshSession",
                maxRetries: 3,
                initialDelayMs: 1000
            );

            var result = response.AuthenticationResult;

            SessionManager.Instance.UpdateTokens(
                result.IdToken,
                result.AccessToken,
                result.ExpiresIn ?? 3600
            );

            AwsManager.Instance.RefreshCredentials(result.IdToken);

            Debug.Log("[AuthManager] Session refreshed successfully.");
            return AuthResult.Success("Session refreshed.");
        }
        catch (NotAuthorizedException)
        {
            SessionManager.Instance.ClearSession();
            AwsManager.Instance.DowngradeToGuest();
            return AuthResult.Failure(AuthError.SessionExpired);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Refresh session error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }
    
    // DELETE ACCOUNT
    // Permanently deletes the user from Cognito
    // Requires the user to re-enter their password to confirm
    public async Task<AuthResult> DeleteAccount(string email, string password)
    {
        try
        {
            // Re-authenticate first to confirm the user owns this account
            // Never delete an account without re-verifying identity
            var reAuthRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = AwsConfig.AppClientId,
                AuthParameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var reAuthResponse = await RetryHelper.ExecuteWithRetry(
                () => AwsManager.Instance.CognitoProvider.InitiateAuthAsync(reAuthRequest),
                "DeleteAccount.ReAuth",
                maxRetries: 2,
                initialDelayMs: 1000
            );

            string freshAccessToken = reAuthResponse.AuthenticationResult.AccessToken;

            // Delete the user using the fresh access token
            var deleteRequest = new DeleteUserRequest
            {
                AccessToken = freshAccessToken
            };

            await RetryHelper.ExecuteWithRetry(
                () => AwsManager.Instance.CognitoProvider.DeleteUserAsync(deleteRequest),
                "DeleteAccount",
                maxRetries: 2,
                initialDelayMs: 1000
            );

            // Clear all local session data after deletion
            SessionManager.Instance.ClearSession();
            AwsManager.Instance.DowngradeToGuest();
            SessionRefreshService.Instance.Stop();
            
            // Clear pending email on account deletion too
            PlayerPrefs.DeleteKey("ibm_ros_pending_email");
            PlayerPrefs.Save();

            Debug.Log($"[AuthManager] Account deleted for {email}.");
            return AuthResult.Success("Your account has been permanently deleted.");
        }
        catch (NotAuthorizedException)
        {
            return AuthResult.Failure(AuthError.WrongEmailOrPassword);
        }
        catch (UserNotFoundException)
        {
            return AuthResult.Failure(AuthError.UserNotFound);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthManager] Delete account error: {e.Message}");
            return AuthResult.Failure(AuthError.UnknownError, e.Message);
        }
    }


}