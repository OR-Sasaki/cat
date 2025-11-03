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
            catch (Exception e)
            {
                Debug.LogError($"[FadeStarter] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
