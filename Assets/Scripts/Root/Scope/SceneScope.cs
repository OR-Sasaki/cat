using Root.Service;
using VContainer;
using VContainer.Unity;

namespace Root.Scope
{
    public abstract class SceneScope : LifetimeScope
    {
        protected override void Awake()
        {
            base.Awake();
            EnsureMasterDataImported();
        }

        void EnsureMasterDataImported()
        {
            if (Parent is { Container: not null })
            {
                var importService = Parent.Container.Resolve<MasterDataImportService>();
                importService.Import();
            }
        }
    }
}

