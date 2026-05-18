public class AuthResult
{
    // Whether the operation succeeded
    public bool IsSuccess { get; private set; }

    // Human readable message to show the user
    public string Message { get; private set; }

    // Error type, null if operation succeeded
    public AuthError? Error { get; private set; }

    // Optional extra detail for debugging
    public string DebugDetail { get; private set; }

    // Optional user data returned after login
    public UserModel User { get; private set; }

    // Private constructor, use static methods below to create instances
    private AuthResult() { }

    // Creates a success result with a message
    public static AuthResult Success(string message, UserModel user = null)
    {
        return new AuthResult
        {
            IsSuccess = true,
            Message = message,
            Error = null,
            User = user
        };
    }

    // Creates a failure result with an error type
    public static AuthResult Failure(AuthError error, string debugDetail = null)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = AuthErrorMessages.GetMessage(error),
            Error = error,
            DebugDetail = debugDetail
        };
    }

    public override string ToString()
    {
        return IsSuccess
            ? $"AuthResult[Success: {Message}]"
            : $"AuthResult[Failure: {Error}, {Message}]";
    }
}