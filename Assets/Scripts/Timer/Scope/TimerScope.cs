using System.Threading;
using Root.Scope;
using Timer.Service;
using Timer.Starter;
using Timer.State;
using Timer.Manager;
using Timer.View;
using VContainer;
using VContainer.Unity;

namespace Timer.Scope
{
    public class TimerScope : SceneScope
    {
        CancellationTokenSource _cts;

        protected override void Configure(IContainerBuilder builder)
        {
            _cts = new CancellationTokenSource();
            builder.RegisterInstance(_cts.Token);

            // State
            builder.Register<PomodoroState>(Lifetime.Scoped);

            // Service
            builder.Register<PomodoroService>(Lifetime.Scoped);

            // Manager
            builder.RegisterComponentInHierarchy<UiSlideManager>();

            // View
            builder.RegisterComponentInHierarchy<FocusPanelView>();
            builder.RegisterComponentInHierarchy<BreakPanelView>();
            builder.RegisterComponentInHierarchy<CompletePanelView>();
            builder.RegisterComponentInHierarchy<BackgroundScrollView>();
            builder.RegisterComponentInHierarchy<TimerCharacterView>();

            // EntryPoint
            builder.RegisterEntryPoint<TimerStarter>();
        }

        protected override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnDestroy();
        }
    }
}
