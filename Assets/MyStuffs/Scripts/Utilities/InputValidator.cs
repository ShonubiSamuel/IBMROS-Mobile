using System.Text.RegularExpressions;

public static class InputValidator
{
    // Minimum and maximum field lengths
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 64;
    private const int MaxEmailLength = 254;
    private const int ConfirmationCodeLength = 6;

    // Regex pattern for valid email format
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled
    );

    // Regex patterns for password strength
    private static readonly Regex HasUpperCase = new Regex(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex HasLowerCase = new Regex(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new Regex(@"\d", RegexOptions.Compiled);
    private static readonly Regex HasSpecialChar = new Regex(@"[!@#$%^&*(),.?""':{}|<>]", RegexOptions.Compiled);

    // VALIDATE SIGN UP
    // Validates all three sign up fields together
    public static string ValidateSignUp(string email, string password, string confirmPassword)
    {
        string emailError = ValidateEmail(email);
        if (emailError != null)
            return emailError;

        string passwordError = ValidatePassword(password);
        if (passwordError != null)
            return passwordError;

        string confirmError = ValidateConfirmPassword(password, confirmPassword);
        if (confirmError != null)
            return confirmError;

        return null;
    }

    // VALIDATE LOGIN
    // Validates email and password fields for login
    public static string ValidateLogin(string email, string password)
    {
        string emailError = ValidateEmail(email);
        if (emailError != null)
            return emailError;

        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";

        return null;
    }

    // VALIDATE CONFIRM NEW PASSWORD
    // Validates reset code, new password, and confirm password fields
    public static string ValidateConfirmNewPassword(
        string code,
        string newPassword,
        string confirmNewPassword)
    {
        string codeError = ValidateConfirmationCode(code);
        if (codeError != null)
            return codeError;

        string passwordError = ValidatePassword(newPassword);
        if (passwordError != null)
            return passwordError;

        string confirmError = ValidateConfirmPassword(newPassword, confirmNewPassword);
        if (confirmError != null)
            return confirmError;

        return null;
    }

    // VALIDATE EMAIL
    // Checks format, length, and empty state
    public static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email address is required.";

        if (email.Length > MaxEmailLength)
            return $"Email address is too long. Maximum {MaxEmailLength} characters.";

        if (!EmailRegex.IsMatch(email))
            return "Please enter a valid email address.";

        return null;
    }

    // VALIDATE PASSWORD
    // Checks length, complexity requirements
    public static string ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";

        if (password.Length < MinPasswordLength)
            return $"Password must be at least {MinPasswordLength} characters.";

        if (password.Length > MaxPasswordLength)
            return $"Password must not exceed {MaxPasswordLength} characters.";

        if (!HasUpperCase.IsMatch(password))
            return "Password must contain at least one uppercase letter.";

        if (!HasLowerCase.IsMatch(password))
            return "Password must contain at least one lowercase letter.";

        if (!HasDigit.IsMatch(password))
            return "Password must contain at least one number.";

        if (!HasSpecialChar.IsMatch(password))
            return "Password must contain at least one special character.";

        return null;
    }

    // VALIDATE CONFIRM PASSWORD
    // Checks that both password fields match
    public static string ValidateConfirmPassword(string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(confirmPassword))
            return "Please confirm your password.";

        if (password != confirmPassword)
            return "Passwords do not match.";

        return null;
    }

    // VALIDATE CONFIRMATION CODE
    // Checks the 6 digit code sent by AWS to user email
    public static string ValidateConfirmationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Verification code is required.";

        if (code.Length != ConfirmationCodeLength)
            return $"Verification code must be {ConfirmationCodeLength} digits.";

        if (!Regex.IsMatch(code, @"^\d+$"))
            return "Verification code must contain numbers only.";

        return null;
    }
}