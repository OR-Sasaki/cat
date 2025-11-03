using System.Threading.Tasks;
using Fade.View;
using UnityEngine.SceneManagement;

namespace Fade.Service
{
    public class FadeService
    {
        const string FadeSceneName = "Fade";
        readonly FadeView _fadeView;

        public FadeService(FadeView fadeView)
        {
            _fadeView = fadeView;
        }

        public async Task FadeOut(float duration)
        {
            await _fadeView.AnimateFade(0f, 1f, duration);
        }

        public async Task FadeIn(float duration)
        {
            await _fadeView.AnimateFade(1f, 0f, duration);
        }

        public async Task LoadScene(string sceneName)
        {
            // Fadeシーン以外で最も古いシーンを探索
            string oldSceneName = null;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == FadeSceneName) continue;

                oldSceneName = scene.name;
                break;
            }

            // 古いシーンをアンロード
            if (!string.IsNullOrEmpty(oldSceneName))
            {
                var asyncUnload = SceneManager.UnloadSceneAsync(oldSceneName);
                while (asyncUnload is { isDone: false })
                {
                    await Task.Yield();
                }
            }

            // 遷移先のシーンをロード
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (asyncLoad is { isDone: false})
            {
                await Task.Yield();
            }

            // 遷移先のシーンをアクティブに変更
            var newScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(newScene);
        }

        public async Task UnloadFadeScene()
        {
            var asyncUnload = SceneManager.UnloadSceneAsync(FadeSceneName);
            while (asyncUnload is { isDone: false })
            {
                await Task.Yield();
            }
        }
    }
}
