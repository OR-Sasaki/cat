using Home.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Starter
{
    /// <summary>
    /// シーン上のIsoDraggableViewにDIを行うスターター
    /// </summary>
    public class IsoDraggableStarter : IStartable
    {
        readonly IObjectResolver _resolver;

        public IsoDraggableStarter(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Start()
        {
            var draggables = Object.FindObjectsByType<IsoDraggableView>(FindObjectsSortMode.None);
            foreach (var draggable in draggables)
            {
                _resolver.Inject(draggable);
            }
        }
    }
}
