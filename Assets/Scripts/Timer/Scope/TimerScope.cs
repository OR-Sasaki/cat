using Root.Scope;
using Timer.View;
using VContainer;
using VContainer.Unity;

namespace Timer.Scope
{
    public class TimerScope : SceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<ReturnButtonView>();
        }
    }
}

