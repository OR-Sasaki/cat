using System;
using Fade.Manager;
using UnityEngine;
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
            try
            {
                await _fadeManager.StartFadeSequence();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FadeStarter] Exception occurred during fade sequence: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
