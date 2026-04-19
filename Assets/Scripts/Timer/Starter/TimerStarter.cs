using System;
using Timer.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Timer.Starter
{
    public class TimerStarter : IStartable
    {
        readonly PomodoroService _pomodoroService;

        [Inject]
        public TimerStarter(PomodoroService pomodoroService)
        {
            _pomodoroService = pomodoroService;
        }

        public async void Start()
        {
            try
            {
                await _pomodoroService.StartAsync();
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
