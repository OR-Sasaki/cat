using System.Linq;
using Home.State;
using Root.Service;
using Root.State;
using UnityEngine;
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

        public IsoGridLoadService(
            FurniturePlacementService furniturePlacementService,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            PlayerPrefsService playerPrefsService)
        {
            _furniturePlacementService = furniturePlacementService;
            _userState = userState;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _playerPrefsService = playerPrefsService;
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
                return;
            }

            // UserStateに設定
            _userState.IsoGridSaveData = saveData;

            var floorLoadedCount = LoadFloorObjects(saveData);
            var wallLoadedCount = LoadWallObjects(saveData);
            var fragmentedLoadedCount = LoadFragmentedObjects(saveData);

            Debug.Log($"IsoGridLoadService: Loaded {floorLoadedCount} floor objects, {wallLoadedCount} wall objects, and {fragmentedLoadedCount} fragmented objects from IsoGrid");
        }

        int LoadFloorObjects(IsoGridSaveData saveData)
        {
            if (saveData.ObjectPositions is null or { Length: 0 })
            {
                return 0;
            }

            var loadedCount = 0;
            foreach (var objectPosition in saveData.ObjectPositions)
            {
                var furnitureAsset = GetFurnitureAsset(objectPosition.UserFurnitureId);
                if (furnitureAsset == null) continue;

                // 指定位置に床家具を配置
                var gridPos = new Vector2Int(objectPosition.X, objectPosition.Y);
                _furniturePlacementService.PlaceFloorFurnitureAt(objectPosition.UserFurnitureId, furnitureAsset, gridPos);
                loadedCount++;
            }

            return loadedCount;
        }

        int LoadWallObjects(IsoGridSaveData saveData)
        {
            if (saveData.WallObjectPositions is null or { Length: 0 })
            {
                return 0;
            }

            var loadedCount = 0;
            foreach (var wallObjectPosition in saveData.WallObjectPositions)
            {
                var furnitureAsset = GetFurnitureAsset(wallObjectPosition.UserFurnitureId);
                if (furnitureAsset == null) continue;

                // 指定位置に壁家具を配置
                var side = (WallSide)wallObjectPosition.Side;
                var gridPos = new Vector2Int(wallObjectPosition.X, wallObjectPosition.Z);
                _furniturePlacementService.PlaceWallFurnitureAt(wallObjectPosition.UserFurnitureId, furnitureAsset, side, gridPos);
                loadedCount++;
            }

            return loadedCount;
        }

        int LoadFragmentedObjects(IsoGridSaveData saveData)
        {
            if (saveData.FragmentedObjectPositions is null or { Length: 0 })
            {
                return 0;
            }

            // Depthが浅い順（昇順）にソートしてロード
            var sortedPositions = saveData.FragmentedObjectPositions.OrderBy(p => p.Depth);

            var loadedCount = 0;
            foreach (var fragmentedPosition in sortedPositions)
            {
                var furnitureAsset = GetFurnitureAsset(fragmentedPosition.UserFurnitureId);
                if (furnitureAsset == null) continue;

                // 指定位置にFragmentedIsoGrid上の家具を配置
                var localGridPos = new Vector2Int(fragmentedPosition.X, fragmentedPosition.Y);
                var result = _furniturePlacementService.PlaceFragmentedFurnitureAt(
                    fragmentedPosition.ParentUserFurnitureId,
                    fragmentedPosition.UserFurnitureId,
                    furnitureAsset,
                    localGridPos);

                if (result is not null) loadedCount++;
            }

            return loadedCount;
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
