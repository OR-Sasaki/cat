using Root.Service;
using VContainer;
using VContainer.Unity;

namespace Root.Scope
{
    public class RootScop : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ICatLogger, EditorLogger>(Lifetime.Scoped);
        }
    }
}
