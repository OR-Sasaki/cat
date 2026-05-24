#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// リワード広告の内部状態
    public enum RewardedAdState
    {
        Uninitialized,
        Initializing,
        Loading,
        Ready,
        Showing,
        Failed
    }

    /// 1 回の視聴セッションの終端結果
    public enum RewardedAdResult
    {
        Rewarded,
        Dismissed,
        DisplayFailed,
        NotReady
    }

    /// 広告 SDK を Shop 層から隠蔽する抽象ポート
    /// 実装は LevelPlay SDK 型を一切公開しない
    public interface IRewardedAdService
    {
        RewardedAdState State { get; }
        bool IsReady { get; }

        event Action<RewardedAdState> StateChanged;

        UniTask InitializeAsync(CancellationToken cancellationToken);
        UniTask<RewardedAdResult> ShowAsync(string placementName, CancellationToken cancellationToken);
    }
}
