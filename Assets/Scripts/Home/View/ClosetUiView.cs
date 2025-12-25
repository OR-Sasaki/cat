using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Home.View
{
    public class ClosetUiView : UiView
    {
        [SerializeField] Button _backButton;

        [Inject]
        public void Init(HomeFooterService homeFooterService)
        {
            _backButton.onClick.AddListener(() => homeFooterService.SetState(HomeState.State.Home));
        }
    }
}
