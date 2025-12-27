using System.Collections.Generic;
using System.Linq;
using Cat.Character;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;
using Home.View;
using Root.Service;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Home.Service
{
    /// <summary>
    /// Closetスクローラーのビジネスロジック
    /// </summary>
    public class ClosetScrollerService : IEnhancedScrollerDelegate, IStartable
    {
        readonly CharacterView _characterView;
        readonly ClosetUiView _closetUiView;
        readonly PlayerOutfitState _playerOutfitState;
        readonly PlayerOutfitService _playerOutfitService;
        readonly MasterDataState _masterDataState;
        readonly UnityEvent<ClosetRowCellView> _cellSelectedEvent = new();
        SmallList<ClosetOutfitData> _data = new();
        Dictionary<string, Cat.Character.Outfit> _loadedOutfits = new();
        int _pendingLoadCount;
        bool _isOutfitsLoaded;

        public ClosetScrollerService(
            CharacterView characterView,
            ClosetUiView closetUiView,
            PlayerOutfitState playerOutfitState,
            PlayerOutfitService playerOutfitService,
            MasterDataState masterDataState)
        {
            _characterView = characterView;
            _closetUiView = closetUiView;
            _playerOutfitState = playerOutfitState;
            _playerOutfitService = playerOutfitService;
            _masterDataState = masterDataState;

            _closetUiView.OnOpen.AddListener(Initialize);
            _cellSelectedEvent.AddListener(OnCellViewSelected);
        }

        void Initialize()
        {
            var scroller = _closetUiView.Scroller;
            scroller.Delegate = this;

            if (_isOutfitsLoaded)
            {
                LoadData();
            }
            else
            {
                LoadOutfitsAsync();
            }
        }

        public void Start()
        {
            // ClosetScrollerServiceはInjectされないため、IStartableをRegisterすることで強制的にインスタンスを作る
        }

        void LoadOutfitsAsync()
        {
            if (_masterDataState.Outfits is null || _masterDataState.Outfits.Length == 0)
            {
                Debug.LogError("[ClosetScrollerService] MasterDataState.Outfits is null or empty");
                return;
            }

            _pendingLoadCount = _masterDataState.Outfits.Length;

            foreach (var masterOutfit in _masterDataState.Outfits)
            {
                var outfitName = masterOutfit.Name;
                var address = $"{masterOutfit.Type}/{outfitName}.asset";
                var handle = Addressables.LoadAssetAsync<Cat.Character.Outfit>(address);
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && h.Result is not null)
                    {
                        _loadedOutfits[outfitName] = h.Result;
                    }

                    _pendingLoadCount--;
                    if (_pendingLoadCount <= 0)
                    {
                        _isOutfitsLoaded = true;
                        LoadData();
                    }
                };
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

            if (_masterDataState.Outfits is null)
            {
                Debug.LogError("[ClosetScrollerService] MasterDataState.Outfits is null");
                return;
            }

            var equippedOutfitIds = _playerOutfitState.GetAllEquippedOutfitIds();

            foreach (var masterOutfit in _masterDataState.Outfits)
            {
                if (!_loadedOutfits.TryGetValue(masterOutfit.Name, out var outfit))
                {
                    continue;
                }

                var closetData = new ClosetOutfitData(outfit);

                // 装備中のOutfitか確認
                if (equippedOutfitIds.TryGetValue(outfit.OutfitType, out var equippedId))
                {
                    closetData.Selected = masterOutfit.Id == equippedId;
                }

                _data.Add(closetData);
            }

            _closetUiView.Scroller.ReloadData();
        }

        void OnCellViewSelected(ClosetRowCellView cellView)
        {
            if (cellView is null) return;

            var selectedDataIndex = cellView.DataIndex;
            var selectedData = _data[selectedDataIndex];
            if (selectedData?.Outfit is null) return;

            var selectedOutfitType = selectedData.Outfit.OutfitType;

            // 同じOutfitTypeのものだけ選択状態を更新
            for (var i = 0; i < _data.Count; i++)
            {
                if (_data[i].Outfit.OutfitType == selectedOutfitType)
                {
                    _data[i].Selected = selectedDataIndex == i;
                }
            }

            // 選択されたOutfitをキャラクターに適用
            _characterView.SetOutfit(selectedData.Outfit);

            // PlayerOutfitServiceで保存
            var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Name == selectedData.Outfit.name);
            if (masterOutfit is not null)
            {
                _playerOutfitService.Equip(selectedOutfitType, masterOutfit.Id);
                _playerOutfitService.Save();
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

