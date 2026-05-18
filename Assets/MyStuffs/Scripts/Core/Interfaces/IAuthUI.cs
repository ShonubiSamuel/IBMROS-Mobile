public interface IAuthUI
{
    // Shows an error message on the screen
    void ShowError(string message);

    // Shows a success message on the screen
    void ShowSuccess(string message);

    // Clears any visible feedback message
    void ClearFeedback();

    // Enables or disables all interactive elements
    // Used during loading to prevent double submissions
    void SetLoadingState(bool isLoading);

    // Called when the screen becomes the active screen
    // Use this to pre-fill fields or reset state
    void OnScreenActivated();
}