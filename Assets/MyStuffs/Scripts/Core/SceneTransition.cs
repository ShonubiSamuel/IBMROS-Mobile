public static class SceneTransition
{
    public static bool SkipSplash { get; private set; } = false;

    public static void SetSkipSplash(bool skip)
    {
        SkipSplash = skip;
    }
}