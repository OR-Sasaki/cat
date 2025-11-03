using Home.Service;
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
        public void Init(HomeFooterService homeFooterService)
        {
            _redecorateButton.onClick.AddListener(() => homeFooterService.NavigateToScene(Const.SceneName.Redecorate));
            _closetButton.onClick.AddListener(() => homeFooterService.NavigateToScene(Const.SceneName.Closet));
            _timerButton.onClick.AddListener(() => homeFooterService.NavigateToScene(Const.SceneName.Timer));
            _shopButton.onClick.AddListener(() => homeFooterService.NavigateToScene(Const.SceneName.Shop));
            _historyButton.onClick.AddListener(() => homeFooterService.NavigateToScene(Const.SceneName.History));
        }
    }
}

