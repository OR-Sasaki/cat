#nullable enable

using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    /// sceneLoaded を購読し、シーンに対応する BGM を自動再生するサービス
    public sealed class AudioSceneService : IInitializable, System.IDisposable
    {
        readonly IAudioService _audioService;
        readonly AudioRegistry _audioRegistry;

        [Inject]
        public AudioSceneService(IAudioService audioService, AudioRegistry audioRegistry)
        {
            _audioService = audioService;
            _audioRegistry = audioRegistry;
        }

        public void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            // RootScope 構築時点で既にロードされている起動シーンにも同じ処理を適用する
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var bgmId = _audioRegistry.ResolveSceneBgm(scene.name);
            if (bgmId == null)
            {
                return;
            }

            _audioService.PlayBgm(bgmId.Value);
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
