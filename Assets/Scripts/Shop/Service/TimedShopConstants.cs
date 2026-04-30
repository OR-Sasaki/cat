#nullable enable

using System;

namespace Shop.Service
{
    public static class TimedShopConstants
    {
        public static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(30);
        public const int TimedFurnitureSlotCount = 6;
        public const int TimedOutfitSlotCount = 6;
    }
}
