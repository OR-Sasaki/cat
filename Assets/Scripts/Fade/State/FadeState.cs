namespace Fade.State
{
    public enum FadePhase
    {
        None,
        FadingOut,
        Loading,
        FadingIn,
        Completed
    }

    public class FadeState
    {
        FadePhase _currentPhase = FadePhase.None;

        public void SetPhase(FadePhase phase)
        {
            _currentPhase = phase;
        }
    }
}
