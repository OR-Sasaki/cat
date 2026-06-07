#nullable enable

namespace Shop.RewardAd
{
    /// リワード広告ショップの共通定数
    public static class RewardAdShopConstants
    {
        /// 日次視聴上限の既定値（マスターの daily_cap 未指定・0 以下のとき適用）
        public const int DefaultDailyCap = 5;
    }
}
