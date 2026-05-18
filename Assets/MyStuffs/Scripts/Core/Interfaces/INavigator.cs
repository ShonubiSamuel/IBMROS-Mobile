public interface INavigator
{
    // Navigate to a screen by name with transition animation
    void NavigateTo(ScreenName screen);

    // Navigate immediately without animation
    void NavigateToImmediate(ScreenName screen);

    // Returns the currently active screen
    ScreenName CurrentScreen { get; }
}