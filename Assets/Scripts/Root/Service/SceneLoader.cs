using Root.State;
using UnityEngine.SceneManagement;

namespace Root.Service
{
    public class SceneLoader
    {
        readonly SceneLoaderState _state;

        bool _isLoading;

        public SceneLoader(SceneLoaderState state)
        {
            _state = state;
        }

        public void Load(string targetSceneName, float fadeOutDuration = 0.5f, float fadeInDuration = 0.5f)
        {
            if (_isLoading) return;
            _isLoading = true;

            _state.Setup(targetSceneName, fadeOutDuration, fadeInDuration);

            SceneManager.LoadScene("Fade", LoadSceneMode.Additive);
        }

        /// シーン遷移完了時にFadeServiceから呼び出す
        public void CompleteLoad()
        {
            _isLoading = false;
        }
    }
}
