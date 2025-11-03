using Root.Service;

namespace Redecorate.Service
{
    public class ReturnButtonService
    {
        readonly SceneLoader _sceneLoader;

        public ReturnButtonService(SceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void NavigateToScene(string sceneName)
        {
            _sceneLoader.Load(sceneName);
        }
    }
}

