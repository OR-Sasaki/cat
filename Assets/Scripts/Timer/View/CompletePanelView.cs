using Root.Service;
using Timer.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Timer.View
{
    public class CompletePanelView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _totalFocusTimeText;
        [SerializeField] Button _homeButton;

        PomodoroState _state;
        SceneLoader _sceneLoader;

        [Inject]
        public void Construct(PomodoroState state, SceneLoader sceneLoader)
        {
            _state = state;
            _sceneLoader = sceneLoader;
        }

        void Start()
        {
            _homeButton.onClick.AddListener(OnHomeButtonClicked);
            _state.OnPhaseChanged += OnPhaseChanged;

            if (_state.CurrentPhase == PomodoroPhase.Complete)
            {
                UpdateTotalFocusTimeText();
            }
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            if (phase != PomodoroPhase.Complete) return;
            UpdateTotalFocusTimeText();
        }

        void UpdateTotalFocusTimeText()
        {
            var totalMinutes = Mathf.FloorToInt(_state.TotalFocusTime / 60f);
            var totalSeconds = Mathf.FloorToInt(_state.TotalFocusTime % 60f);
            _totalFocusTimeText.text = $"{totalMinutes:D2}:{totalSeconds:D2}";
        }

        void OnHomeButtonClicked()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }
    }
}
