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
        public void Init(HomeStateSetService homeStateSetService)
        {
            _backButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Home));
        }
    }
}
