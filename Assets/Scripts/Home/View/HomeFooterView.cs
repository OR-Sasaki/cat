using Home.Service;
using Home.State;
using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Home.View
{
    public class HomeFooterView : MonoBehaviour
    {
        [SerializeField] Button _redecorateButton;
        [SerializeField] Button _closetButton;
        [SerializeField] Button _timerButton;
        [SerializeField] Button _shopButton;
        [SerializeField] Button _historyButton;

        [Inject]
        public void Init(HomeStateSetService homeStateSetService, SceneLoader sceneLoader)
        {
            _redecorateButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Redecorate));
            _closetButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Closet));
            _timerButton.onClick.AddListener(() => sceneLoader.Load(Const.SceneName.Timer));
        }
    }
}

