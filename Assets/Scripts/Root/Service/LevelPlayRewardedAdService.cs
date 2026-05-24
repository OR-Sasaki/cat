#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
#if UNITY_ANDROID || UNITY_IOS
using Unity.Services.LevelPlay;
#endif
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisements.IosSupport;
#endif

namespace Root.Service
{
    /// LevelPlay SDK 9.4.1 を IRewardedAdService に適合させる実機実装
    /// LevelPlay 型参照は #if UNITY_ANDROID || UNITY_IOS 配下に閉じ、それ以外は NoOp フォールバック
    public sealed class LevelPlayRewardedAdService : IRewardedAdService, IDisposable
    {
        /// OnAdClosed 先行時に OnAdRewarded の追従を待つ猶予時間
        const int GraceMilliseconds = 200;

        readonly RewardedAdConfig _config;
        readonly IClock _clock;
        readonly CancellationTokenSource _lifetimeCts = new();

        RewardedAdState _state = RewardedAdState.Uninitialized;

        public event Action<RewardedAdState>? StateChanged;

        public RewardedAdState State => _state;
        public bool IsReady => _state == RewardedAdState.Ready;

#if UNITY_ANDROID || UNITY_IOS
        LevelPlayRewardedAd? _rewardedAd;
        RewardedAdSession? _session;
        UniTaskCompletionSource<bool>? _initTcs;
        int _loadRetryCount;
        bool _initEventsSubscribed;
#endif

        [Inject]
        public LevelPlayRewardedAdService(RewardedAdConfig config, IClock clock)
        {
            _config = config;
            _clock = clock;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_state is RewardedAdState.Initializing
                or RewardedAdState.Loading
                or RewardedAdState.Ready
                or RewardedAdState.Showing)
            {
                return;
            }

            SetState(RewardedAdState.Initializing);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);

            try
            {
                await RequestTrackingAuthorizationIfNeededAsync(linkedCts.Token);

                var appKey = _config.GetAppKey();
                if (string.IsNullOrEmpty(appKey))
                {
                    Debug.LogError("[LevelPlayRewardedAdService] App Key is empty. Check RewardedAdConfig.");
                    SetState(RewardedAdState.Failed);
                    return;
                }

                var initTcs = _initTcs = new UniTaskCompletionSource<bool>();

                if (!_initEventsSubscribed)
                {
                    LevelPlay.OnInitSuccess += HandleInitSuccess;
                    LevelPlay.OnInitFailed += HandleInitFailed;
                    _initEventsSubscribed = true;
                }

                LevelPlay.Init(appKey);

                var initialized = await initTcs.Task.AttachExternalCancellation(linkedCts.Token);
                if (!initialized)
                {
                    SetState(RewardedAdState.Failed);
                }
            }
            catch (OperationCanceledException)
            {
                SetState(RewardedAdState.Uninitialized);
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelPlayRewardedAdService] {e.Message}\n{e.StackTrace}");
                SetState(RewardedAdState.Failed);
            }
#else
            cancellationToken.ThrowIfCancellationRequested();
            Debug.LogWarning("[LevelPlayRewardedAdService] LevelPlay is unsupported on this platform. Operating as NoOp.");
            SetState(RewardedAdState.Failed);
            await UniTask.CompletedTask;
