using System.Linq;
using Home.State;
using Home.View;
using Root.Service;
using Root.State;
using UnityEngine;
using VContainer.Unity;

namespace Home.Service
{
    /// PlayerPrefsからIsoGridの状態を読み込み、家具を復元するサービス
    public class IsoGridLoadService : IStartable
    {
        readonly IsoGridService _isoGridService;
        readonly UserState _userState;
        readonly MasterDataState _masterDataState;
        readonly FurnitureAssetState _furnitureAssetState;
        readonly PlayerPrefsService _playerPrefsService;

        public IsoGridLoadService(
            IsoGridService isoGridService,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            PlayerPrefsService playerPrefsService)
        {
            _isoGridService = isoGridService;
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

                // プレハブをインスタンス化
                var instance = Object.Instantiate(furnitureAsset.SceneObject);
                var gridPos = new Vector2Int(objectPosition.X, objectPosition.Y);

                // グリッドにオブジェクトを配置
                _isoGridService.PlaceObject(gridPos, instance.FootprintSize, objectPosition.UserFurnitureId);

                // ワールド座標を計算してViewを移動
                var pivotOffset = instance.PivotGridPosition;
                var worldPos = _isoGridService.GridToWorld(gridPos + pivotOffset);
                instance.SetPosition(worldPos);
                instance.SetPlacedOnGrid(true);

                loadedCount++;
            }

            Debug.Log($"IsoGridLoadService: Loaded {loadedCount} objects from IsoGrid");
        }
    }
}
