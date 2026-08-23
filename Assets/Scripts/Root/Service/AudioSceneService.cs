#nullable enable

using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    /// sceneLoaded を購読し、シーンに対応する BGM を自動再生するサービス
    /// IInitializable は RootScope.Awake 中に同期実行され、子の AudioPlayerView.Awake 前に
    /// PlayBgm へ到達して NRE になるため、全 Awake 完了後の IStartable で初期化する
    public sealed class AudioSceneService : IStartable, System.IDisposable
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

        public void Start()
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
