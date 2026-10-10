#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Root.Service
{
    /// Unity Analytics へ一切送信しないスタブ
    /// Editor の再生セッションがダッシュボードの DAU / 継続率へ混ざるのを避けるため、
    /// RootScope は Editor でこちらを登録する
    public sealed class EditorAnalyticsService : IAnalyticsService
    {
        public bool IsCollecting => false;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("[EditorAnalyticsService] Initialized (stub, no data is sent)");
            return UniTask.CompletedTask;
        }
    }
}
