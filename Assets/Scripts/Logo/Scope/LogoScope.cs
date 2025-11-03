using Logo.Starter;
using VContainer;
using VContainer.Unity;

namespace Logo.Scope
{
    public class LogoScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LogoStarter>();
        }
    }
}
