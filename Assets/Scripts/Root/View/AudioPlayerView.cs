#nullable enable

using DG.Tweening;
using Root.AudioLogic;
using UnityEngine;

namespace Root.View
{
    /// AudioSource 実体を持つ受動 View。判断ロジックは持たない
    /// RootLifetimeScope プレハブの子に配置され DDoL になる前提
    public sealed class AudioPlayerView : MonoBehaviour
    {
        [SerializeField] AudioSource _bgmSourceA = null!;
        [SerializeField] AudioSource _bgmSourceB = null!;
        [SerializeField] int _sePoolSize = 8;

        AudioSource[] _seSources = null!;
        int[] _seStartOrder = null!;
        int _seOrderCounter;

        AudioSource _activeBgmSource = null!;
        AudioSource _inactiveBgmSource = null!;

        Tween? _bgmTween;

        void Awake()
        {
            _activeBgmSource = _bgmSourceA;
            _inactiveBgmSource = _bgmSourceB;

            _seSources = new AudioSource[_sePoolSize];
            _seStartOrder = new int[_sePoolSize];
            for (var i = 0; i < _sePoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _seSources[i] = source;
            }
        }

        /// SE をワンショット再生する。空きがなければ最古のソースを奪う
        public void PlaySe(AudioClip clip, float volume)
        {
            var isPlaying = new bool[_seSources.Length];
            for (var i = 0; i < _seSources.Length; i++)
            {
                isPlaying[i] = _seSources[i].isPlaying;
            }

            var index = SeSourcePicker.Pick(isPlaying, _seStartOrder);
            if (index < 0)
                return;

            var source = _seSources[index];
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            _seStartOrder[index] = _seOrderCounter++;
            source.Play();
        }

        /// BGM を対象クリップへクロスフェード切替する
        public void CrossFadeTo(AudioClip clip, float targetVolume, float duration)
        {
            _bgmTween?.Kill();

            var fadeOutSource = _activeBgmSource;
            var fadeInSource = _inactiveBgmSource;

            fadeInSource.clip = clip;
            fadeInSource.loop = true;
            fadeInSource.volume = 0f;
            fadeInSource.Play();

            _bgmTween = DOTween.Sequence()
                .Join(fadeInSource.DOFade(targetVolume, duration))
                .Join(fadeOutSource.DOFade(0f, duration).OnComplete(fadeOutSource.Stop))
                .SetLink(gameObject);

            _activeBgmSource = fadeInSource;
            _inactiveBgmSource = fadeOutSource;
        }

        /// BGM をフェードアウトして停止する
        public void FadeOutAndStop(float duration)
        {
            _bgmTween?.Kill();

            var source = _activeBgmSource;
            _bgmTween = source.DOFade(0f, duration)
                .OnComplete(source.Stop)
                .SetLink(gameObject);
        }

        /// フェード中でも Tween を止めてアクティブソースの音量を即時設定する
        public void SetBgmVolumeImmediate(float volume)
        {
            _bgmTween?.Kill();

            _inactiveBgmSource.Stop();
            _activeBgmSource.volume = volume;
        }

        void OnDestroy()
        {
            _bgmTween?.Kill();
        }
    }
}
