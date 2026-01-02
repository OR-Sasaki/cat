using Root.Service;
using Root.State;
using VContainer;
using VContainer.Unity;

namespace Root.Scope
{
    public class RootScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<SceneLoaderState>(Lifetime.Singleton);
            builder.Register<MasterDataState>(Lifetime.Singleton);
            builder.Register<MasterDataImportService>(Lifetime.Singleton);
            builder.Register<PlayerPrefsService>(Lifetime.Singleton);
            builder.Register<PlayerOutfitState>(Lifetime.Singleton);
            builder.Register<PlayerOutfitService>(Lifetime.Singleton);

            builder.Register<DialogState>(Lifetime.Singleton);
            builder.Register<DialogContainer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<DialogService>(Lifetime.Singleton).As<IDialogService>();
        }
    }
}
