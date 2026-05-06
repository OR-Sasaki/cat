using System.Linq;
using Cat.Furniture;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using Home.State;
using Home.View;
using Root.State;
using UnityEngine;
using UnityEngine.Events;
using VContainer;
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
        readonly RedecorateTinyService _redecorateTinyService;
        readonly RedecorateCameraService _redecorateCameraService;
        readonly RoomBaseState _roomBaseState;
        readonly UnityEvent<RedecorateRowCellView> _cellSelectedEvent = new();
        SmallList<RedecorateFurnitureData> _data = new();

        [Inject]
        public RedecorateScrollerService(
            RedecorateUiView redecorateUiView,
            UserState userState,
            MasterDataState masterDataState,
            FurnitureAssetState furnitureAssetState,
            IsoGridState isoGridState,
            FurniturePlacementService furniturePlacementService,
            RedecorateTinyService redecorateTinyService,
            RedecorateCameraService redecorateCameraService,
            RoomBaseState roomBaseState)
        {
            _redecorateUiView = redecorateUiView;
            _userState = userState;
            _masterDataState = masterDataState;
            _furnitureAssetState = furnitureAssetState;
            _isoGridState = isoGridState;
            _furniturePlacementService = furniturePlacementService;
            _redecorateTinyService = redecorateTinyService;
            _redecorateCameraService = redecorateCameraService;
            _roomBaseState = roomBaseState;
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
                _data.Add(furnitureData);
            }

            UpdateSelectionStates();
            _redecorateUiView.Scroller.ReloadData();
        }

        void OnCellViewSelected(RedecorateRowCellView cellView)
        {
            if (cellView is null) return;

            var selectedDataIndex = cellView.DataIndex;
            var selectedData = _data[selectedDataIndex];
            if (selectedData?.Furniture is null) return;

            var userFurnitureId = selectedData.UserFurnitureId;
            var furniture = selectedData.Furniture;

            // Base 専用分岐: 既選択 Base セル再タップは何もしない (Base 取り外し UI を提供しないため)
            // 未選択時のみ PlaceBase に委譲し、カメラ移動・Tiny 化はスキップして UI 反映のみ行う
            if (furniture.PlacementType == PlacementType.Base)
            {
                if (!selectedData.Selected)
                {
                    _furniturePlacementService.PlaceBase(userFurnitureId, furniture);
                }
                UpdateSelectionStates();
                return;
            }

            // selectedDataで判定するのは設計思想的に微妙だが、設置判定は処理コストが重いためこうしている
            // 直後にUpdateSelectionStates()をしていることで、設置判定をキャッシュしている
            if (!selectedData.Selected)
            {
                // 未配置の場合：空き位置を探して配置
                var placedPosition = _furniturePlacementService.PlaceFurniture(userFurnitureId, furniture);
                if (placedPosition.HasValue)
                {
                    _redecorateCameraService.MoveTo(placedPosition.Value);
                }
                _redecorateTinyService.SetTiny(true);
            }

            UpdateSelectionStates();
        }

        /// 全グリッドのObjectPositionsおよびRoomBaseStateに基づいて全データの選択状態を更新する
        public void UpdateSelectionStates()
        {
            for (var i = 0; i < _data.Count; i++)
            {
                var data = _data[i];
                if (data.Furniture.PlacementType == PlacementType.Base)
                {
                    // BaseはIsoGridStateに登録されないためRoomBaseState経由で判定する
                    data.Selected = _roomBaseState.PlacedBaseUserFurnitureId == data.UserFurnitureId;
                }
                else
                {
                    // 床、壁、またはFragmentedGridに登録されていればSelectedをtrueにする
                    data.Selected = _isoGridState.EnumerateAllGrids()
                        .Any(g => g.ObjectPositions.ContainsKey(data.UserFurnitureId));
                }
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
