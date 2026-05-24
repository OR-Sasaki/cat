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
    public sealed class RewardedAdServiceStarter : IStartable
    {
        readonly IRewardedAdService _rewardedAdService;

        [Inject]
        public RewardedAdServiceStarter(IRewardedAdService rewardedAdService)
        {
            _rewardedAdService = rewardedAdService;
        }

        public void Start()
        {
            InitializeAsync().Forget();
        }

        async UniTaskVoid InitializeAsync()
        {
            try
            {
                await _rewardedAdService.InitializeAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[RewardedAdServiceStarter] {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
