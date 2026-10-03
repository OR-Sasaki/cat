#nullable enable

using System;
using Root.State;
using Shop.RewardAd;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    /// タイマー完了時に集中時間ぶんの毛糸玉を付与し、日次上限を管理するサービス。
    /// 1 日の区切りはショップのリワード広告日次上限と同じ JST(UTC+9) 0:00 境界を使う
    public class TimerYarnRewardService : ITimerYarnRewardService
    {
        readonly TimerYarnRewardState _state;
        readonly IUserPointService _userPointService;
        readonly PlayerPrefsService _playerPrefsService;
        readonly IClock _clock;

        [Inject]
        public TimerYarnRewardService(
            TimerYarnRewardState state,
            IUserPointService userPointService,
            PlayerPrefsService playerPrefsService,
            IClock clock)
        {
            _state = state;
            _userPointService = userPointService;
            _playerPrefsService = playerPrefsService;
            _clock = clock;
            Load();
        }

        public TimerYarnRewardResult GrantForCompletedSession(float focusSeconds)
        {
            // 日付を跨いだセッションでも、上限はこの呼び出し時点 (= タイマー終了時点) の日付で判定する
            var today = CurrentJstDate();
            RollOverIfNeeded(today);

            var earnable = TimerYarnRewardRules.CalculateEarnable(focusSeconds);
            var granted = TimerYarnRewardRules.ClampToDailyCap(earnable, _state.EarnedYarn);

            if (granted <= 0)
            {
                return BuildResult(0, earnable);
            }

            var addResult = _userPointService.AddYarn(granted);
            if (!addResult.IsSuccess)
            {
                // 残高への加算が通らなかった場合は当日カウントも進めず、獲得 0 として扱う
                Debug.LogError($"[TimerYarnRewardService] 毛糸玉の加算に失敗しました: {addResult.Error}");
                return BuildResult(0, earnable);
            }

            _state.Set(today, _state.EarnedYarn + granted);
            Save();
            return BuildResult(granted, earnable);
        }

        string CurrentJstDate() => JstDateHelper.ToJstDateString(_clock.UtcNow);

        /// 保持している日付が当日と違えばカウントを 0 に戻す。
        /// 永続化は付与時にまとめて行い、ここでは書き込まない (次回起動時は Load で同じ突き合わせが走る)
        void RollOverIfNeeded(string today)
        {
            if (_state.JstDate == today) return;
            _state.Set(today, 0);
        }

        TimerYarnRewardResult BuildResult(int granted, int earnable)
        {
            return new TimerYarnRewardResult(
                granted, earnable, _state.EarnedYarn, TimerYarnRewardRules.DailyCap);
        }

        void Load()
        {
            var today = CurrentJstDate();

            TimerYarnRewardSnapshot? snapshot;
            try
            {
                snapshot = _playerPrefsService.Load<TimerYarnRewardSnapshot>(PlayerPrefsKey.TimerYarnReward);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerYarnRewardService] {e.Message}\n{e.StackTrace}");
                _state.Set(today, 0);
                return;
            }

            // 保存が無い / 版が違う / 日付が変わっている場合は当日 0 個から数え直す
            if (snapshot is null
                || snapshot.Version != TimerYarnRewardSnapshot.CurrentVersion
                || snapshot.JstDate != today)
            {
                _state.Set(today, 0);
                return;
            }

            _state.Set(today, Math.Max(0, snapshot.EarnedYarn));
        }

        void Save()
        {
            try
            {
                var snapshot = new TimerYarnRewardSnapshot
                {
                    Version = TimerYarnRewardSnapshot.CurrentVersion,
                    JstDate = _state.JstDate,
                    EarnedYarn = _state.EarnedYarn,
                };
                _playerPrefsService.Save(PlayerPrefsKey.TimerYarnReward, snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerYarnRewardService] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
