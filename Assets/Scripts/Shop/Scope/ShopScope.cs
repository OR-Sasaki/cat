#nullable enable

using Root.Scope;
using Shop.Service;
using Shop.Starter;
using Shop.State;
using Shop.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Shop.Scope
{
    public class ShopScope : SceneScope
    {
        [SerializeField] ShopView? _shopView;

        protected override void Configure(IContainerBuilder builder)
        {
            // View
            if (_shopView != null)
            {
                builder.RegisterComponent(_shopView);
            }

            // State
            builder.Register<ShopState>(Lifetime.Scoped);

            // Service
            builder.Register<ShopService>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            // EntryPoint
            builder.RegisterEntryPoint<ShopStarter>();
        }
    }
}
