#nullable enable

using System;
using System.Collections.Generic;
using Shop.State;

namespace Shop.Service
{
    public static class TimedShopLottery
    {
        public static IReadOnlyList<ShopProduct> DrawTimedProducts(
            IReadOnlyList<ShopProduct> source,
            int slotCount,
            int seed)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "slotCount must be positive.");

            if (source.Count == 0)
                return Array.Empty<ShopProduct>();

            var random = new System.Random(seed);

            if (source.Count >= slotCount)
            {
                var pool = new ShopProduct[source.Count];
                for (var i = 0; i < source.Count; i++) pool[i] = source[i];

                // Fisher-Yates partial shuffle: only need top slotCount elements
                for (var i = 0; i < slotCount; i++)
                {
                    var j = i + random.Next(pool.Length - i);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                var result = new ShopProduct[slotCount];
                Array.Copy(pool, result, slotCount);
                return result;
            }
            else
            {
                var result = new ShopProduct[slotCount];
                for (var i = 0; i < slotCount; i++)
                {
                    result[i] = source[random.Next(source.Count)];
                }
                return result;
            }
        }
    }
}
