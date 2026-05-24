#nullable enable

using UnityEngine;

namespace Root.Service
{
    /// App Key / Rewarded Ad Unit ID をコードリテラルから排除する構成アセット
    /// Resources/RewardedAdConfig.asset として配置し RootScope からロードする
    [CreateAssetMenu(fileName = "RewardedAdConfig", menuName = "Cat/Rewarded Ad Config")]
    public sealed class RewardedAdConfig : ScriptableObject
    {
        [Header("Android")]
        [SerializeField] string _androidAppKey = string.Empty;
        [SerializeField] string _androidRewardedAdUnitId = string.Empty;

        [Header("iOS")]
        [SerializeField] string _iosAppKey = string.Empty;
        [SerializeField] string _iosRewardedAdUnitId = string.Empty;

        [Header("Placement")]
        [SerializeField] string _defaultPlacementName = "ShopRewardAd";

        [Header("Load Retry")]
        [SerializeField] int _maxLoadRetryCount = 5;
        [SerializeField] float _loadRetryInitialDelaySeconds = 1f;
        [SerializeField] float _loadRetryMaxDelaySeconds = 60f;

        public string DefaultPlacementName => _defaultPlacementName;
        public int MaxLoadRetryCount => _maxLoadRetryCount;
        public float LoadRetryInitialDelaySeconds => _loadRetryInitialDelaySeconds;
        public float LoadRetryMaxDelaySeconds => _loadRetryMaxDelaySeconds;

        /// 対象プラットフォームの App Key を返す
        public string GetAppKey()
        {
#if UNITY_IOS
            return _iosAppKey;
#else
            return _androidAppKey;
#endif
        }

        /// 対象プラットフォームの Rewarded Ad Unit ID を返す
        public string GetRewardedAdUnitId()
        {
#if UNITY_IOS
            return _iosRewardedAdUnitId;
#else
            return _androidRewardedAdUnitId;
#endif
        }
    }
}
