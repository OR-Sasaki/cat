namespace Root.State
{
    public class SceneLoaderState
    {
        public string TargetSceneName { get; private set; }
        public float FadeOutDuration { get; private set; }
        public float FadeInDuration { get; private set; }

        public void Setup(string targetSceneName, float fadeOutDuration, float fadeInDuration)
        {
            TargetSceneName = targetSceneName;
            FadeOutDuration = fadeOutDuration;
            FadeInDuration = fadeInDuration;
        }
    }
}