#endif
        }

        public async UniTask<RewardedAdResult> ShowAsync(string placementName, CancellationToken cancellationToken)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!IsReady || _rewardedAd == null)
            {
                Debug.LogWarning($"[LevelPlayRewardedAdService] ShowAsync called while not ready (state={_state}).");
                return RewardedAdResult.NotReady;
            }

            if (_session != null)
            {
                Debug.LogWarning("[LevelPlayRewardedAdService] ShowAsync called while a session is in progress.");
                return RewardedAdResult.NotReady;
            }

            if (!_rewardedAd.IsAdReady() || LevelPlayRewardedAd.IsPlacementCapped(placementName))
            {
                Debug.LogWarning($"[LevelPlayRewardedAdService] Ad not ready or placement capped (placement={placementName}).");
                return RewardedAdResult.NotReady;
            }

            var session = new RewardedAdSession();
            _session = session;
            SetState(RewardedAdState.Showing);

            var startedAt = _clock.UtcNow;
            RewardedAdResult result;
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
                _rewardedAd.ShowAd(placementName);
                result = await session.Tcs.Task.AttachExternalCancellation(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                result = RewardedAdResult.DisplayFailed;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelPlayRewardedAdService] {e.Message}\n{e.StackTrace}");
                result = RewardedAdResult.DisplayFailed;
            }
            finally
            {
                _session = null;
            }

            var elapsed = _clock.UtcNow - startedAt;
            Debug.Log($"[LevelPlayRewardedAdService] ShowAsync completed: {result} ({elapsed.TotalSeconds:F1}s)");

            ReloadForNextShow();
            return result;
#else
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
            return RewardedAdResult.NotReady;
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            Debug.Log("[LevelPlayRewardedAdService] LevelPlay init success.");
            CreateAndLoadRewardedAd();
            _initTcs?.TrySetResult(true);
        }

        void HandleInitFailed(LevelPlayInitError error)
        {
            Debug.LogError($"[LevelPlayRewardedAdService] LevelPlay init failed: {error}");
            _initTcs?.TrySetResult(false);
        }

        void CreateAndLoadRewardedAd()
        {
            var adUnitId = _config.GetRewardedAdUnitId();
            if (string.IsNullOrEmpty(adUnitId))
            {
                Debug.LogError("[LevelPlayRewardedAdService] Rewarded Ad Unit ID is empty. Check RewardedAdConfig.");
                SetState(RewardedAdState.Failed);
                return;
            }

            if (_rewardedAd == null)
            {
                _rewardedAd = new LevelPlayRewardedAd(adUnitId);
                _rewardedAd.OnAdLoaded += HandleAdLoaded;
                _rewardedAd.OnAdLoadFailed += HandleAdLoadFailed;
                _rewardedAd.OnAdDisplayed += HandleAdDisplayed;
                _rewardedAd.OnAdDisplayFailed += HandleAdDisplayFailed;
                _rewardedAd.OnAdRewarded += HandleAdRewarded;
                _rewardedAd.OnAdClosed += HandleAdClosed;
            }

            _loadRetryCount = 0;
            SetState(RewardedAdState.Loading);
            _rewardedAd.LoadAd();
        }

        void HandleAdLoaded(LevelPlayAdInfo info)
        {
            Debug.Log("[LevelPlayRewardedAdService] Rewarded ad loaded.");
            _loadRetryCount = 0;
            SetState(RewardedAdState.Ready);
        }

        void HandleAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[LevelPlayRewardedAdService] Rewarded ad load failed: {error}");
            RetryLoadAsync().Forget();
        }

        async UniTaskVoid RetryLoadAsync()
        {
            if (_loadRetryCount >= _config.MaxLoadRetryCount)
            {
                Debug.LogError($"[LevelPlayRewardedAdService] Rewarded ad load failed after {_loadRetryCount} retries. Giving up.");
                SetState(RewardedAdState.Failed);
                return;
            }

            var delaySeconds = Mathf.Min(
                _config.LoadRetryInitialDelaySeconds * Mathf.Pow(2f, _loadRetryCount),
                _config.LoadRetryMaxDelaySeconds);
            _loadRetryCount++;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: _lifetimeCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_rewardedAd == null) return;
            SetState(RewardedAdState.Loading);
            _rewardedAd.LoadAd();
        }

        void HandleAdDisplayed(LevelPlayAdInfo info)
        {
            Debug.Log("[LevelPlayRewardedAdService] Rewarded ad displayed.");
        }

        void HandleAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogError($"[LevelPlayRewardedAdService] Rewarded ad display failed: {error}");
            _session?.TryComplete(RewardedAdResult.DisplayFailed);
        }

        void HandleAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            Debug.Log("[LevelPlayRewardedAdService] Rewarded ad rewarded.");
            var session = _session;
            if (session == null) return;

            session.RewardedFired = true;
            if (session.ClosedFired)
            {
                session.TryComplete(RewardedAdResult.Rewarded);
            }
        }

        void HandleAdClosed(LevelPlayAdInfo info)
        {
            Debug.Log("[LevelPlayRewardedAdService] Rewarded ad closed.");
            var session = _session;
            if (session == null) return;

            session.ClosedFired = true;
            if (session.RewardedFired)
            {
                session.TryComplete(RewardedAdResult.Rewarded);
                return;
            }

            ResolveDismissAfterGraceAsync(session).Forget();
        }

        async UniTaskVoid ResolveDismissAfterGraceAsync(RewardedAdSession session)
        {
            try
            {
                await UniTask.Delay(GraceMilliseconds, cancellationToken: _lifetimeCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (session.Completed) return;
            session.TryComplete(session.RewardedFired ? RewardedAdResult.Rewarded : RewardedAdResult.Dismissed);
        }

        void ReloadForNextShow()
        {
            if (_rewardedAd == null) return;

            _loadRetryCount = 0;
            SetState(RewardedAdState.Loading);
            _rewardedAd.LoadAd();
        }
#endif

        void SetState(RewardedAdState next)
        {
            if (_state == next) return;

            _state = next;
            FireStateChanged(next);
        }

        void FireStateChanged(RewardedAdState next)
        {
            var handlers = StateChanged;
            if (handlers is null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<RewardedAdState>)handler)(next);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LevelPlayRewardedAdService] {e.Message}\n{e.StackTrace}");
                }
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        /// iOS ATT 応答が確定するまで SDK 初期化を遅延する
        /// Denied / Restricted を含むあらゆる応答でも初期化は継続する (eCPM は下がるが広告は表示可能)
        async UniTask RequestTrackingAuthorizationIfNeededAsync(CancellationToken cancellationToken)
        {
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();

                while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                    == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    await UniTask.Delay(100, cancellationToken: cancellationToken);
                }
            }

            Debug.Log($"[LevelPlayRewardedAdService] ATT status: {ATTrackingStatusBinding.GetAuthorizationTrackingStatus()}");
        }
