using Cat.Character;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;
using Home.View;
using UnityEngine;
using VContainer.Unity;

namespace Home.Service
{
    /// <summary>
    /// Closetスクローラーのビジネスロジック
    /// </summary>
    public class ClosetScrollerService : IEnhancedScrollerDelegate, IStartable
    {
        readonly OutfitSetting _outfitSetting;
        readonly CharacterView _characterView;
        readonly ClosetUiView _closetUiView;
        SmallList<ClosetOutfitData> _data = new();

        public ClosetScrollerService(OutfitSetting outfitSetting, CharacterView characterView, ClosetUiView closetUiView)
        {
            _outfitSetting = outfitSetting;
            _characterView = characterView;
            _closetUiView = closetUiView;

            _closetUiView.OnOpen.AddListener(Initialize);
        }

        void Initialize()
        {
            var scroller = _closetUiView.Scroller;
            scroller.Delegate = this;
            LoadData();
        }

        public void Start()
        {
            // ClosetScrollerServiceはInjectされないため、IStartableをRegisterすることで強制的にインスタンスを作る
        }

        void LoadData()
        {
            // 既存データのハンドラーをクリア
            for (var i = 0; i < _data.Count; i++)
            {
                _data[i].SelectedChanged = null;
            }

            _data.Clear();

            foreach (var outfit in _outfitSetting.Outfits)
            {
                _data.Add(new ClosetOutfitData(outfit));
            }

            _closetUiView.Scroller.ReloadData();
        }

        void OnCellViewSelected(ClosetRowCellView cellView)
        {
            if (cellView is null) return;

            var selectedDataIndex = cellView.DataIndex;

            for (var i = 0; i < _data.Count; i++)
            {
                _data[i].Selected = selectedDataIndex == i;
            }

            // 選択されたOutfitをキャラクターに適用
            var selectedData = _data[selectedDataIndex];
            if (selectedData?.Outfit != null)
            {
                _characterView.SetOutfit(selectedData.Outfit);
            }
        }

        #region IEnhancedScrollerDelegate

        int DataRowCount => Mathf.CeilToInt((float)_data.Count / _closetUiView.NumberOfCellsPerRow);

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return DataRowCount + 1; // +1 でダミー行追加
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            // 最後の行（ダミー）の場合は余白サイズを返す
            if (dataIndex >= DataRowCount)
            {
                return _closetUiView.BottomPadding;
            }

            return _closetUiView.CellViewSize;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(_closetUiView.CellViewPrefab) as ClosetCellView;
            var cellsPerRow = _closetUiView.NumberOfCellsPerRow;

            // ダミー行の場合は範囲外のインデックスでnullデータを渡す
            if (dataIndex >= DataRowCount)
            {
                cellView.name = "Cell Padding";
                cellView.SetData(ref _data, _data.Count, OnCellViewSelected);
                return cellView;
            }

            var di = dataIndex * cellsPerRow;
            cellView.name = $"Cell {di} to {di + cellsPerRow - 1}";
            cellView.SetData(ref _data, di, OnCellViewSelected);

            return cellView;
        }

        #endregion
    }
}

