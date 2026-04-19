using System.Threading;
using Cysharp.Threading.Tasks;
using Home.Service;
using Home.State;
using Root.Service;
using Root.View;
using TimerSetting.View;
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
        public void Init(
            HomeStateSetService homeStateSetService,
            SceneLoader sceneLoader,
            IDialogService dialogService)
        {
            _redecorateButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Redecorate));
            _closetButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Closet));
            _timerButton.onClick.AddListener(() =>
                OnTimerButtonClickedAsync(dialogService, sceneLoader, destroyCancellationToken).Forget());
        }

        async UniTaskVoid OnTimerButtonClickedAsync(
            IDialogService dialogService,
            SceneLoader sceneLoader,
            CancellationToken cancellationToken)
        {
            var result = await dialogService.OpenAsync<TimerSettingDialog>(cancellationToken);
            if (result == DialogResult.Ok)
            {
                sceneLoader.Load(Const.SceneName.Timer);
            }
        }
    }
}
