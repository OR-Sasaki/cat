using Root.Service;
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

        [Inject]
        public void Construct(PomodoroState state, PomodoroService service, SceneLoader sceneLoader)
        {
            _state = state;
            _service = service;
            _sceneLoader = sceneLoader;
        }

        void Start()
        {
            _breakButton.gameObject.SetActive(false);
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
            _breakButton.gameObject.SetActive(false);
            _messageText.gameObject.SetActive(false);
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
