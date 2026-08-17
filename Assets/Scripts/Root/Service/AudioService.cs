#nullable enable

using System;
using Root.AudioLogic;
using Root.State;
using Root.View;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    public sealed class AudioService : IAudioService
    {
        const float BgmFadeDuration = 0.5f;

        readonly AudioState _state;
        readonly AudioPlayerView _playerView;
        readonly AudioRegistry _registry;
        readonly PlayerPrefsService _playerPrefsService;

        [Inject]
        public AudioService(AudioState state, AudioPlayerView playerView, AudioRegistry registry, PlayerPrefsService playerPrefsService)
        {
            _state = state;
            _playerView = playerView;
            _registry = registry;
            _playerPrefsService = playerPrefsService;
            Load();
        }

        public void PlaySe(SeId id)
        {
            if (id == SeId.None)
                return;

            var effectiveVolume = AudioVolumeLogic.CalcSeVolume(_state.SoundEnabled, _state.SeVolume);
            if (effectiveVolume <= 0f)
                return;

            var clip = _registry.Resolve(id);
            if (clip == null)
            {
                Debug.LogError($"[AudioService] SeId '{id}' に対応する AudioClip が見つかりません。");
                return;
            }

            _playerView.PlaySe(clip, effectiveVolume);
        }

        public void PlayBgm(BgmId id)
        {
            if (_state.CurrentBgm == id)
                return;

            var resolved = _registry.Resolve(id);
            if (resolved == null)
            {
                Debug.LogError($"[AudioService] BgmId '{id}' に対応する AudioClip が見つかりません。");
                return;
            }

            var (clip, baseVolume) = resolved.Value;

            _state.CurrentBgm = id;
            _state.CurrentBgmBaseVolume = baseVolume;

            var targetVolume = AudioVolumeLogic.CalcBgmVolume(_state.SoundEnabled, baseVolume, _state.BgmVolume);
            _playerView.CrossFadeTo(clip, targetVolume, BgmFadeDuration);
        }

        public void StopBgm()
        {
            if (_state.CurrentBgm == null)
                return;

            _playerView.FadeOutAndStop(BgmFadeDuration);
            _state.CurrentBgm = null;
        }

        public void SetBgmVolume(float volume)
        {
            _state.BgmVolume = AudioVolumeLogic.Clamp(volume);

            if (_state.CurrentBgm != null)
            {
                var effectiveVolume = AudioVolumeLogic.CalcBgmVolume(_state.SoundEnabled, _state.CurrentBgmBaseVolume, _state.BgmVolume);
                _playerView.SetBgmVolumeImmediate(effectiveVolume);
            }

            Save();
        }

        public void SetSeVolume(float volume)
        {
            _state.SeVolume = AudioVolumeLogic.Clamp(volume);
            Save();
        }

        public void SetSoundEnabled(bool enabled)
        {
            _state.SoundEnabled = enabled;

            if (_state.CurrentBgm != null)
            {
                var effectiveVolume = AudioVolumeLogic.CalcBgmVolume(_state.SoundEnabled, _state.CurrentBgmBaseVolume, _state.BgmVolume);
                _playerView.SetBgmVolumeImmediate(effectiveVolume);
            }

            Save();
        }

        public AudioSettingSnapshot GetSettingSnapshot()
        {
            return new AudioSettingSnapshot(_state.BgmVolume, _state.SeVolume, _state.SoundEnabled);
        }

        void Load()
        {
            AudioSettingData? data;
            try
            {
                data = _playerPrefsService.Load<AudioSettingData>(PlayerPrefsKey.AudioSetting);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioService] {e.Message}\n{e.StackTrace}");
                data = null;
            }

            data ??= new AudioSettingData();

            _state.BgmVolume = AudioVolumeLogic.Clamp(data.bgmVolume);
            _state.SeVolume = AudioVolumeLogic.Clamp(data.seVolume);
            _state.SoundEnabled = data.soundEnabled;
        }

        void Save()
        {
            var data = new AudioSettingData
            {
                bgmVolume = _state.BgmVolume,
                seVolume = _state.SeVolume,
                soundEnabled = _state.SoundEnabled,
            };
            _playerPrefsService.Save(PlayerPrefsKey.AudioSetting, data);
        }
    }
}
