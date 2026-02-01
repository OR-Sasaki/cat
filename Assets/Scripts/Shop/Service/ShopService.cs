using Root.Service;
using Shop.State;

namespace Shop.Service
{
    public class ShopService
    {
        readonly ShopState _state;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;

        public ShopService(ShopState state, IDialogService dialogService, SceneLoader sceneLoader)
        {
            _state = state;
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            // Mock data initialization will be implemented in Task 5
        }
    }
}