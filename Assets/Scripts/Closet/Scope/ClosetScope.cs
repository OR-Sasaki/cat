using Closet.Service;
using VContainer;
using VContainer.Unity;

namespace Closet.Scope
{
    public class ClosetScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReturnButtonService>(Lifetime.Scoped);
        }
    }
}

