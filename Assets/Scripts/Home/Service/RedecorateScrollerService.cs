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
        readonly FurniturePlacementService _furniturePlacementService;
        readonly UnityEvent<RedecorateRowCellView> _cellSelectedEvent = new();
        SmallList<RedecorateFurnitureData> _data = new();

        public RedecorateScrollerService(
            RedecorateUiView redecorateUiView,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            IsoGridState isoGridState,
            FurniturePlacementService furniturePlacementService)
        {
            _redecorateUiView = redecorateUiView;
            _userState = userState;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _isoGridState = isoGridState;
            _furniturePlacementService = furniturePlacementService;
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
                _furniturePlacementService.RemoveFurniture(userFurnitureId, selectedData.Furniture);
            }
            else
            {
                // 未配置の場合：空き位置を探して配置
                _furniturePlacementService.PlaceFurniture(userFurnitureId, selectedData.Furniture);
            }

            UpdateSelectionStates();
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
