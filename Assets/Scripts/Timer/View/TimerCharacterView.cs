using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.View
{
    public class TimerCharacterView : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField, Range(0f, 1f)] float _slowestRunAnimationSpeed = 0.55f;

        static readonly int RunHash = Animator.StringToHash("Run");
        static readonly int RestHash = Animator.StringToHash("Rest");
        static readonly int CompleteHash = Animator.StringToHash("Complete");

        PomodoroState _state;
        BackgroundScrollView _backgroundScroll;
        bool _isResting;

        [Inject]
        public void Construct(PomodoroState state, BackgroundScrollView backgroundScroll)
        {
            _state = state;
            _backgroundScroll = backgroundScroll;
        }

        void Start()
        {
            _state.OnPhaseChanged += OnPhaseChanged;
            _state.OnPauseChanged += OnPauseChanged;
            _backgroundScroll.BreakScrollStopped += OnBreakScrollStopped;

            // 初期状態: 走行アニメーション
            if (_animator != null)
            {
                _animator.SetTrigger(RunHash);
            }
        }

        void OnDestroy()
        {
            if (_backgroundScroll != null)
            {
                _backgroundScroll.BreakScrollStopped -= OnBreakScrollStopped;
            }
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
            _state.OnPauseChanged -= OnPauseChanged;
        }

        void Update()
        {
            // 走行中は背景スクロールの速度係数に脚の速さを同期させる
            // （休憩開始の減速・集中再開の加速でキャラクターと背景が一体に見える）
            if (_animator == null || _isResting || _state.IsPaused) return;
            if (_state.CurrentPhase == PomodoroPhase.Complete) return;

            _animator.speed = Mathf.Lerp(
                _slowestRunAnimationSpeed, 1f, _backgroundScroll.CurrentSpeedFactor);
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            if (_animator == null) return;

            switch (phase)
            {
                case PomodoroPhase.Focus:
                    // 休憩明け: 起き上がって走行再開
                    _isResting = false;
                    _animator.speed = _state.IsPaused ? 0f : 1f;
                    _animator.SetBool(RestHash, false);
                    _animator.SetTrigger(RunHash);
                    break;
                case PomodoroPhase.Break:
                    // 走行のまま減速し、背景スクロール停止と同時に寝そべりへ移る
                    break;
                case PomodoroPhase.Complete:
                    _animator.speed = 1f;
                    _animator.SetTrigger(CompleteHash);
                    break;
            }
        }

        void OnBreakScrollStopped()
        {
            if (_animator == null) return;
            if (_state.CurrentPhase != PomodoroPhase.Break) return;

            _isResting = true;
            _animator.speed = _state.IsPaused ? 0f : 1f;
            _animator.SetBool(RestHash, true);
        }

        void OnPauseChanged(bool paused)
        {
            if (_animator == null) return;
            _animator.speed = paused ? 0f : 1f;
        }
    }
}
