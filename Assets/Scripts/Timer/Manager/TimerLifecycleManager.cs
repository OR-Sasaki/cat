#nullable enable

using Timer.Service;
using UnityEngine;
using VContainer;

namespace Timer.Manager
{
    /// アプリのバックグラウンド遷移 / 終了通知を受け、
    /// PomodoroService の確定要求を呼び出すライフサイクル橋渡し役。
    /// PomodoroService は Pure C# クラスでありライフサイクルフックを持たないため、
    /// MonoBehaviour 側で OnApplicationPause / OnApplicationQuit を捕捉する。
    public class TimerLifecycleManager : MonoBehaviour
    {
        PomodoroService? _pomodoroService;

        [Inject]
        public void Construct(PomodoroService pomodoroService)
        {
            _pomodoroService = pomodoroService;
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause) return;
            _pomodoroService?.RequestFlush();
        }

        void OnApplicationQuit()
        {
            _pomodoroService?.RequestFlush();
        }
    }
}
