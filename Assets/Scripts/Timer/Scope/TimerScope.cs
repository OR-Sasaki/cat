using System.Threading;
using Cat.Character;
using Root.Scope;
using Root.Starter;
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
            builder.RegisterComponentInHierarchy<TimerLifecycleManager>();
            builder.RegisterComponentInHierarchy<CompleteSequenceManager>();

            // View
            builder.RegisterComponentInHierarchy<FocusPanelView>();
            builder.RegisterComponentInHierarchy<BreakPanelView>();
            builder.RegisterComponentInHierarchy<CompletePanelView>();
            builder.RegisterComponentInHierarchy<BackgroundScrollView>();
            builder.RegisterComponentInHierarchy<TimerCharacterView>();
            builder.RegisterComponentInHierarchy<TimerEveningView>();
            // Home で着替えた見た目を反映するため、スプライト差し替え用の CharacterView も登録する
            builder.RegisterComponentInHierarchy<CharacterView>();

            // EntryPoint
            builder.RegisterEntryPoint<TimerStarter>();
            builder.RegisterEntryPoint<CharacterOutfitStarter>();
        }

        protected override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnDestroy();
        }
    }
}
