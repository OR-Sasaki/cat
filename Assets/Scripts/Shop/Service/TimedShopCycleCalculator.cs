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

            var intervalSeconds = (long)interval.TotalSeconds;
            var epochSeconds = utcNow.ToUnixTimeSeconds();

            // Floor division (handle negative epoch correctly though irrelevant for current dates)
            var cycleId = epochSeconds >= 0
                ? epochSeconds / intervalSeconds
                : -((-epochSeconds + intervalSeconds - 1) / intervalSeconds);

            var cycleStartSeconds = cycleId * intervalSeconds;
            var cycleStart = DateTimeOffset.FromUnixTimeSeconds(cycleStartSeconds);
            var nextUpdate = cycleStart + interval;

            var remaining = nextUpdate - utcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            if (remaining >= interval) remaining = interval - TimeSpan.FromTicks(1);

            var seed = (int)(cycleId & 0xFFFFFFFFL) ^ (int)((cycleId >> 32) & 0xFFFFFFFFL);

            return new TimedShopCycleSnapshot(cycleId, cycleStart, nextUpdate, remaining, seed);
        }
    }
}
