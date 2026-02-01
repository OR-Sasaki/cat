using Shop.Service;
using VContainer.Unity;

namespace Shop.Starter
{
    public class ShopStarter : IStartable
    {
        readonly ShopService _shopService;

        public ShopStarter(ShopService shopService)
        {
            _shopService = shopService;
        }

        public void Start()
        {
            _shopService.Initialize();
        }
    }
}