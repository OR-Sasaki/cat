#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Starter
{
    /// クローゼットの一覧表示に必要な Outfit アセットを全件ロードする
    /// 装備中の分は CharacterOutfitStarter が個別にロードするため、ここは一覧用の先読みが役割
    public sealed class OutfitAssetStarter : IStartable, IDisposable
    {
        readonly OutfitAssetService _outfitAssetService;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public OutfitAssetStarter(OutfitAssetService outfitAssetService)
        {
            _outfitAssetService = outfitAssetService;
        }

        public void Start()
        {
            LoadAllAsync(_cts.Token).Forget();
        }

        async UniTaskVoid LoadAllAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _outfitAssetService.LoadAllAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // シーン破棄によるキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[OutfitAssetStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
