using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

public static class FeedbackHelper
{
    private const int AutoDismissDelayMs = 5000;

    // Call this instead of manually adding banner classes
    // It shows the banner and auto-dismisses after 5 seconds
    public static async Task ShowError(
        VisualElement banner,
        Label text,
        string message,
        CancellationToken token = default)
    {
        if (banner == null || text == null)
            return;

        text.text = message;
        banner.RemoveFromClassList("feedback-banner--success");
        banner.AddToClassList("feedback-banner--error");
        banner.AddToClassList("feedback-banner--visible");

        await DismissAfterDelay(banner, token);
    }

    public static async Task ShowSuccess(
        VisualElement banner,
        Label text,
        string message,
        CancellationToken token = default)
    {
        if (banner == null || text == null)
            return;

        text.text = message;
        banner.RemoveFromClassList("feedback-banner--error");
        banner.AddToClassList("feedback-banner--success");
        banner.AddToClassList("feedback-banner--visible");

        await DismissAfterDelay(banner, token);
    }

    public static void Clear(VisualElement banner, Label text)
    {
        if (banner == null)
            return;

        if (text != null)
            text.text = string.Empty;

        banner.RemoveFromClassList("feedback-banner--visible");
        banner.RemoveFromClassList("feedback-banner--error");
        banner.RemoveFromClassList("feedback-banner--success");
    }

    private static async Task DismissAfterDelay(
        VisualElement banner,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(AutoDismissDelayMs, token);

            // Only dismiss if still visible and not cancelled
            if (!token.IsCancellationRequested)
                banner.RemoveFromClassList("feedback-banner--visible");
        }
        catch (TaskCanceledException)
        {
            // A new message replaced this one before timer finished
            // That is fine, do nothing
        }
    }
}