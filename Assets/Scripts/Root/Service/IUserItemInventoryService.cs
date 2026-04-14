#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// 家具 (数量管理) と着せ替え (保有フラグ管理) の所持状態を
    /// 読み書き・通知・永続化するサービス契約
    public interface IUserItemInventoryService
    {
        /// 家具の所持数を取得する (未所持は 0)
        int GetFurnitureCount(uint furnitureId);

        /// 所持している全家具の ID と数量を列挙する
        IReadOnlyDictionary<uint, int> GetAllFurnitureCounts();

        /// 家具を amount 個追加する (amount > 0、Master に存在する ID のみ)
        ItemInventoryResult AddFurniture(uint furnitureId, int amount);

        /// 着せ替えの所持判定
        bool HasOutfit(uint outfitId);

        /// 所持している全着せ替え ID を列挙する
        IReadOnlyCollection<uint> GetAllOwnedOutfitIds();

        /// 着せ替えを付与する (既所持の場合は冪等成功、通知・保存は発火しない)
        ItemInventoryResult GrantOutfit(uint outfitId);

        /// 家具変更通知 (変更後の所持数を伴う)
        event Action<FurnitureChange> FurnitureChanged;

        /// 着せ替え変更通知 (付与された outfitId)
        event Action<uint> OutfitChanged;

        /// 初期化 (現状はコンストラクタで Load 済みのためノーオペ)
        UniTask InitializeAsync(CancellationToken cancellationToken);

        /// 明示的な保存要求 (通常は書き込み操作時に自動保存される)
        UniTask SaveAsync(CancellationToken cancellationToken);
    }

    public readonly struct FurnitureChange
    {
        public uint FurnitureId { get; }
        public int NewCount { get; }

        public FurnitureChange(uint furnitureId, int newCount)
        {
            FurnitureId = furnitureId;
            NewCount = newCount;
        }
    }

    public enum ItemInventoryErrorCode
    {
        InvalidArgument,
        UnknownId,
        BelowZero,
    }

    public readonly struct ItemInventoryResult
    {
        public bool IsSuccess { get; }
        public ItemInventoryErrorCode? Error { get; }
        public string? Message { get; }

        ItemInventoryResult(bool isSuccess, ItemInventoryErrorCode? error, string? message)
        {
            IsSuccess = isSuccess;
            Error = error;
            Message = message;
        }

        public static ItemInventoryResult Ok() => new(true, null, null);

        public static ItemInventoryResult Fail(ItemInventoryErrorCode code, string message) => new(false, code, message);
    }
}
