#nullable enable

using System.Collections.Generic;

namespace Shop.RewardAd
{
    /// 日次視聴回数に関する純粋計算ロジック。
    /// 状態を持たず、ShopService から委譲して使う（テスト容易性のため分離）。
    public static class RewardAdDailyCount
    {
        /// daily_cap の実効値を解決する（null・0 以下は既定値を適用）
        public static int ResolveDailyCap(int? dailyCap, int defaultCap)
        {
            if (!dailyCap.HasValue || dailyCap.Value <= 0)
                return defaultCap;
            return dailyCap.Value;
        }

        /// 残り視聴回数（下限 0）
        public static int ComputeRemaining(int count, int? dailyCap, int defaultCap)
        {
            var remaining = ResolveDailyCap(dailyCap, defaultCap) - count;
            return remaining < 0 ? 0 : remaining;
        }

        /// 永続化スナップショットを現在の有効商品集合・JST 日付と突き合わせて
        /// インメモリ用のカウント辞書を再構築する。
        /// Version 不一致・日付不一致・スナップショット無しなら全カウント 0。
        /// マスターに存在しない productId のエントリは破棄する。
        public static ReconcileResult Reconcile(
            RewardAdDailyCountSnapshot? snapshot,
            string currentJstDate,
            IReadOnlyCollection<uint> validProductIds)
        {
            var counts = new Dictionary<uint, int>(validProductIds.Count);
            foreach (var id in validProductIds)
                counts[id] = 0;

            var isFresh = snapshot != null
                          && snapshot.Version == RewardAdDailyCountSnapshot.CurrentVersion
                          && snapshot.JstDate == currentJstDate;

            if (!isFresh)
            {
                // Version 不一致 / 日付跨ぎ / スナップショット無し → 全 0 にして再永続化を要求
                return new ReconcileResult(counts, requiresPersist: true);
            }

            var pruned = false;
            if (snapshot!.Entries != null)
            {
                foreach (var entry in snapshot.Entries)
                {
                    if (counts.ContainsKey(entry.ProductId))
                        counts[entry.ProductId] = entry.Count < 0 ? 0 : entry.Count;
                    else
                        pruned = true; // マスターに存在しない productId は破棄
                }
            }

            return new ReconcileResult(counts, requiresPersist: pruned);
        }

        /// インメモリのカウント辞書から永続化スナップショットを構築する
        public static RewardAdDailyCountSnapshot BuildSnapshot(
            IReadOnlyDictionary<uint, int> counts,
            string currentJstDate)
        {
            var entries = new RewardAdDailyCountSnapshot.DailyCountEntry[counts.Count];
            var i = 0;
            foreach (var kv in counts)
            {
                entries[i++] = new RewardAdDailyCountSnapshot.DailyCountEntry
                {
                    ProductId = kv.Key,
                    Count = kv.Value
                };
            }

            return new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = currentJstDate,
                Entries = entries
            };
        }

        public readonly struct ReconcileResult
        {
            public readonly Dictionary<uint, int> Counts;
            public readonly bool RequiresPersist;

            public ReconcileResult(Dictionary<uint, int> counts, bool requiresPersist)
            {
                Counts = counts;
                RequiresPersist = requiresPersist;
            }
        }
    }
}
