#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// Analytics SDK を上位層から隠蔽する抽象ポート
    /// 実装は Unity Analytics (UGS) の型を一切公開しない
    public interface IAnalyticsService
    {
        /// データ収集中か。初期化前・初期化失敗時・スタブ実装では false
        bool IsCollecting { get; }

        UniTask InitializeAsync(CancellationToken cancellationToken);
    }
}
