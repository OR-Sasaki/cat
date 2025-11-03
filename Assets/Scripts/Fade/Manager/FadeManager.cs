using System;
using System.Threading.Tasks;
using Fade.Service;
using Fade.State;
using Root.State;
using UnityEngine;

namespace Fade.Manager
{
    public class FadeManager
    {
        readonly FadeState _state;
        readonly FadeService _service;
        readonly SceneLoaderState _sceneLoaderState;

        public FadeManager(FadeState state, FadeService service, SceneLoaderState sceneLoaderState)
        {
            _state = state;
            _service = service;
            _sceneLoaderState = sceneLoaderState;
        }

        public async Task StartFadeSequence()
        {
            try
            {
                _state.SetPhase(FadePhase.FadingOut);
                await _service.FadeOut(_sceneLoaderState.FadeOutDuration);

                _state.SetPhase(FadePhase.Loading);
                await _service.LoadScene(_sceneLoaderState.TargetSceneName);

                _state.SetPhase(FadePhase.FadingIn);
                await _service.FadeIn(_sceneLoaderState.FadeInDuration);

                _state.SetPhase(FadePhase.Completed);
                await _service.UnloadFadeScene();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FadeManager] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