#else
        /// 非 iOS では ATT 不要のため即時完了
        UniTask RequestTrackingAuthorizationIfNeededAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }
#endif

        public void Dispose()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();

#if UNITY_ANDROID || UNITY_IOS
            if (_initEventsSubscribed)
            {
                LevelPlay.OnInitSuccess -= HandleInitSuccess;
                LevelPlay.OnInitFailed -= HandleInitFailed;
                _initEventsSubscribed = false;
            }

            if (_rewardedAd != null)
            {
                _rewardedAd.OnAdLoaded -= HandleAdLoaded;
                _rewardedAd.OnAdLoadFailed -= HandleAdLoadFailed;
                _rewardedAd.OnAdDisplayed -= HandleAdDisplayed;
                _rewardedAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
                _rewardedAd.OnAdRewarded -= HandleAdRewarded;
                _rewardedAd.OnAdClosed -= HandleAdClosed;
                _rewardedAd.DestroyAd();
                _rewardedAd = null;
            }
#endif
        }

        /// 1 回の視聴セッションの結果合流を担う
        /// OnAdRewarded / OnAdClosed の発火順序に依存せず 1 件の終端結果を確定する
        sealed class RewardedAdSession
        {
            public readonly UniTaskCompletionSource<RewardedAdResult> Tcs = new();
            public bool RewardedFired;
            public bool ClosedFired;
            public bool Completed;

            public bool TryComplete(RewardedAdResult result)
            {
                if (Completed) return false;

                Completed = true;
                Tcs.TrySetResult(result);
                return true;
            }
        }
    }
}
