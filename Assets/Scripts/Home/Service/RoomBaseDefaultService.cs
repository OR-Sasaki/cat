using System.Linq;
using Cat.Furniture;
using Home.State;
using Root.Service;
using Root.State;
using VContainer;

namespace Home.Service
{
    /// IsoGridLoadService.Load 末尾から呼ばれる post-load フックの実体。
    /// Base 未配置かつユーザーが Base を所持していれば、所持家具インスタンスを FurnitureId 昇順に走査して
    /// 最初に見つかった PlacementType.Base の家具を配置する。すでに配置済み or 所持無し なら no-op。
    /// IStartable を実装せず FurnitureAssetState.OnLoaded を直接購読しないことで、
    /// IsoGridLoadService との購読順依存を構造的に排除している。
    public class RoomBaseDefaultService
    {
        readonly UserFurnitureInstanceService _userFurnitureInstanceService;
        readonly MasterDataState _masterDataState;
        readonly FurnitureAssetState _furnitureAssetState;
        readonly FurniturePlacementService _furniturePlacementService;
        readonly RoomBaseState _roomBaseState;

        [Inject]
        public RoomBaseDefaultService(
            UserFurnitureInstanceService userFurnitureInstanceService,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            FurniturePlacementService furniturePlacementService,
            RoomBaseState roomBaseState)
        {
            _userFurnitureInstanceService = userFurnitureInstanceService;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _furniturePlacementService = furniturePlacementService;
            _roomBaseState = roomBaseState;
        }

        /// IsoGridLoadService.Load の最末尾から同期呼び出しされるエントリポイント。
        /// 既配置 (RoomBaseState.IsPlaced == true) なら即 return。
        /// Base 未所持なら警告ログを出さず no-op。
        /// 配置に失敗 (PlaceBase が false 返却) しても再試行はしない (静的フォールバックは 1 回のみ)。
        public void ApplyDefaultIfNeeded()
        {
            if (_roomBaseState.IsPlaced) return;

            foreach (var instance in _userFurnitureInstanceService.GetAll())
            {
                var masterFurniture = _masterDataState.Furnitures?.FirstOrDefault(f => f.Id == instance.FurnitureId);
                if (masterFurniture is null) continue;

                var furnitureAsset = _furnitureAssetState.Get(masterFurniture.Name);
                if (furnitureAsset is null) continue;

                if (furnitureAsset.PlacementType != PlacementType.Base) continue;

                _furniturePlacementService.PlaceBase(instance.UserFurnitureId, furnitureAsset);
                return;
            }
        }
    }
}
