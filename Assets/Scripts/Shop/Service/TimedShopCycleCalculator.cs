#nullable enable

using System;

namespace Shop.Service
{
    public readonly struct TimedShopCycleSnapshot
    {
        public long CycleId { get; }
        public DateTimeOffset CycleStartUtc { get; }
        public DateTimeOffset NextUpdateAtUtc { get; }
        public TimeSpan Remaining { get; }
        public int Seed { get; }

        public TimedShopCycleSnapshot(
            long cycleId,
            DateTimeOffset cycleStartUtc,
            DateTimeOffset nextUpdateAtUtc,
            TimeSpan remaining,
            int seed)
        {
            CycleId = cycleId;
            CycleStartUtc = cycleStartUtc;
            NextUpdateAtUtc = nextUpdateAtUtc;
            Remaining = remaining;
            Seed = seed;
        }
    }

    public static class TimedShopCycleCalculator
    {
        public static TimedShopCycleSnapshot Calculate(DateTimeOffset utcNow, TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "interval must be positive.");

            // 秒単位で割ると 1 秒未満の interval が 0 へ切り捨てられゼロ除算になるため tick で計算する
            var intervalTicks = interval.Ticks;
            var elapsedTicks = (utcNow - DateTimeOffset.UnixEpoch).Ticks;

            var cycleId = elapsedTicks >= 0
                ? elapsedTicks / intervalTicks
                : -((-elapsedTicks + intervalTicks - 1) / intervalTicks);

            var cycleStart = DateTimeOffset.UnixEpoch + TimeSpan.FromTicks(cycleId * intervalTicks);
            var nextUpdate = cycleStart + interval;

            var remaining = nextUpdate - utcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            if (remaining >= interval) remaining = interval - TimeSpan.FromTicks(1);

            var seed = (int)(cycleId & 0xFFFFFFFFL) ^ (int)((cycleId >> 32) & 0xFFFFFFFFL);

            return new TimedShopCycleSnapshot(cycleId, cycleStart, nextUpdate, remaining, seed);
        }
    }
}
