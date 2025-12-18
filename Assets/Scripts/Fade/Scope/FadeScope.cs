using Fade.Manager;
using Fade.Service;
using Fade.Starter;
using Fade.State;
using Fade.View;
using Root.Scope;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Fade.Scope
{
    public class FadeScope : SceneScope
    {
        [SerializeField] FadeView _fadeView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_fadeView);
            builder.Register<FadeManager>(Lifetime.Scoped);
            builder.Register<FadeState>(Lifetime.Scoped);
            builder.Register<FadeService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<FadeStarter>();
        }
    }
}
