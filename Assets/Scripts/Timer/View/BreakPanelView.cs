using Root.Service;
using Timer.Service;
using Timer.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Timer.View
{
    public class BreakPanelView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _timerText;
        [SerializeField] TextMeshProUGUI _messageText;
        [SerializeField] TextMeshProUGUI _setsText;
        [SerializeField] TextMeshProUGUI _totalFocusTimeText;
        [SerializeField] Button _focusButton;
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
            _messageText.gameObject.SetActive(false);
            _resumeButton.gameObject.SetActive(false);

            _focusButton.onClick.AddListener(OnFocusButtonClicked);
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
            if (_state.CurrentPhase != PomodoroPhase.Break) return;

            var displaySeconds = Mathf.Max(0f, remainingSeconds);
            var minutes = Mathf.FloorToInt(displaySeconds / 60f);
            var seconds = Mathf.FloorToInt(displaySeconds % 60f);
            _timerText.text = $"{minutes:D2}:{seconds:D2}";

            _setsText.text = $"{_state.RemainingSets}";
            var totalMinutes = Mathf.FloorToInt(_state.TotalFocusTime / 60f);
            var totalSeconds = Mathf.FloorToInt(_state.TotalFocusTime % 60f);
            _totalFocusTimeText.text = $"{totalMinutes:D2}:{totalSeconds:D2}";
        }

        void OnTimerExpired()
        {
            if (_state.CurrentPhase != PomodoroPhase.Break) return;
            _focusButton.gameObject.SetActive(true);
            _messageText.gameObject.SetActive(true);
            _messageText.text = "集中しよう";
        }

        void OnPauseChanged(bool paused)
        {
            _pauseButton.gameObject.SetActive(!paused);
            _resumeButton.gameObject.SetActive(paused);
        }

        void OnFocusButtonClicked()
        {
            _service.TransitionToFocus();
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
