#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Root.Service
{
    /// Outfit アセットの Addressables ロードとキャッシュを一元管理する
    /// ロード済みアセットは Root スコープで保持し続けるため、意図的に Release しない
    public class OutfitAssetService
    {
        readonly OutfitAssetState _outfitAssetState;
        readonly MasterDataState _masterDataState;

        public OutfitAssetService(
            OutfitAssetState outfitAssetState,
            MasterDataState masterDataState)
        {
            _outfitAssetState = outfitAssetState;
            _masterDataState = masterDataState;
        }

        /// マスタ全件をロードする（クローゼットの一覧表示用）
        public async UniTask LoadAllAsync(CancellationToken cancellationToken)
        {
            if (_outfitAssetState.IsAllLoaded) return;

            var masterOutfits = _masterDataState.Outfits;
            if (masterOutfits is null || masterOutfits.Length == 0)
            {
                Debug.LogError("[OutfitAssetService] MasterDataState.Outfits is null or empty");
                // 一覧側が待ち続けないよう、ロードできなくても完了として通知する
                _outfitAssetState.NotifyAllLoaded();
                return;
            }

            await LoadAsync(masterOutfits.Select(o => o.Name), cancellationToken);
            _outfitAssetState.NotifyAllLoaded();
        }

        /// 指定した名前の Outfit をロードする（キャッシュ済みのものはスキップ）
        /// 同じアドレスへのロードが同時に走っても Addressables 側で 1 回にまとめられるため、ここでは重複排除しない
        public async UniTask LoadAsync(IEnumerable<string> outfitNames, CancellationToken cancellationToken)
        {
            var tasks = outfitNames
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .Where(name => !_outfitAssetState.Contains(name))
                .Select(name => LoadAndCacheAsync(name, cancellationToken))
                .ToArray();
            if (tasks.Length == 0) return;

            await UniTask.WhenAll(tasks);
        }

        async UniTask LoadAndCacheAsync(string outfitName, CancellationToken cancellationToken)
        {
            var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Name == outfitName);
            if (masterOutfit is null)
            {
                Debug.LogError($"[OutfitAssetService] master data not found: {outfitName}");
                return;
            }

            var address = $"{masterOutfit.Type}/{outfitName}.asset";
            var handle = Addressables.LoadAssetAsync<Cat.Character.Outfit>(address);

            try
            {
                await handle.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result is null)
            {
                var error = handle.OperationException?.Message ?? "Unknown error";
                Debug.LogError($"[OutfitAssetService] failed to load '{address}': {error}");
                if (handle.IsValid()) Addressables.Release(handle);
                return;
            }

            _outfitAssetState.Add(outfitName, handle.Result);
        }
    }
}
