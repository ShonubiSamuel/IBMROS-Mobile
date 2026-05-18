public enum AuthError
{
    // Sign up errors
    EmailAlreadyExists,
    WeakPassword,
    InvalidInput,

    // Confirmation errors
    InvalidConfirmationCode,
    ExpiredConfirmationCode,

    // Login errors
    WrongEmailOrPassword,
    UserNotFound,
    EmailNotConfirmed,

    // Session errors
    SessionExpired,
    NoActiveSession,
    TokenRefreshFailed,

    // Rate limiting
    TooManyRequests,

    // Network errors
    NetworkError,
    ServiceUnavailable,

    // Fallback
    UnknownError
}

// Separate class that maps every error type to a clean user facing message
public static class AuthErrorMessages
{
    public static string GetMessage(AuthError error)
    {
        switch (error)
        {
            // Sign up errors
            case AuthError.EmailAlreadyExists:
                return "An account with this email already exists. Try logging in instead.";

            case AuthError.WeakPassword:
                return "Password must be at least 8 characters and include a number and special character.";

            case AuthError.InvalidInput:
                return "One or more fields contain invalid values. Please check and try again.";

            // Confirmation errors
            case AuthError.InvalidConfirmationCode:
                return "The verification code you entered is incorrect. Please check and try again.";

            case AuthError.ExpiredConfirmationCode:
                return "Your verification code has expired. Request a new one.";

            // Login errors
            case AuthError.WrongEmailOrPassword:
                return "Incorrect email or password. Please try again.";

            case AuthError.UserNotFound:
                return "No account found with this email. Please sign up first.";

            case AuthError.EmailNotConfirmed:
                return "Please verify your email before logging in. Check your inbox for a verification code.";

            // Session errors
            case AuthError.SessionExpired:
                return "Your session has expired. Please log in again.";

            case AuthError.NoActiveSession:
                return "No active session found. Please log in.";

            case AuthError.TokenRefreshFailed:
                return "Failed to refresh your session. Please log in again.";

            // Rate limiting
            case AuthError.TooManyRequests:
                return "Too many attempts. Please wait a few minutes and try again.";

            // Network errors
            case AuthError.NetworkError:
                return "Network error. Please check your internet connection and try again.";

            case AuthError.ServiceUnavailable:
                return "Service is temporarily unavailable. Please try again shortly.";

            // Fallback
            case AuthError.UnknownError:
            default:
                return "Something went wrong. Please try again.";
        }
    }
}