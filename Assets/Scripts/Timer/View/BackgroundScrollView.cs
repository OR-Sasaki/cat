using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.View
{
    public class BackgroundScrollView : MonoBehaviour
    {
        [SerializeField] Transform _background1;
        [SerializeField] Transform _background2;
        [SerializeField] float _scrollSpeed = 2f;
        [SerializeField] float _backgroundWidth = 20f;

        PomodoroState _state;
        bool _isScrolling = true;

        [Inject]
        public void Construct(PomodoroState state)
        {
            _state = state;
        }

        void Start()
        {
            _state.OnPhaseChanged += OnPhaseChanged;
            _state.OnPauseChanged += OnPauseChanged;
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
            _state.OnPauseChanged -= OnPauseChanged;
        }

        void Update()
        {
            if (!_isScrolling) return;

            var delta = _scrollSpeed * Time.deltaTime;
            _background1.Translate(Vector3.left * delta);
            _background2.Translate(Vector3.left * delta);

            if (_background1.localPosition.x <= -_backgroundWidth)
            {
                _background1.localPosition = new Vector3(
                    _background2.localPosition.x + _backgroundWidth,
                    _background1.localPosition.y,
                    _background1.localPosition.z
                );
            }

            if (_background2.localPosition.x <= -_backgroundWidth)
            {
                _background2.localPosition = new Vector3(
                    _background1.localPosition.x + _backgroundWidth,
                    _background2.localPosition.y,
                    _background2.localPosition.z
                );
            }
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            _isScrolling = phase != PomodoroPhase.Complete;
        }

        void OnPauseChanged(bool paused)
        {
            _isScrolling = !paused && _state.CurrentPhase != PomodoroPhase.Complete;
        }
    }
}
