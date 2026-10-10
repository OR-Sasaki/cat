#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    /// 起動時に IAnalyticsService.InitializeAsync を発火する起動フック
    public sealed class AnalyticsServiceStarter : IStartable, IDisposable
    {
        readonly IAnalyticsService _analyticsService;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public AnalyticsServiceStarter(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public void Start()
        {
            InitializeAsync(_cts.Token).Forget();
        }

        async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _analyticsService.InitializeAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnalyticsServiceStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
