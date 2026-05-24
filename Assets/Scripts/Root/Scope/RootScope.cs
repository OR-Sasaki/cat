using Root.Service;
using Root.State;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Root.Scope
{
    public class RootScope : LifetimeScope
    {
        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = 60;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<SceneLoaderState>(Lifetime.Singleton);
            builder.Register<MasterDataState>(Lifetime.Singleton);
            builder.Register<MasterDataImportService>(Lifetime.Singleton);
            builder.Register<SystemClock>(Lifetime.Singleton).As<IClock>();
            builder.Register<UserState>(Lifetime.Singleton);
            builder.Register<PlayerPrefsService>(Lifetime.Singleton);
            builder.Register<UserEquippedOutfitState>(Lifetime.Singleton);
            builder.Register<UserEquippedOutfitService>(Lifetime.Singleton);
            builder.Register<UserItemInventoryState>(Lifetime.Singleton);
            builder.Register<UserItemInventoryService>(Lifetime.Singleton)
                .As<IUserItemInventoryService>().AsSelf();
            builder.Register<UserPointState>(Lifetime.Singleton);
            builder.Register<UserPointService>(Lifetime.Singleton)
                .As<IUserPointService>().AsSelf();
            builder.Register<TimerRecordState>(Lifetime.Singleton);
            builder.Register<TimerRecordService>(Lifetime.Singleton)
                .As<ITimerRecordService>().AsSelf();

            builder.Register<DialogState>(Lifetime.Singleton);
            builder.Register<DialogContainer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<DialogService>(Lifetime.Singleton).As<IDialogService>();

            builder.RegisterInstance(Resources.Load<RewardedAdConfig>("RewardedAdConfig"));
#if UNITY_EDITOR
            builder.Register<EditorRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#elif UNITY_ANDROID || UNITY_IOS
            builder.Register<LevelPlayRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#else
            builder.Register<EditorRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#endif
            builder.RegisterEntryPoint<RewardedAdServiceStarter>();

            builder.RegisterEntryPoint<UserDataImportService>();
        }
    }
}
