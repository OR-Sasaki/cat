using Root.Service;
using Root.State;
using UnityEngine;

namespace Home.Service
{
    public class HomeFooterService
    {
        readonly SceneLoader _sceneLoader;
         readonly MasterDataState _masterDataState;

        public HomeFooterService(SceneLoader sceneLoader, MasterDataState masterDataState)
        {
            _sceneLoader = sceneLoader;
            _masterDataState = masterDataState;
        }

        public void NavigateToScene(string sceneName)
        {
            _sceneLoader.Load(sceneName);
        }
    }
}
