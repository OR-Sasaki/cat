using System.Linq;
using Cat.Furniture;
using Home.State;
using Root.Service;
using Root.State;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Service
{
    /// PlayerPrefsからIsoGridの状態を読み込み、家具を復元するサービス
    public class IsoGridLoadService : IStartable
    {
        readonly FurniturePlacementService _furniturePlacementService;
        readonly UserState _userState;
        readonly MasterDataState _masterDataState;
        readonly FurnitureAssetState _furnitureAssetState;
        readonly PlayerPrefsService _playerPrefsService;
        readonly RoomBaseState _roomBaseState;
        readonly RoomBaseDefaultService _roomBaseDefaultService;

        [Inject]
        public IsoGridLoadService(
            FurniturePlacementService furniturePlacementService,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            PlayerPrefsService playerPrefsService,
            RoomBaseState roomBaseState,
            RoomBaseDefaultService roomBaseDefaultService)
        {
            _furniturePlacementService = furniturePlacementService;
            _userState = userState;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _playerPrefsService = playerPrefsService;
            _roomBaseState = roomBaseState;
            _roomBaseDefaultService = roomBaseDefaultService;
        }

        public void Start()
        {
            if (_furnitureAssetState.IsLoaded)
            {
                Load();
            }
            else
            {
                _furnitureAssetState.OnLoaded += Load;
            }
        }

        void Load()
        {
            // イベント購読を解除（再実行防止・メモリリーク防止）
            _furnitureAssetState.OnLoaded -= Load;

            // PlayerPrefsから読み込み
            var saveData = _playerPrefsService.Load<IsoGridSaveData>(PlayerPrefsKey.IsoGrid);

            if (saveData is null)
            {
                Debug.Log("IsoGridLoadService: No saved data found");
            }
            else
            {
                // UserStateに設定
                _userState.IsoGridSaveData = saveData;

                var floorLoadedCount = LoadFloorObjects(saveData.Floor);
                var leftWallLoadedCount = LoadWallObjects(saveData.LeftWall, WallSide.Left);
                var rightWallLoadedCount = LoadWallObjects(saveData.RightWall, WallSide.Right);
                var fragmentedLoadedCount = LoadFragmentedObjects(saveData.FragmentedGrids);
                LoadBaseObject(saveData.BaseUserFurnitureId);

                Debug.Log($"IsoGridLoadService: Loaded Floor={floorLoadedCount}, LeftWall={leftWallLoadedCount}, RightWall={rightWallLoadedCount}, Fragmented={fragmentedLoadedCount}, Base={_roomBaseState.PlacedBaseUserFurnitureId}");
            }

            // Post-loadフック: saveData の有無に関わらず必ず1回呼び、Base未配置のときだけデフォルト適用する。
            // OnLoaded多重購読を排除した単線フローを成立させる。
            _roomBaseDefaultService.ApplyDefaultIfNeeded();
        }

        int LoadFloorObjects(GridSaveEntry entry)
        {
            if (entry?.ObjectPositions is null or { Length: 0 })
            {
                return 0;
            }

            var loadedCount = 0;
            foreach (var placement in entry.ObjectPositions)
            {
                var furnitureAsset = GetFurnitureAsset(placement.UserFurnitureId);
                if (furnitureAsset == null) continue;

                var gridPos = new Vector2Int(placement.X, placement.Y);
                _furniturePlacementService.PlaceFloorFurnitureAt(placement.UserFurnitureId, furnitureAsset, gridPos);
                loadedCount++;
            }

            return loadedCount;
        }

        int LoadWallObjects(GridSaveEntry entry, WallSide side)
        {
            if (entry?.ObjectPositions is null or { Length: 0 })
            {
                return 0;
            }

            var loadedCount = 0;
            foreach (var placement in entry.ObjectPositions)
            {
                var furnitureAsset = GetFurnitureAsset(placement.UserFurnitureId);
                if (furnitureAsset == null) continue;

                var gridPos = new Vector2Int(placement.X, placement.Y);
                _furniturePlacementService.PlaceWallFurnitureAt(placement.UserFurnitureId, furnitureAsset, side, gridPos);
                loadedCount++;
            }

            return loadedCount;
        }

        int LoadFragmentedObjects(FragmentedGridSaveEntry[] fragmentedGrids)
        {
            if (fragmentedGrids is null or { Length: 0 })
            {
                return 0;
            }

            // Depthが浅い順（昇順）にソートしてロード（親→子の順で配置する必要があるため）
            var sortedPositions = fragmentedGrids
                .SelectMany(parent => parent.ObjectPositions.Select(child => (parent.ParentUserFurnitureId, Placement: child)))
                .OrderBy(p => p.Placement.Depth);

            var loadedCount = 0;
            foreach (var (parentUserFurnitureId, placement) in sortedPositions)
            {
                var furnitureAsset = GetFurnitureAsset(placement.UserFurnitureId);
                if (furnitureAsset == null) continue;

                var localGridPos = new Vector2Int(placement.X, placement.Y);
                var result = _furniturePlacementService.PlaceFragmentedFurnitureAt(
                    parentUserFurnitureId,
                    placement.UserFurnitureId,
                    furnitureAsset,
                    localGridPos);

                if (result is not null) loadedCount++;
            }

            return loadedCount;
        }

        /// 復元時のBase配置専用パス。GetFurnitureAsset (Floor/Wall 既存挙動) には手を加えない。
        /// 内部でPlacementType == Base かつ BaseSceneObject != null を独自検証してからPlaceBaseに委譲する。
        /// 復元IDがユーザー所持外・PlacementType不一致・BaseSceneObject null のいずれも黙ってreturnし、
        /// 後段の RoomBaseDefaultService.ApplyDefaultIfNeeded がフォールバック配置を担う。
        void LoadBaseObject(int baseUserFurnitureId)
        {
            // -1 sentinel または旧データの 0 は未設定扱い
            if (baseUserFurnitureId <= 0) return;

            var userFurniture = _userState.UserFurnitures?.FirstOrDefault(f => f.Id == baseUserFurnitureId);
            if (userFurniture is null) return;

            var masterFurniture = _masterDataState.Furnitures?.FirstOrDefault(f => f.Id == userFurniture.FurnitureID);
            if (masterFurniture is null) return;

            var furnitureAsset = _furnitureAssetState.Get(masterFurniture.Name);
            if (furnitureAsset is null) return;
            if (furnitureAsset.PlacementType != PlacementType.Base) return;
            if (furnitureAsset.BaseSceneObject is null) return;

            _furniturePlacementService.PlaceBase(baseUserFurnitureId, furnitureAsset);
        }

        Cat.Furniture.Furniture GetFurnitureAsset(int userFurnitureId)
        {
            // UserFurnitureIdからUserFurnitureを取得
            var userFurniture = _userState.UserFurnitures?.FirstOrDefault(f => f.Id == userFurnitureId);
            if (userFurniture is null)
            {
                Debug.LogWarning($"IsoGridLoadService: UserFurniture with Id {userFurnitureId} not found");
                return null;
            }

            // FurnitureIDからマスタデータを取得
            var masterFurniture = _masterDataState.Furnitures?.FirstOrDefault(f => f.Id == userFurniture.FurnitureID);
            if (masterFurniture is null)
            {
                Debug.LogWarning($"IsoGridLoadService: MasterFurniture with Id {userFurniture.FurnitureID} not found");
                return null;
            }

            // FurnitureAssetStateから家具アセットを取得
            var furnitureAsset = _furnitureAssetState.Get(masterFurniture.Name);
            if (furnitureAsset?.SceneObject is null)
            {
                Debug.LogWarning($"IsoGridLoadService: Furniture asset '{masterFurniture.Name}' not found");
                return null;
            }

            return furnitureAsset;
        }
    }
}
