using System;
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
            // Outfit アセットのキャッシュと適用はシーンを跨いで共通化する
            builder.Register<OutfitAssetState>(Lifetime.Singleton);
            builder.Register<OutfitAssetService>(Lifetime.Singleton);
            builder.Register<CharacterOutfitService>(Lifetime.Singleton);
            builder.Register<InitialItemService>(Lifetime.Singleton);
            builder.Register<UserItemInventoryState>(Lifetime.Singleton);
            builder.Register<UserItemInventoryService>(Lifetime.Singleton)
                .As<IUserItemInventoryService>().AsSelf();
            // 数量ベースの所持家具を IsoGrid 用の UserFurnitureId へ射影する
            builder.Register<UserFurnitureInstanceService>(Lifetime.Singleton);
            builder.Register<UserPointState>(Lifetime.Singleton);
            builder.Register<UserPointService>(Lifetime.Singleton)
                .As<IUserPointService>().AsSelf();
            builder.Register<TimerRecordState>(Lifetime.Singleton);
            builder.Register<TimerRecordService>(Lifetime.Singleton)
                .As<ITimerRecordService>().AsSelf();

            builder.Register<DialogState>(Lifetime.Singleton);
            builder.Register<DialogContainer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<DialogService>(Lifetime.Singleton).As<IDialogService>();

            var rewardedAdConfig = Resources.Load<RewardedAdConfig>("RewardedAdConfig");
            if (rewardedAdConfig == null)
            {
                throw new InvalidOperationException("[RootScope] Assets/Resources/RewardedAdConfig.asset が見つかりません。");
            }
            builder.RegisterInstance(rewardedAdConfig);
#if UNITY_EDITOR
            builder.Register<EditorRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#elif UNITY_ANDROID || UNITY_IOS
            builder.Register<LevelPlayRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#else
            builder.Register<EditorRewardedAdService>(Lifetime.Singleton).As<IRewardedAdService>();
#endif
            builder.RegisterEntryPoint<RewardedAdServiceStarter>();
        }
    }
}
