#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    public class UserItemInventoryService : IUserItemInventoryService
    {
        readonly UserItemInventoryState _state;
        readonly PlayerPrefsService _playerPrefsService;
        readonly MasterDataState _masterDataState;
        readonly MasterDataImportService _masterDataImportService;
        readonly UserEquippedOutfitService _userEquippedOutfitService;

        public event Action<FurnitureChange>? FurnitureChanged;
        public event Action<uint>? OutfitChanged;

        [Inject]
        public UserItemInventoryService(
            UserItemInventoryState state,
            PlayerPrefsService playerPrefsService,
            MasterDataState masterDataState,
            MasterDataImportService masterDataImportService,
            UserEquippedOutfitService userEquippedOutfitService)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            _masterDataState = masterDataState;
            _masterDataImportService = masterDataImportService;
            _userEquippedOutfitService = userEquippedOutfitService;

            // 未 import 時に Load すると全 ID が破棄されるため、import 完了を待つ
            if (_masterDataState.IsImported)
            {
                Load();
            }
            else
            {
                _masterDataImportService.Imported += OnMasterDataImported;
            }
        }

        void OnMasterDataImported()
        {
            _masterDataImportService.Imported -= OnMasterDataImported;
            Load();
        }

        public int GetFurnitureCount(uint furnitureId) => _state.GetFurnitureCount(furnitureId);

        public IReadOnlyDictionary<uint, int> GetAllFurnitureCounts() => _state.GetAllFurnitureCounts();

        public bool HasOutfit(uint outfitId) => _state.HasOutfit(outfitId);

        public IReadOnlyCollection<uint> GetAllOwnedOutfitIds() => _state.GetAllOwnedOutfitIds();

        public ItemInventoryResult AddFurniture(uint furnitureId, int amount)
        {
            if (amount <= 0)
            {
                return ItemInventoryResult.Fail(ItemInventoryErrorCode.InvalidArgument);
            }

            if (!IsKnownFurnitureId(furnitureId))
            {
                return ItemInventoryResult.Fail(ItemInventoryErrorCode.UnknownId);
            }

            var currentCount = _state.GetFurnitureCount(furnitureId);
            if ((long)currentCount + amount > int.MaxValue)
            {
                return ItemInventoryResult.Fail(ItemInventoryErrorCode.InvalidArgument);
            }

            var newCount = currentCount + amount;
            _state.SetFurnitureCount(furnitureId, newCount);
            FireFurnitureChanged(new FurnitureChange(furnitureId, newCount));
            Save();
            return ItemInventoryResult.Ok();
        }

        public ItemInventoryResult GrantOutfit(uint outfitId)
        {
            if (!IsKnownOutfitId(outfitId))
            {
                return ItemInventoryResult.Fail(ItemInventoryErrorCode.UnknownId);
            }

            // 既所持は冪等成功 (通知・保存なし)
            if (_state.HasOutfit(outfitId))
            {
                return ItemInventoryResult.Ok();
            }

            _state.AddOwnedOutfit(outfitId);
            FireOutfitChanged(outfitId);
            Save();
            return ItemInventoryResult.Ok();
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public UniTask SaveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Save();
            return UniTask.CompletedTask;
        }

        void Load()
        {
            _state.Clear();

            UserItemInventorySnapshot? snapshot;
            try
            {
                snapshot = _playerPrefsService.Load<UserItemInventorySnapshot>(PlayerPrefsKey.UserItemInventory);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserItemInventoryService] {e.Message}\n{e.StackTrace}");
                EnsureEquippedOutfitsOwned();
                return;
            }

            if (snapshot is not { Version: UserItemInventorySnapshot.CurrentVersion })
            {
                EnsureEquippedOutfitsOwned();
                return;
            }

            if (snapshot.Furnitures is not null)
            {
                foreach (var entry in snapshot.Furnitures)
                {
                    if (entry.Count <= 0) continue;
                    if (!IsKnownFurnitureId(entry.FurnitureId)) continue;
                    _state.SetFurnitureCount(entry.FurnitureId, entry.Count);
                }
            }

            if (snapshot.OwnedOutfitIds is not null)
            {
                foreach (var id in snapshot.OwnedOutfitIds)
                {
                    if (!IsKnownOutfitId(id)) continue;
                    _state.AddOwnedOutfit(id);
                }
            }

            EnsureEquippedOutfitsOwned();
        }

        void EnsureEquippedOutfitsOwned()
        {
            var equipped = _userEquippedOutfitService.GetAllEquippedOutfitIds();
            foreach (var kvp in equipped)
            {
                if (!IsKnownOutfitId(kvp.Value)) continue;
                if (!_state.HasOutfit(kvp.Value))
                {
                    _state.AddOwnedOutfit(kvp.Value);
                }
            }
        }

        void Save()
        {
            try
            {
                var counts = _state.GetAllFurnitureCounts();
                var furnitures = new FurnitureHoldingEntry[counts.Count];
                var fi = 0;
                foreach (var kvp in counts)
                {
                    furnitures[fi++] = new FurnitureHoldingEntry { FurnitureId = kvp.Key, Count = kvp.Value };
                }

                var owned = _state.GetAllOwnedOutfitIds();
                var outfits = new uint[owned.Count];
                var oi = 0;
                foreach (var id in owned)
                {
                    outfits[oi++] = id;
                }

                var snapshot = new UserItemInventorySnapshot
                {
                    Version = UserItemInventorySnapshot.CurrentVersion,
                    Furnitures = furnitures,
                    OwnedOutfitIds = outfits,
                };
                _playerPrefsService.Save(PlayerPrefsKey.UserItemInventory, snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserItemInventoryService] {e.Message}\n{e.StackTrace}");
            }
        }

        bool IsKnownFurnitureId(uint id)
        {
            var list = _masterDataState.Furnitures;
            if (list is null) return false;
            foreach (var f in list)
            {
                if (f.Id == id) return true;
            }
            return false;
        }

        bool IsKnownOutfitId(uint id)
        {
            var list = _masterDataState.Outfits;
            if (list is null) return false;
            foreach (var o in list)
            {
                if (o.Id == id) return true;
            }
            return false;
        }

        void FireFurnitureChanged(FurnitureChange change)
        {
            var handlers = FurnitureChanged;
            if (handlers is null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<FurnitureChange>)handler)(change);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserItemInventoryService] {e.Message}\n{e.StackTrace}");
                }
            }
        }

        void FireOutfitChanged(uint outfitId)
        {
            var handlers = OutfitChanged;
            if (handlers is null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<uint>)handler)(outfitId);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserItemInventoryService] {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
