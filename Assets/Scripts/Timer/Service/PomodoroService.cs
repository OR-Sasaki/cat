using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Timer.State;
using TimerSetting.State;
using UnityEngine;
using VContainer;

namespace Timer.Service
{
    public class PomodoroService
    {
        readonly PomodoroState _state;
        readonly PlayerPrefsService _playerPrefsService;
        readonly CancellationToken _cancellationToken;

        float _focusSeconds;
        float _breakSeconds;
        bool _isRunning;

        [Inject]
        public PomodoroService(PomodoroState state, PlayerPrefsService playerPrefsService, CancellationToken cancellationToken)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            _cancellationToken = cancellationToken;
        }

        /// タイマー設定を読み込み、集中フェーズでタイマーを開始する
        public async UniTask StartAsync()
        {
            var settings = _playerPrefsService.Load<TimerSettingData>(PlayerPrefsKey.TimerSetting);
            if (settings == null)
            {
                settings = new TimerSettingData();
            }

            _focusSeconds = settings.focusTime * 60f;
            _breakSeconds = settings.breakTime * 60f;

            _state.Setup(settings.sets);
            _state.SetPhase(PomodoroPhase.Focus);
            _state.SetRemainingSeconds(_focusSeconds);
            _state.SetTimerExpired(false);

            _isRunning = true;
            await RunTimerLoopAsync();
        }

        /// 休憩フェーズに遷移する
        public void TransitionToBreak()
        {
            if (_state.CurrentPhase != PomodoroPhase.Focus)
            {
                Debug.LogWarning("[PomodoroService] TransitionToBreak: 事前条件不成立（Focus が必要）");
                return;
            }

            // 最終セットなら完了へ直接遷移
            if (_state.CurrentSet == _state.TotalSets)
            {
                _isRunning = false;
                _state.SetPhase(PomodoroPhase.Complete);
                return;
            }

            _state.SetTimerExpired(false);
            _state.SetRemainingSeconds(_breakSeconds);
            _state.SetPhase(PomodoroPhase.Break);
        }

        /// 次のセットの集中フェーズに遷移する
        public void TransitionToFocus()
        {
            if (_state.CurrentPhase != PomodoroPhase.Break)
            {
                Debug.LogWarning("[PomodoroService] TransitionToFocus: 事前条件不成立（Break が必要）");
                return;
            }

            _state.SetCurrentSet(_state.CurrentSet + 1);
            _state.SetTimerExpired(false);
            _state.SetRemainingSeconds(_focusSeconds);
            _state.SetPhase(PomodoroPhase.Focus);
        }

        /// タイマーを一時停止する
        public void Pause()
        {
            _state.SetPaused(true);
        }

        /// タイマーを再開する
        public void Resume()
        {
            _state.SetPaused(false);
        }

        async UniTask RunTimerLoopAsync()
        {
            while (_isRunning && !_cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, _cancellationToken);

                if (_state.IsPaused) continue;
                if (_state.CurrentPhase == PomodoroPhase.Complete) break;

                var delta = Time.deltaTime;
                var remaining = _state.RemainingSeconds - delta;
                _state.SetRemainingSeconds(remaining);

                // 集中フェーズ中は合計集中時間を累算
                if (_state.CurrentPhase == PomodoroPhase.Focus)
                {
                    _state.SetTotalFocusTime(_state.TotalFocusTime + delta);
                }

                // タイマー0到達の検知
                if (!_state.IsTimerExpired && remaining <= 0f)
                {
                    _state.SetTimerExpired(true);
                }
            }
        }
    }
}
