using Root.Service;

namespace Home.Service
{
    public class HomeFooterService
    {
        readonly SceneLoader _sceneLoader;

        public HomeFooterService(SceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void NavigateToScene(string sceneName)
        {
            _sceneLoader.Load(sceneName);
        }
    }
}

