using Logo.Starter;
using Root.Scope;
using VContainer;
using VContainer.Unity;

namespace Logo.Scope
{
    public class LogoScope : SceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LogoStarter>();
        }
    }
}
