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

            if (saveData?.ObjectPositions == null || saveData.ObjectPositions.Length == 0)
            {
                Debug.Log("IsoGridLoadService: No saved data found");
                return;
            }

            // UserStateに設定
            _userState.IsoGridSaveData = saveData;

            var loadedCount = 0;
            foreach (var objectPosition in saveData.ObjectPositions)
            {
                // UserFurnitureIdからUserFurnitureを取得
                var userFurniture = _userState.UserFurnitures?.FirstOrDefault(f => f.Id == objectPosition.UserFurnitureId);
                if (userFurniture == null)
                {
                    Debug.LogWarning($"IsoGridLoadService: UserFurniture with Id {objectPosition.UserFurnitureId} not found");
                    continue;
                }

                // FurnitureIDからマスタデータを取得
                var masterFurniture = _masterDataState.Furnitures?.FirstOrDefault(f => f.Id == userFurniture.FurnitureID);
                if (masterFurniture == null)
                {
                    Debug.LogWarning($"IsoGridLoadService: MasterFurniture with Id {userFurniture.FurnitureID} not found");
                    continue;
                }

                // FurnitureAssetStateから家具アセットを取得
                var furnitureAsset = _furnitureAssetState.Get(masterFurniture.Name);
                if (furnitureAsset?.SceneObject == null)
                {
                    Debug.LogWarning($"IsoGridLoadService: Furniture asset '{masterFurniture.Name}' not found");
                    continue;
                }

                // 指定位置に家具を配置
                var gridPos = new Vector2Int(objectPosition.X, objectPosition.Y);
                _furniturePlacementService.PlaceFurnitureAt(objectPosition.UserFurnitureId, furnitureAsset, gridPos);
                loadedCount++;
            }

            Debug.Log($"IsoGridLoadService: Loaded {loadedCount} objects from IsoGrid");
        }
    }
}
