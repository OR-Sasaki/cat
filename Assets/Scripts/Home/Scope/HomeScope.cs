using Cat.Character;
using Home.Service;
using Home.Starter;
using Home.State;
using Home.View;
using Root.Scope;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Scope
{
    public class HomeScope : SceneScope
    {
        [SerializeField] CharacterView _characterView;
        [SerializeField] HomeUiView _homeUiView;
        [SerializeField] ClosetUiView _closetUiView;
        [SerializeField] RedecorateUiView _redecorateUiView;
        [SerializeField] CameraView _cameraView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_characterView);
            builder.RegisterComponent(_homeUiView);
            builder.RegisterComponent(_closetUiView);
            builder.RegisterComponent(_redecorateUiView);
            builder.RegisterComponent(_cameraView);
            builder.Register<HomeState>(Lifetime.Scoped);
            builder.Register<OutfitAssetState>(Lifetime.Scoped);
            builder.Register<FurnitureAssetState>(Lifetime.Scoped);
            builder.Register<HomeStateSetService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<OutfitAssetStarter>();
            builder.RegisterEntryPoint<FurnitureAssetStarter>();
            builder.RegisterEntryPoint<ClosetScrollerService>();
            builder.RegisterEntryPoint<RedecorateScrollerService>();
            builder.RegisterEntryPoint<RedecorateCameraService>();
            builder.RegisterEntryPoint<HomeViewService>();
            builder.RegisterEntryPoint<HomeStarter>();
        }
    }
}
