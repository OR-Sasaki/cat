using System;
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
        readonly ITimerYarnRewardService _timerYarnRewardService;
        readonly ScreenSleepService _screenSleepService;
        readonly IClock _clock;
        readonly CancellationToken _cancellationToken;

        float _focusSeconds;
        float _breakSeconds;
        bool _isRunning;
        int _flushedSeconds;

        /// バックグラウンドへ遷移した時刻。復帰時の経過反映に使う (null = フォアグラウンド)
        DateTimeOffset? _backgroundedAt;

        [Inject]
        public PomodoroService(
            PomodoroState state,
            PlayerPrefsService playerPrefsService,
            ITimerRecordService timerRecordService,
            ITimerYarnRewardService timerYarnRewardService,
            ScreenSleepService screenSleepService,
            IClock clock,
            CancellationToken cancellationToken)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            _timerRecordService = timerRecordService;
            _timerYarnRewardService = timerYarnRewardService;
            _screenSleepService = screenSleepService;
            _clock = clock;
            _cancellationToken = cancellationToken;
        }

        /// タイマー設定を読み込み、集中フェーズでタイマーを開始する
        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            // 再起動時の不変量 (_flushedSeconds <= floor(TotalFocusTime)) を維持するため、
            // PomodoroState.Setup による TotalFocusTime = 0 と同フレームで 0 化する。
            _flushedSeconds = 0;
            _backgroundedAt = null;

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

            // 計測中は画面を消灯させない (脱出経路での復帰は RunTimerLoopAsync の finally が担う)
            _screenSleepService.PreventSleep();

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
                // Complete の購読者 (完了演出) が結果を読めるよう、フェーズ通知より先に付与する。
                // 日跨ぎセッションでも上限はこの時点 (= タイマー終了時点) の日付で判定される
                _state.SetYarnReward(
                    _timerYarnRewardService.GrantForCompletedSession(RewardableFocusSeconds()));
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
            // 停止中は端末設定どおりに消灯させる
            _screenSleepService.AllowSleep();
        }

        /// タイマーを再開する
        public void Resume()
        {
            _state.SetPaused(false);
            _screenSleepService.PreventSleep();
        }

        /// 外部 (TimerLifecycleManager 等) からの確定要求。冪等。
        public void RequestFlush()
        {
            Flush();
        }

        /// アプリがバックグラウンドへ遷移した。集中時間を記録に確定し、復帰時の経過計算用に時刻を控える
        public void NotifyEnteredBackground()
        {
            Flush();

            if (!_isRunning || _state.IsPaused) return;
            _backgroundedAt = _clock.UtcNow;
        }

        /// アプリがフォアグラウンドへ復帰した。サスペンド中は Time.deltaTime が進まないため、
        /// 欠けた分を壁時計 (IClock) で補う。
        /// 反映量は現フェーズの残り時間を上限とする — 就寝などで長時間離れた分が集中記録に乗らないようにする。
        /// フェーズ送りはユーザー操作が起点なので、0 到達 (IsTimerExpired) まで進めれば足りる
        public void NotifyReturnedToForeground()
        {
            var backgroundedAt = _backgroundedAt;
            _backgroundedAt = null;

            if (backgroundedAt == null) return;
            if (!_isRunning || _state.IsPaused) return;

            var elapsed = (float)(_clock.UtcNow - backgroundedAt.Value).TotalSeconds;
            var applicable = Mathf.Min(elapsed, Mathf.Max(_state.RemainingSeconds, 0f));
            if (applicable <= 0f) return;

            AdvanceBy(applicable);
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

                    // バックグラウンド中はフレーム更新を止め、復帰時に壁時計でまとめて反映する。
                    // 実機では再開まで player loop 自体が止まるが、フレームが回り続ける環境
                    // (エディタ / Standalone の runInBackground) での二重計上を防ぐ
                    if (_backgroundedAt != null) continue;

                    AdvanceBy(Time.deltaTime);
                }
            }
            finally
            {
                // シーン破棄・キャンセル・例外いずれの脱出経路でも確定する
                Flush();
                _screenSleepService.AllowSleep();
            }
        }

        /// 経過秒数をタイマーへ反映する。毎フレーム更新とバックグラウンド復帰の補正で共用する
        void AdvanceBy(float deltaSeconds)
        {
            var remaining = _state.RemainingSeconds - deltaSeconds;

            // 集中フェーズ中は合計集中時間を累算
            if (_state.CurrentPhase == PomodoroPhase.Focus)
            {
                _state.SetTotalFocusTime(_state.TotalFocusTime + deltaSeconds);
            }

            // タイマー0到達の検知（残時間更新より先に判定し、ビュー側のちらつきを防止）
            if (!_state.IsTimerExpired && remaining <= 0f)
            {
                _state.SetTimerExpired(true);
            }

            _state.SetRemainingSeconds(remaining);
        }

        /// 報酬の対象になる集中秒数。
        /// タイマーが 0 に到達してもユーザーが休憩ボタンを押すまで Focus のまま TotalFocusTime は伸び続けるため、
        /// 放置した分が報酬に乗らないよう設定どおりの集中時間 (集中時間 × セット数) で頭打ちにする。
        float RewardableFocusSeconds()
        {
            return Mathf.Min(_state.TotalFocusTime, _focusSeconds * _state.TotalSets);
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
