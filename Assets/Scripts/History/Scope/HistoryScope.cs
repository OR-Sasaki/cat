using History.Service;
using VContainer;
using VContainer.Unity;

namespace History.Scope
{
    public class HistoryScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReturnButtonService>(Lifetime.Scoped);
        }
    }
}

