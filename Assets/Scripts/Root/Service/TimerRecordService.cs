#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Root.State;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    public class TimerRecordService : ITimerRecordService
    {
        const string DateFormat = "yyyy-MM-dd";

        readonly TimerRecordState _state;
        readonly PlayerPrefsService _playerPrefsService;
        readonly IClock _clock;

        public event Action<DateTime, int, int>? FocusSecondsAdded;

        [Inject]
        public TimerRecordService(
            TimerRecordState state,
            PlayerPrefsService playerPrefsService,
            IClock clock)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            _clock = clock;
            Load();
        }

        public void AddSeconds(int seconds)
        {
            if (seconds <= 0) return;

            var today = GetToday();
            var current = _state.Get(today);
            var next = current + seconds;
            _state.Set(today, next);
            FireFocusSecondsAdded(today, seconds, next);
            Save();
        }

        public int GetSeconds(DateTime date) => _state.Get(date);

        public int GetMonthTotalSeconds(int year, int month)
        {
            var total = 0;
            foreach (var kvp in _state.GetAll())
            {
                if (kvp.Key.Year == year && kvp.Key.Month == month)
                {
                    total += kvp.Value;
                }
            }
            return total;
        }

        public int GetTodayTotalSeconds() => _state.Get(GetToday());

        public IReadOnlyDictionary<DateTime, int> GetAllRecords() => _state.GetAll();

        DateTime GetToday()
        {
            return _clock.UtcNow.LocalDateTime.Date;
        }

        void Load()
        {
            _state.Clear();

            TimerRecordSnapshot? snapshot;
            try
            {
                snapshot = _playerPrefsService.Load<TimerRecordSnapshot>(PlayerPrefsKey.TimerRecord);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerRecordService] {e.Message}\n{e.StackTrace}");
                return;
            }

            if (snapshot is null || snapshot.Version != TimerRecordSnapshot.CurrentVersion)
            {
                return;
            }

            if (snapshot.Entries is null) return;

            foreach (var entry in snapshot.Entries)
            {
                if (entry is null) continue;
                if (entry.Seconds <= 0) continue;
                if (string.IsNullOrEmpty(entry.Date)) continue;

                if (!DateTime.TryParseExact(
                        entry.Date,
                        DateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    Debug.LogWarning($"[TimerRecordService] Invalid date entry skipped: {entry.Date}");
                    continue;
                }

                _state.Set(date, entry.Seconds);
            }
        }

        void Save()
        {
            try
            {
                var records = _state.GetAll();
                var entries = new TimerRecordEntry[records.Count];
                var i = 0;
                foreach (var kvp in records)
                {
                    entries[i++] = new TimerRecordEntry
                    {
                        Date = kvp.Key.ToString(DateFormat, CultureInfo.InvariantCulture),
                        Seconds = kvp.Value,
                    };
                }

                var snapshot = new TimerRecordSnapshot
                {
                    Version = TimerRecordSnapshot.CurrentVersion,
                    Entries = entries,
                };
                _playerPrefsService.Save(PlayerPrefsKey.TimerRecord, snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TimerRecordService] {e.Message}\n{e.StackTrace}");
            }
        }

        void FireFocusSecondsAdded(DateTime date, int addedSeconds, int totalSeconds)
        {
            var handlers = FocusSecondsAdded;
            if (handlers is null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<DateTime, int, int>)handler)(date, addedSeconds, totalSeconds);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TimerRecordService] {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
