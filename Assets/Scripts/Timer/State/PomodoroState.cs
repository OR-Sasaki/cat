#nullable enable
using System;

namespace Timer.State
{
    public enum PomodoroPhase
    {
        Focus,
        Break,
        Complete
    }

    public class PomodoroState
    {
        /// 現在のフェーズ
        public PomodoroPhase CurrentPhase { get; private set; }

        /// タイマー残り時間（秒）。0以下の場合は超過時間
        public float RemainingSeconds { get; private set; }

        /// タイマーが0に到達したか
        public bool IsTimerExpired { get; private set; }

        /// 現在のセット番号（1始まり）
        public int CurrentSet { get; private set; }

        /// 総セット数
        public int TotalSets { get; private set; }

        /// 残りセット数
        public int RemainingSets => TotalSets - CurrentSet;

        /// 合計集中時間（秒）
        public float TotalFocusTime { get; private set; }

        /// 一時停止中か
        public bool IsPaused { get; private set; }

        /// フェーズ変更イベント
        public event Action<PomodoroPhase>? OnPhaseChanged;

        /// タイマー更新イベント（毎フレーム）
        public event Action<float>? OnTimerUpdated;

        /// タイマー0到達イベント
        public event Action? OnTimerExpired;

        /// 一時停止状態変更イベント
        public event Action<bool>? OnPauseChanged;

        /// 総セット数を初期設定する
        public void Setup(int totalSets)
        {
            TotalSets = totalSets;
            CurrentSet = 1;
            TotalFocusTime = 0f;
            RemainingSeconds = 0f;
            IsTimerExpired = false;
            IsPaused = false;
        }

        public void SetPhase(PomodoroPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        public void SetRemainingSeconds(float seconds)
        {
            RemainingSeconds = seconds;
            OnTimerUpdated?.Invoke(seconds);
        }

        public void SetTimerExpired(bool expired)
        {
            if (IsTimerExpired == expired) return;
            IsTimerExpired = expired;
            if (expired)
            {
                OnTimerExpired?.Invoke();
            }
        }

        public void SetCurrentSet(int set)
        {
            CurrentSet = set;
        }

        public void SetTotalFocusTime(float time)
        {
            TotalFocusTime = time;
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            OnPauseChanged?.Invoke(paused);
        }
    }
}
