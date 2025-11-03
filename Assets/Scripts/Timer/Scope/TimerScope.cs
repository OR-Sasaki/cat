using Timer.Service;
using VContainer;
using VContainer.Unity;

namespace Timer.Scope
{
    public class TimerScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReturnButtonService>(Lifetime.Scoped);
        }
    }
}

