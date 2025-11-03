using Fade.Manager;
using Fade.State;
using Root.State;
using VContainer.Unity;

namespace Fade.Starter
{
    public class FadeStarter : IStartable
    {
        readonly FadeManager _fadeManager;

        public FadeStarter(FadeManager fadeManager)
        {
            _fadeManager = fadeManager;
        }

        public async void Start()
        {
            await _fadeManager.StartFadeSequence();
        }
    }
}
