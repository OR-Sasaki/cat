using Cat.Character;
using Home.Service;
using Home.Starter;
using Root.Scope;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Scope
{
    public class HomeScope : SceneScope
    {
        [SerializeField] CharacterView _characterView;
        [SerializeField] OutfitSetting _outfitSetting;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_characterView);
            builder.RegisterInstance(_outfitSetting);
            builder.Register<HomeFooterService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<HomeStarter>();
        }
    }
}
