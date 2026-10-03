using System;
using UnityEngine;

namespace Timer.Service
{
    /// タイマー計測中の画面自動スリープを抑止する。
    /// Lifetime.Scoped で登録し、シーン破棄時の Dispose で必ず端末設定へ戻す
    /// (抑止したまま別シーンへ移ると画面が消灯しなくなるため)。
    public sealed class ScreenSleepService : IDisposable
    {
        bool _isPrevented;

        /// 自動スリープを抑止する。冪等。
        public void PreventSleep()
        {
            if (_isPrevented) return;

            _isPrevented = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// 端末のシステム設定による自動スリープへ戻す。冪等。
        public void AllowSleep()
        {
            if (!_isPrevented) return;

            _isPrevented = false;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        public void Dispose()
        {
            AllowSleep();
        }
    }
}
