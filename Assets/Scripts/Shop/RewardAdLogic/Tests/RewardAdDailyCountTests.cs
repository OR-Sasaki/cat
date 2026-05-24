#nullable enable

using NUnit.Framework;

namespace Shop.RewardAd.Tests
{
    public class RewardAdDailyCountTests
    {
        const int DefaultCap = 5;

        [Test]
        [Description("daily_cap が null・0・負値のときは既定値を適用する")]
        public void ResolveDailyCap_NullOrNonPositive_UsesDefault()
        {
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(null, DefaultCap));
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(0, DefaultCap));
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(-3, DefaultCap));
        }

        [Test]
        [Description("daily_cap が正値のときはその値をそのまま使う")]
        public void ResolveDailyCap_Positive_UsesValue()
        {
            Assert.AreEqual(3, RewardAdDailyCount.ResolveDailyCap(3, DefaultCap));
        }

        [Test]
        [Description("上限未指定（null）のときは既定値を分母として残数を計算する")]
        public void ComputeRemaining_NullCap_UsesDefault()
        {
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ComputeRemaining(0, null, DefaultCap));
        }

        [Test]
        [Description("消化数が上限と等しいとき残数は 0 になる")]
        public void ComputeRemaining_AtCap_IsZero()
        {
            Assert.AreEqual(0, RewardAdDailyCount.ComputeRemaining(3, 3, DefaultCap));
        }

        [Test]
        [Description("消化数が上限を超えても残数は 0 に下限クランプされる")]
        public void ComputeRemaining_OverCap_ClampedToZero()
        {
            Assert.AreEqual(0, RewardAdDailyCount.ComputeRemaining(10, 3, DefaultCap));
        }

        [Test]
        [Description("一部消化時は「上限 - 消化数」を残数として返す")]
        public void ComputeRemaining_PartiallyConsumed()
        {
            Assert.AreEqual(2, RewardAdDailyCount.ComputeRemaining(3, 5, DefaultCap));
        }

        [Test]
        [Description("スナップショット無し（初回起動）は全カウント 0 とし、再永続化を要求する")]
        public void Reconcile_NoSnapshot_ResetsAllToZeroAndRequiresPersist()
        {
            var result = RewardAdDailyCount.Reconcile(null, "2026-05-23", new uint[] { 1, 2 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(0, result.Counts[1]);
            Assert.AreEqual(0, result.Counts[2]);
        }

        [Test]
        [Description("Version 不一致のときは全カウントを 0 にリセットする")]
        public void Reconcile_VersionMismatch_Resets()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion + 1,
                JstDate = "2026-05-23",
                Entries = new[] { new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = 4 } }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(0, result.Counts[1]);
        }

        [Test]
        [Description("保存日付が現在 JST 日付と異なる（日付跨ぎ）ときは全カウントを 0 にリセットする")]
        public void Reconcile_DateMismatch_Resets()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = "2026-05-22",
                Entries = new[] { new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = 4 } }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(0, result.Counts[1]);
        }

        [Test]
        [Description("同一日付・同一 Version のときは保存済みカウントを復元する")]
        public void Reconcile_SameDate_RestoresCounts()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = "2026-05-23",
                Entries = new[]
                {
                    new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = 2 },
                    new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 2, Count = 0 }
                }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1, 2 });

            Assert.IsFalse(result.RequiresPersist);
            Assert.AreEqual(2, result.Counts[1]);
            Assert.AreEqual(0, result.Counts[2]);
        }

        [Test]
        [Description("マスターに存在しない productId のエントリは破棄し、再永続化を要求する")]
        public void Reconcile_DropsProductIdsAbsentFromMaster()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = "2026-05-23",
                Entries = new[]
                {
                    new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = 2 },
                    new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 99, Count = 5 } // master に存在しない
                }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(2, result.Counts[1]);
            Assert.IsFalse(result.Counts.ContainsKey(99));
        }

        [Test]
        [Description("壊れた負値カウントは 0 に補正し、自己修復のため再永続化を要求する")]
        public void Reconcile_NegativeCount_ClampedToZeroAndRequiresPersist()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = "2026-05-23",
                Entries = new[] { new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = -4 } }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(0, result.Counts[1]);
        }

        [Test]
        [Description("スナップショットに無い新規マスター productId は 0 から開始する（再永続化は不要）")]
        public void Reconcile_NewMasterProduct_StartsAtZero()
        {
            var snapshot = new RewardAdDailyCountSnapshot
            {
                Version = RewardAdDailyCountSnapshot.CurrentVersion,
                JstDate = "2026-05-23",
                Entries = new[] { new RewardAdDailyCountSnapshot.DailyCountEntry { ProductId = 1, Count = 2 } }
            };

            var result = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1, 2 });

            Assert.IsFalse(result.RequiresPersist);
            Assert.AreEqual(2, result.Counts[1]);
            Assert.AreEqual(0, result.Counts[2]);
        }

        [Test]
        [Description("BuildSnapshot で生成したスナップショットは Reconcile で元のカウントに復元できる（往復整合）")]
        public void BuildSnapshot_RoundTrips()
        {
            var counts = new System.Collections.Generic.Dictionary<uint, int> { { 1, 3 }, { 2, 0 } };

            var snapshot = RewardAdDailyCount.BuildSnapshot(counts, "2026-05-23");

            Assert.AreEqual(RewardAdDailyCountSnapshot.CurrentVersion, snapshot.Version);
            Assert.AreEqual("2026-05-23", snapshot.JstDate);
            Assert.AreEqual(2, snapshot.Entries.Length);

            var reconciled = RewardAdDailyCount.Reconcile(snapshot, "2026-05-23", new uint[] { 1, 2 });
            Assert.AreEqual(3, reconciled.Counts[1]);
            Assert.AreEqual(0, reconciled.Counts[2]);
        }
    }
}
