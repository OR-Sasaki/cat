using System.Linq;
using Home.State;
using Root.Service;
using Root.State;
using UnityEngine;
using VContainer.Unity;

namespace Home.Service
{
    /// IsoGridの状態をUserStateとPlayerPrefsに保存するサービス
    public class IsoGridSaveService : IStartable
    {
        readonly HomeState _homeState;
        readonly IsoGridState _isoGridState;
        readonly UserState _userState;
        readonly PlayerPrefsService _playerPrefsService;

        public IsoGridSaveService(
            HomeState homeState,
            IsoGridState isoGridState,
            UserState userState,
            PlayerPrefsService playerPrefsService)
        {
            _homeState = homeState;
            _isoGridState = isoGridState;
            _userState = userState;
            _playerPrefsService = playerPrefsService;
        }

        public void Start()
        {
            _homeState.OnStateChange.AddListener(OnStateChange);
        }

        void OnStateChange(HomeState.State previous, HomeState.State current)
        {
            if (previous == HomeState.State.Redecorate && current != HomeState.State.Redecorate)
            {
                Save();
            }
        }

        void Save()
        {
            var fragmentedGrids = _isoGridState.FragmentedGridsV2
                .Select(parent => new FragmentedGridSaveEntry
                {
                    ParentUserFurnitureId = parent.Key,
                    ObjectPositions = ToSaveEntries(parent.Value.ObjectPositions),
                })
                .ToArray();

            var saveData = new IsoGridSaveData
            {
                Floor = new GridSaveEntry { ObjectPositions = ToSaveEntries(_isoGridState.Floor.ObjectPositions) },
                LeftWall = new GridSaveEntry { ObjectPositions = ToSaveEntries(_isoGridState.LeftWall.ObjectPositions) },
                RightWall = new GridSaveEntry { ObjectPositions = ToSaveEntries(_isoGridState.RightWall.ObjectPositions) },
                FragmentedGrids = fragmentedGrids,
            };

            // UserStateに保存
            _userState.IsoGridSaveData = saveData;

            // PlayerPrefsに保存
            _playerPrefsService.Save(PlayerPrefsKey.IsoGrid, saveData);

            Debug.Log($"IsoGridSaveService: Saved Floor={saveData.Floor.ObjectPositions.Length}, LeftWall={saveData.LeftWall.ObjectPositions.Length}, RightWall={saveData.RightWall.ObjectPositions.Length}, FragmentedGrids={fragmentedGrids.Length}");
        }

        static ObjectPlacementSaveEntry[] ToSaveEntries(System.Collections.Generic.Dictionary<int, ObjectPlacement> positions)
        {
            return positions
                .Select(kvp => new ObjectPlacementSaveEntry
                {
                    UserFurnitureId = kvp.Key,
                    X = kvp.Value.Position.x,
                    Y = kvp.Value.Position.y,
                    Depth = kvp.Value.Depth,
                })
                .ToArray();
        }
    }
}
