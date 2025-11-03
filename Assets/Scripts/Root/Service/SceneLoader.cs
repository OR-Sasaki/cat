using Root.State;
using UnityEngine.SceneManagement;

namespace Root.Service
{
    public class SceneLoader
    {
        readonly SceneLoaderState _state;

        public SceneLoader(SceneLoaderState state)
        {
            _state = state;
        }

        public void Load(string targetSceneName, float fadeOutDuration = 0.5f, float fadeInDuration = 0.5f)
        {
            _state.Setup(targetSceneName, fadeOutDuration, fadeInDuration);

            SceneManager.LoadScene("Fade", LoadSceneMode.Additive);
        }
    }
}
