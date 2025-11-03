using Home.Service;
using Home.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Scope
{
    public class HomeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<HomeFooterService>(Lifetime.Scoped);
        }
    }
}

