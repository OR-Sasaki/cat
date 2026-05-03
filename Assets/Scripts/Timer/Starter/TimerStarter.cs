using System;
using System.Threading;
using Timer.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Timer.Starter
{
    public class TimerStarter : IStartable
    {
        readonly PomodoroService _pomodoroService;
        readonly CancellationToken _cancellationToken;

        [Inject]
        public TimerStarter(PomodoroService pomodoroService, CancellationToken cancellationToken)
        {
            _pomodoroService = pomodoroService;
            _cancellationToken = cancellationToken;
        }

        public async void Start()
        {
            try
            {
                await _pomodoroService.StartAsync(_cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // シーン破棄によるキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerStarter] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
