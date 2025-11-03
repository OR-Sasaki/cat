using System;
using System.Threading.Tasks;
using Root.Service;
using UnityEngine;
using VContainer.Unity;

namespace Logo.Starter
{
    public class LogoStarter : IStartable
    {
        readonly SceneLoader _sceneLoader;

        public LogoStarter(SceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public async void Start()
        {
            try
            {
                await Task.Delay(1000);
                _sceneLoader.Load("Title");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogoStarter] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}

