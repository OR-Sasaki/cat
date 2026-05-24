#nullable enable

using NUnit.Framework;

namespace Shop.RewardAd.Tests
{
    public class RewardAdDailyCountTests
    {
        const int DefaultCap = 5;

        [Test]
        public void ResolveDailyCap_NullOrNonPositive_UsesDefault()
        {
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(null, DefaultCap));
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(0, DefaultCap));
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ResolveDailyCap(-3, DefaultCap));
        }

        [Test]
        public void ResolveDailyCap_Positive_UsesValue()
        {
            Assert.AreEqual(3, RewardAdDailyCount.ResolveDailyCap(3, DefaultCap));
        }

        [Test]
        public void ComputeRemaining_NullCap_UsesDefault()
        {
            Assert.AreEqual(DefaultCap, RewardAdDailyCount.ComputeRemaining(0, null, DefaultCap));
        }

        [Test]
        public void ComputeRemaining_AtCap_IsZero()
        {
            Assert.AreEqual(0, RewardAdDailyCount.ComputeRemaining(3, 3, DefaultCap));
        }

        [Test]
        public void ComputeRemaining_OverCap_ClampedToZero()
        {
            Assert.AreEqual(0, RewardAdDailyCount.ComputeRemaining(10, 3, DefaultCap));
        }

        [Test]
        public void ComputeRemaining_PartiallyConsumed()
        {
            Assert.AreEqual(2, RewardAdDailyCount.ComputeRemaining(3, 5, DefaultCap));
        }

        [Test]
        public void Reconcile_NoSnapshot_ResetsAllToZeroAndRequiresPersist()
        {
            var result = RewardAdDailyCount.Reconcile(null, "2026-05-23", new uint[] { 1, 2 });

            Assert.IsTrue(result.RequiresPersist);
            Assert.AreEqual(0, result.Counts[1]);
            Assert.AreEqual(0, result.Counts[2]);
        }

        [Test]
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

            Assert.IsTrue(result.RequiresPersist); // 余分エントリを破棄したので再永続化
            Assert.AreEqual(2, result.Counts[1]);
            Assert.IsFalse(result.Counts.ContainsKey(99));
        }

        [Test]
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
