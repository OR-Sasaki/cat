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
            // 床オブジェクトを配列に変換
            var objectPositions = _isoGridState.ObjectFootprintStartPositions
                .Select(kvp => new IsoGridObjectPosition
                {
                    UserFurnitureId = kvp.Key,
                    X = kvp.Value.x,
                    Y = kvp.Value.y
                })
                .ToArray();

            // 壁オブジェクトを配列に変換
            var wallObjectPositions = _isoGridState.WallObjectFootprintStartPositions
                .Select(kvp => new IsoGridWallObjectPosition
                {
                    UserFurnitureId = kvp.Key,
                    Side = (int)kvp.Value.Side,
                    X = kvp.Value.Position.x,
                    Z = kvp.Value.Position.y
                })
                .ToArray();

            // FragmentedIsoGrid上のオブジェクトを配列に変換
            var fragmentedObjectPositions = _isoGridState.FragmentedGrids
                .SelectMany(parent => parent.Value.ObjectPositions.Select(child => new IsoGridFragmentedObjectPosition
                {
                    ParentUserFurnitureId = parent.Key,
                    UserFurnitureId = child.Key,
                    X = child.Value.Position.x,
                    Y = child.Value.Position.y,
                    Depth = child.Value.Depth
                }))
                .ToArray();

            var saveData = new IsoGridSaveData
            {
                ObjectPositions = objectPositions,
                WallObjectPositions = wallObjectPositions,
                FragmentedObjectPositions = fragmentedObjectPositions
            };

            // UserStateに保存
            _userState.IsoGridSaveData = saveData;

            // PlayerPrefsに保存
            _playerPrefsService.Save(PlayerPrefsKey.IsoGrid, saveData);

            Debug.Log($"IsoGridSaveService: Saved {objectPositions.Length} floor objects, {wallObjectPositions.Length} wall objects, and {fragmentedObjectPositions.Length} fragmented objects to IsoGrid");
        }
    }
}
