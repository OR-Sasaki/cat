#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Root.Service
{
    /// Unity Analytics (UGS) を IAnalyticsService に適合させる実機実装
    /// 収集が始まると SDK がセッション・端末情報・継続率用の標準イベントを自動送信する。
    /// 送信先は ProjectSettings の cloudProjectId を SDK が直接参照するため構成アセットは不要
    public sealed class UnityAnalyticsService : IAnalyticsService
    {
        bool _isCollecting;

        public bool IsCollecting => _isCollecting;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isCollecting) return;

            try
            {
                // 同意は Unity 6 の Developer Data framework 経由で立てる。
                // 旧 API の AnalyticsService.Instance.StartDataCollection() は obsolete で、
                // 両方式を混ぜると SDK が例外を投げるためこちらだけを使う
                GrantAnalyticsConsent();

                // 初期化中に Analytics SDK が上記の同意を読み取り収集を開始する。
                // InitializeAsync は多重呼び出しに耐える (初期化中なら同じ Task を待ち、済みなら即完了する)
                // ので、他サービスが先に Unity Services を初期化していても安全に通る
                await UnityServices.InitializeAsync().AsUniTask().AttachExternalCancellation(cancellationToken);

                _isCollecting = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                // 計測が失敗してもゲーム進行は止めない
                Debug.LogError($"[UnityAnalyticsService] {e.Message}\n{e.StackTrace}");
            }
        }

        /// AdsIntent は広告側の同意設計に委ねるため、読み出した値をそのまま書き戻す
        static void GrantAnalyticsConsent()
        {
            var consentState = EndUserConsent.GetConsentState();
            if (consentState.AnalyticsIntent == ConsentStatus.Granted) return;

            consentState.AnalyticsIntent = ConsentStatus.Granted;
            EndUserConsent.SetConsentState(consentState);
        }
    }
}
