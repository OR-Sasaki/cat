#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    /// 起動時に IRewardedAdService.InitializeAsync を発火する起動フック
    public sealed class RewardedAdServiceStarter : IStartable, IDisposable
    {
        readonly IRewardedAdService _rewardedAdService;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public RewardedAdServiceStarter(IRewardedAdService rewardedAdService)
        {
            _rewardedAdService = rewardedAdService;
        }

        public void Start()
        {
            InitializeAsync(_cts.Token).Forget();
        }

        async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _rewardedAdService.InitializeAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[RewardedAdServiceStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
