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
        readonly ITimerRecordService _timerRecordService;
        readonly CancellationToken _cancellationToken;

        float _focusSeconds;
        float _breakSeconds;
        bool _isRunning;
        int _flushedSeconds;

        [Inject]
        public PomodoroService(
            PomodoroState state,
            PlayerPrefsService playerPrefsService,
            ITimerRecordService timerRecordService,
            CancellationToken cancellationToken)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            _timerRecordService = timerRecordService;
            _cancellationToken = cancellationToken;
        }

        /// タイマー設定を読み込み、集中フェーズでタイマーを開始する
        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            // 再起動時の不変量 (_flushedSeconds <= floor(TotalFocusTime)) を維持するため、
            // PomodoroState.Setup による TotalFocusTime = 0 と同フレームで 0 化する。
            _flushedSeconds = 0;

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
            await RunTimerLoopAsync(cancellationToken);
        }

        /// 休憩フェーズに遷移する
        public void TransitionToBreak()
        {
            if (_state.CurrentPhase != PomodoroPhase.Focus)
            {
                Debug.LogWarning("[PomodoroService] TransitionToBreak: 事前条件不成立（Focus が必要）");
                return;
            }

            // Focus 完了直後 / Complete 直前のいずれでも Flush で集中時間を確定する
            // 加算先は Flush 呼出時点のローカル日付 (日跨ぎセッションの開始日按分は行わない)
            Flush();

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

        /// 外部 (TimerLifecycleManager 等) からの確定要求。冪等。
        public void RequestFlush()
        {
            Flush();
        }

        async UniTask RunTimerLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    if (_state.IsPaused) continue;
                    if (_state.CurrentPhase == PomodoroPhase.Complete) break;

                    var delta = Time.deltaTime;
                    var remaining = _state.RemainingSeconds - delta;

                    // 集中フェーズ中は合計集中時間を累算
                    if (_state.CurrentPhase == PomodoroPhase.Focus)
                    {
                        _state.SetTotalFocusTime(_state.TotalFocusTime + delta);
                    }

                    // タイマー0到達の検知（残時間更新より先に判定し、ビュー側のちらつきを防止）
                    if (!_state.IsTimerExpired && remaining <= 0f)
                    {
                        _state.SetTimerExpired(true);
                    }

                    _state.SetRemainingSeconds(remaining);
                }
            }
            finally
            {
                // シーン破棄・キャンセル・例外いずれの脱出経路でも確定する
                Flush();
            }
        }

        /// 集中時間を記録に確定する。差分 (= floor(TotalFocusTime) - _flushedSeconds) のみを加算する。
        /// 差分が 0 以下なら呼出を省略する (要件 1.10)。冪等。
        void Flush()
        {
            var current = Mathf.FloorToInt(_state.TotalFocusTime);
            var delta = current - _flushedSeconds;
            if (delta <= 0) return;

            _timerRecordService.AddSeconds(delta);
            _flushedSeconds = current;
        }
    }
}
