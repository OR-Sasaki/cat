using System.Linq;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;
using Home.View;
using Root.State;
using UnityEngine;
using UnityEngine.Events;
using VContainer.Unity;

namespace Home.Service
{
    public class RedecorateScrollerService : IEnhancedScrollerDelegate, IStartable
    {
        readonly RedecorateUiView _redecorateUiView;
        readonly UserState _userState;
        readonly MasterDataState _masterDataState;
        readonly FurnitureAssetState _furnitureAssetState;
        readonly IsoGridState _isoGridState;
        readonly IsoGridService _isoGridService;
        readonly UnityEvent<RedecorateRowCellView> _cellSelectedEvent = new();
        SmallList<RedecorateFurnitureData> _data = new();

        public RedecorateScrollerService(
            RedecorateUiView redecorateUiView,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            IsoGridState isoGridState,
            IsoGridService isoGridService)
        {
            _redecorateUiView = redecorateUiView;
            _userState = userState;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _isoGridState = isoGridState;
            _isoGridService = isoGridService;
        }

        public void Start()
        {
            _redecorateUiView.OnOpen.AddListener(Initialize);
            _cellSelectedEvent.AddListener(OnCellViewSelected);
        }

        void Initialize()
        {
            var scroller = _redecorateUiView.Scroller;
            scroller.Delegate = this;

            if (_furnitureAssetState.IsLoaded)
            {
                LoadData();
            }
            else
            {
                _furnitureAssetState.OnLoaded += LoadData;
            }
        }

        void LoadData()
        {
            // 既存データのハンドラーをクリア
            for (var i = 0; i < _data.Count; i++)
            {
                _data[i].SelectedChanged.RemoveAllListeners();
            }

            _data.Clear();

            if (_userState.UserFurnitures is null)
            {
                Debug.LogError("[RedecorateScrollerService] UserState.UserFurnitures is null");
                return;
            }

            foreach (var userFurniture in _userState.UserFurnitures)
            {
                var masterFurniture = _masterDataState.Furnitures?.FirstOrDefault(f => f.Id == userFurniture.FurnitureID);
                if (masterFurniture is null) continue;

                var furniture = _furnitureAssetState.Get(masterFurniture.Name);
                if (furniture is null) continue;

                var furnitureData = new RedecorateFurnitureData(userFurniture.Id, furniture);
                // ObjectFootprintStartPositionsに登録されていればSelectedをtrueにする
                furnitureData.Selected = _isoGridState.ObjectFootprintStartPositions.ContainsKey(userFurniture.Id);
                _data.Add(furnitureData);
            }

            _redecorateUiView.Scroller.ReloadData();
        }

        void OnCellViewSelected(RedecorateRowCellView cellView)
        {
            if (cellView is null) return;

            var selectedDataIndex = cellView.DataIndex;
            var selectedData = _data[selectedDataIndex];
            if (selectedData?.Furniture is null) return;

            var userFurnitureId = selectedData.UserFurnitureId;

            if (selectedData.Selected)
            {
                // 配置済みの場合：グリッドとシーンから削除
                RemoveFurniture(userFurnitureId, selectedData.Furniture);
            }
            else
            {
                // 未配置の場合：空き位置を探して配置
                PlaceFurniture(userFurnitureId, selectedData.Furniture);
            }

            UpdateSelectionStates();
        }

        void PlaceFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            if (furniture.SceneObject == null) return;

            var footprintSize = furniture.SceneObject.FootprintSize;
            var availablePos = FindAvailablePosition(footprintSize);
            if (availablePos == null)
            {
                Debug.LogWarning("[RedecorateScrollerService] No available position found");
                return;
            }

            var gridPos = availablePos.Value;

            // プレハブをインスタンス化
            var instance = Object.Instantiate(furniture.SceneObject);
            instance.SetUserFurnitureId(userFurnitureId);

            // グリッドにオブジェクトを配置
            _isoGridService.PlaceObject(gridPos, footprintSize, userFurnitureId);

            // ワールド座標を計算してViewを移動
            var pivotOffset = instance.PivotGridPosition;
            var worldPos = _isoGridService.GridToWorld(gridPos + pivotOffset);
            instance.SetPosition(worldPos);
            instance.SetPlacedOnGrid(true);

#if UNITY_EDITOR
            var gizmo = instance.GetComponent<IsoDraggableGizmo>();
            if (gizmo != null)
            {
                var settingsView = Object.FindFirstObjectByType<IsoGridSettingsView>();
                gizmo.Init(settingsView, _isoGridService);
            }
#endif
        }

        void RemoveFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            if (furniture.SceneObject == null) return;

            // シーン上からUserFurnitureIdが一致するIsoDraggableViewを探す
            var allDraggables = Object.FindObjectsByType<IsoDraggableView>(FindObjectsSortMode.None);
            IsoDraggableView targetView = null;
            foreach (var draggable in allDraggables)
            {
                if (draggable.UserFurnitureId == userFurnitureId)
                {
                    targetView = draggable;
                    break;
                }
            }

            if (targetView == null)
            {
                Debug.LogWarning($"[RedecorateScrollerService] IsoDraggableView with UserFurnitureId {userFurnitureId} not found");
                return;
            }

            // グリッドからオブジェクトを削除
            _isoGridService.RemoveObject(userFurnitureId, targetView.FootprintSize);

            // シーンからオブジェクトを削除
            Object.Destroy(targetView.gameObject);
        }

        Vector2Int? FindAvailablePosition(Vector2Int footprintSize)
        {
            // グリッドをスキャンして空き位置を探す
            for (var y = 0; y < _isoGridState.GridHeight - footprintSize.y + 1; y++)
            {
                for (var x = 0; x < _isoGridState.GridWidth - footprintSize.x + 1; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (_isoGridService.CanPlaceObject(pos, footprintSize))
                    {
                        return pos;
                    }
                }
            }
            return null;
        }

        /// ObjectFootprintStartPositionsに基づいて全データの選択状態を更新する
        public void UpdateSelectionStates()
        {
            for (var i = 0; i < _data.Count; i++)
            {
                var data = _data[i];
                data.Selected = _isoGridState.ObjectFootprintStartPositions.ContainsKey(data.UserFurnitureId);
            }
        }

        #region IEnhancedScrollerDelegate

        int DataRowCount => Mathf.CeilToInt((float)_data.Count / _redecorateUiView.NumberOfCellsPerRow);

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return DataRowCount + 1; // +1 でダミー行追加
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            // 最後の行（ダミー）の場合は余白サイズを返す
            if (dataIndex >= DataRowCount)
            {
                return _redecorateUiView.BottomPadding;
            }

            return _redecorateUiView.CellViewSize;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(_redecorateUiView.CellViewPrefab) as RedecorateCellView;
            var cellsPerRow = _redecorateUiView.NumberOfCellsPerRow;

            // ダミー行の場合は範囲外のインデックスでnullデータを渡す
            if (dataIndex >= DataRowCount)
            {
                cellView.name = "Cell Padding";
                cellView.SetData(ref _data, _data.Count, _cellSelectedEvent);
                return cellView;
            }

            var di = dataIndex * cellsPerRow;
            cellView.name = $"Cell {di} to {di + cellsPerRow - 1}";
            cellView.SetData(ref _data, di, _cellSelectedEvent);

            return cellView;
        }

        #endregion
    }
}
