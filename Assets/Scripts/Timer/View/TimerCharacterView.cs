using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.View
{
    public class TimerCharacterView : MonoBehaviour
    {
        [SerializeField] Animator _animator;

        static readonly int RunHash = Animator.StringToHash("Run");
        static readonly int CompleteHash = Animator.StringToHash("Complete");

        PomodoroState _state;

        [Inject]
        public void Construct(PomodoroState state)
        {
            _state = state;
        }

        void Start()
        {
            _state.OnPhaseChanged += OnPhaseChanged;
            _state.OnPauseChanged += OnPauseChanged;

            // 初期状態: 走行アニメーション
            if (_animator != null)
            {
                _animator.SetTrigger(RunHash);
            }
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
            _state.OnPauseChanged -= OnPauseChanged;
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            if (_animator == null) return;

            switch (phase)
            {
                case PomodoroPhase.Focus:
                case PomodoroPhase.Break:
                    _animator.speed = _state.IsPaused ? 0f : 1f;
                    _animator.SetTrigger(RunHash);
                    break;
                case PomodoroPhase.Complete:
                    _animator.speed = 1f;
                    _animator.SetTrigger(CompleteHash);
                    break;
            }
        }

        void OnPauseChanged(bool paused)
        {
            if (_animator == null) return;
            _animator.speed = paused ? 0f : 1f;
        }
    }
}
