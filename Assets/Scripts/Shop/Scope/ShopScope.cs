using Shop.Service;
using VContainer;
using VContainer.Unity;

namespace Shop.Scope
{
    public class ShopScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReturnButtonService>(Lifetime.Scoped);
        }
    }
}

