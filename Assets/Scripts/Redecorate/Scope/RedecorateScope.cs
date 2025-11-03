using Redecorate.Service;
using VContainer;
using VContainer.Unity;

namespace Redecorate.Scope
{
    public class RedecorateScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReturnButtonService>(Lifetime.Scoped);
        }
    }
}

