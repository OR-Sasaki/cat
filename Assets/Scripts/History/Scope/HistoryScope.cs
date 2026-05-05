using History.Service;
using History.Starter;
using History.State;
using History.View;
using Root.Scope;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace History.Scope
{
    public class HistoryScope : SceneScope
    {
        [SerializeField] HistoryCalendarSettings _calendarSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            // Settings (Inspector で割り当てた ScriptableObject)
            builder.RegisterInstance(_calendarSettings);

            // State
            builder.Register<HistoryCalendarState>(Lifetime.Scoped);

            // Service
            builder.Register<StreakCalculator>(Lifetime.Scoped);

            // View (シーン階層に配置されたコンポーネントを登録)
            builder.RegisterComponentInHierarchy<StreakLabelView>();
            builder.RegisterComponentInHierarchy<MonthHeaderView>();
            builder.RegisterComponentInHierarchy<CalendarGridView>();
            builder.RegisterComponentInHierarchy<SelectedDateFooterView>();

            // EntryPoint
            builder.RegisterEntryPoint<HistoryStarter>();
        }
    }
}
