using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Root.View;
using Timer.Service;
using Timer.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Timer.View
{
    public class FocusPanelView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _timerText;
        [SerializeField] TextMeshProUGUI _messageText;
        [SerializeField] TextMeshProUGUI _setsText;
        [SerializeField] TextMeshProUGUI _totalFocusTimeText;
        [SerializeField] Button _breakButton;
        [SerializeField] Button _pauseButton;
        [SerializeField] Button _resumeButton;
        [SerializeField] Button _homeButton;

        PomodoroState _state;
        PomodoroService _service;
        SceneLoader _sceneLoader;
        IDialogService _dialogService;

        /// HasOpenDialog はプレハブのロード完了後に true になるため、
        /// ロード中の連打で確認ダイアログが二重に開くのを自前のフラグで防ぐ
        bool _isConfirmingBreak;

        [Inject]
        public void Construct(
            PomodoroState state,
            PomodoroService service,
            SceneLoader sceneLoader,
            IDialogService dialogService)
        {
            _state = state;
            _service = service;
            _sceneLoader = sceneLoader;
            _dialogService = dialogService;
        }

        void Start()
        {
            _messageText.gameObject.SetActive(false);
            _resumeButton.gameObject.SetActive(false);

            _breakButton.onClick.AddListener(OnBreakButtonClicked);
            _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            _homeButton.onClick.AddListener(OnHomeButtonClicked);

            _state.OnTimerUpdated += OnTimerUpdated;
            _state.OnTimerExpired += OnTimerExpired;
            _state.OnPauseChanged += OnPauseChanged;
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnTimerUpdated -= OnTimerUpdated;
            _state.OnTimerExpired -= OnTimerExpired;
            _state.OnPauseChanged -= OnPauseChanged;
        }

        void OnTimerUpdated(float remainingSeconds)
        {
            if (_state.CurrentPhase != PomodoroPhase.Focus) return;

            if (_state.IsTimerExpired)
            {
                var elapsed = Mathf.Abs(remainingSeconds);
                var minutes = Mathf.FloorToInt(elapsed / 60f);
                var seconds = Mathf.FloorToInt(elapsed % 60f);
                _timerText.text = $"+{minutes:D2}:{seconds:D2}";
            }
            else
            {
                var minutes = Mathf.FloorToInt(remainingSeconds / 60f);
                var seconds = Mathf.FloorToInt(remainingSeconds % 60f);
                _timerText.text = $"{minutes:D2}:{seconds:D2}";
            }

            _setsText.text = $"{_state.RemainingSets}";
            var totalMinutes = Mathf.FloorToInt(_state.TotalFocusTime / 60f);
            var totalSeconds = Mathf.FloorToInt(_state.TotalFocusTime % 60f);
            _totalFocusTimeText.text = $"{totalMinutes:D2}:{totalSeconds:D2}";
        }

        void OnTimerExpired()
        {
            if (_state.CurrentPhase != PomodoroPhase.Focus) return;
            _breakButton.gameObject.SetActive(true);
            _messageText.gameObject.SetActive(true);
            _messageText.text = "休憩しよう";
        }

        void OnPauseChanged(bool paused)
        {
            _pauseButton.gameObject.SetActive(!paused);
            _resumeButton.gameObject.SetActive(paused);
        }

        void OnBreakButtonClicked()
        {
            // 設定どおりの集中時間を終えてからの押下は、そのまま休憩へ進む
            if (_state.IsTimerExpired)
            {
                _service.TransitionToBreak();
                return;
            }

            ConfirmBreakAsync(destroyCancellationToken).Forget();
        }

        /// 集中時間が残っているうちに休憩ボタンが押された。誤操作でないか確認してから遷移する
        async UniTaskVoid ConfirmBreakAsync(CancellationToken cancellationToken)
        {
            if (_isConfirmingBreak || _dialogService.HasOpenDialog) return;

            _isConfirmingBreak = true;
            DialogResult result;
            try
            {
                result = await _dialogService.OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                    new CommonConfirmDialogArgs(
                        Title: "休憩確認",
                        Message: "まだ集中時間が残っていますが、休憩に入りますか？"),
                    cancellationToken);
            }
            finally
            {
                _isConfirmingBreak = false;
            }

            if (result != DialogResult.Ok) return;

            _service.TransitionToBreak();
        }

        void OnPauseButtonClicked()
        {
            _service.Pause();
        }

        void OnResumeButtonClicked()
        {
            _service.Resume();
        }

        void OnHomeButtonClicked()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }
    }
}
