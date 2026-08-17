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
        readonly ButtonSeAttacher _buttonSeAttacher;

        [Inject]
        public AudioSceneService(IAudioService audioService, AudioRegistry audioRegistry, ButtonSeAttacher buttonSeAttacher)
        {
            _audioService = audioService;
            _audioRegistry = audioRegistry;
            _buttonSeAttacher = buttonSeAttacher;
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
            if (bgmId is not null)
            {
                _audioService.PlayBgm(bgmId.Value);
            }

            _buttonSeAttacher.AttachToScene(scene);
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
