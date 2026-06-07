#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Root.Service
{
    /// LevelPlay SDK を呼ばずに即時報酬を返すスタブ
    /// Editor および LevelPlay 非対応プラットフォーム (Standalone / WebGL) のフォールバックとして使う
    public sealed class EditorRewardedAdService : IRewardedAdService
    {
        const int ShowDelayMilliseconds = 100;

#pragma warning disable CS0067 // スタブは状態遷移しないため StateChanged は発火しない
        public event Action<RewardedAdState>? StateChanged;
#pragma warning restore CS0067

        public RewardedAdState State => RewardedAdState.Ready;
        public bool IsReady => true;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("[EditorRewardedAdService] Initialized (stub, always Ready)");
            return UniTask.CompletedTask;
        }

        public async UniTask<RewardedAdResult> ShowAsync(string placementName, CancellationToken cancellationToken)
        {
            Debug.Log($"[EditorRewardedAdService] ShowAsync stub for placement '{placementName}'");
            await UniTask.Delay(ShowDelayMilliseconds, cancellationToken: cancellationToken);
            return RewardedAdResult.Rewarded;
        }
    }
}
