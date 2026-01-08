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
        readonly MasterDataState _masterDataState;
        readonly FurnitureAssetState _furnitureAssetState;
        readonly UnityEvent<RedecorateRowCellView> _cellSelectedEvent = new();
        SmallList<RedecorateFurnitureData> _data = new();

        int _selectedIndex = -1;

        public RedecorateScrollerService(
            RedecorateUiView redecorateUiView,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState)
        {
            _redecorateUiView = redecorateUiView;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;

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

        public void Start()
        {
            // RedecorateScrollerServiceはInjectされないため、IStartableをRegisterすることで強制的にインスタンスを作る
        }

        void LoadData()
        {
            // 既存データのハンドラーをクリア
            for (var i = 0; i < _data.Count; i++)
            {
                _data[i].SelectedChanged.RemoveAllListeners();
            }

            _data.Clear();
            _selectedIndex = -1;

            if (_masterDataState.Furnitures is null)
            {
                Debug.LogError("[RedecorateScrollerService] MasterDataState.Furnitures is null");
                return;
            }

            foreach (var masterFurniture in _masterDataState.Furnitures)
            {
                var furniture = _furnitureAssetState.Get(masterFurniture.Name);
                if (furniture is null) continue;

                var furnitureData = new RedecorateFurnitureData(furniture);
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

            // 前の選択を解除
            if (_selectedIndex >= 0 && _selectedIndex < _data.Count)
            {
                _data[_selectedIndex].Selected = false;
            }

            // 新しい選択
            _selectedIndex = selectedDataIndex;
            selectedData.Selected = true;

            // TODO: 選択されたFurnitureを配置する処理
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