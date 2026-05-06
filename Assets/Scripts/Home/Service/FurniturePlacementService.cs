using Cat.Furniture;
using Home.State;
using Home.View;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Home.Service
{
    /// 家具のシーンへの配置・削除を行うService
    /// Drag時はIsoDragService内で処理しており、このクラスは使っていない
    public class FurniturePlacementService
    {
        readonly IsoGridState _isoGridState;
        readonly IsoGridService _isoGridService;
        readonly RoomBaseState _roomBaseState;
        readonly RoomBackGroundView _roomBackGroundView;

        [Inject]
        public FurniturePlacementService(
            IsoGridState isoGridState,
            IsoGridService isoGridService,
            RoomBaseState roomBaseState,
            RoomBackGroundView roomBackGroundView)
        {
            _isoGridState = isoGridState;
            _isoGridService = isoGridService;
            _roomBaseState = roomBaseState;
            _roomBackGroundView = roomBackGroundView;
        }

        #region 共通

        /// 家具を空き位置に配置する（PlacementTypeに基づいて床/壁を自動判定）
        /// 戻り値: 配置したワールド座標（配置失敗時はnull）
        public Vector3? PlaceFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            // PlacementType.Base は専用経路 PlaceBase を使うこと。誤って Floor 経路に流れないよう冒頭で弾く。
            if (furniture.PlacementType == PlacementType.Base)
            {
                Debug.LogWarning($"[FurniturePlacementService] Use PlaceBase for Base placement type: userFurnitureId={userFurnitureId}");
                return null;
            }

            if (furniture.SceneObject is null) return null;

            if (furniture.PlacementType == PlacementType.Wall)
            {
                return PlaceWallFurniture(userFurnitureId, furniture);
            }

            return PlaceFloorFurniture(userFurnitureId, furniture);
        }

        /// 家具をシーンとグリッドから削除する
        public bool RemoveFurniture(int userFurnitureId, Furniture furniture)
        {
            if (furniture.SceneObject is null) return false;

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

            if (targetView is null)
            {
                Debug.LogWarning($"[FurniturePlacementService] IsoDraggableView with UserFurnitureId {userFurnitureId} not found");
                return false;
            }

            // PlacementTypeに基づいてグリッドから削除
            if (furniture.PlacementType == PlacementType.Wall)
            {
                _isoGridService.RemoveWallObject(userFurnitureId, targetView.FootprintSize);
            }
            else
            {
                if (targetView.CurrentFragmentedGrid is null)
                {
                    // Fragmentedに乗っていない場合は、Floor上から削除
                    _isoGridService.RemoveFloorObject(userFurnitureId, targetView.FootprintSize);
                }
                else
                {
                    // Fragmentedに乗っている場合は、Fragmented上から削除
                    _isoGridService.RemoveFragmentedObject(targetView.CurrentFragmentedGrid, userFurnitureId, targetView.FootprintSize);
                }
            }

            // 自身が持つFragmentedIsoGridに紐づくStateエントリを破棄
            // （GameObjectはDestroyで一緒に破棄されるが、Stateに孤児エントリが残るのを防ぐ）
            var childGrids = targetView.GetComponentsInChildren<FragmentedIsoGrid>();
            foreach (var grid in childGrids)
            {
                _isoGridService.UnregisterFragmentedGrid(grid);
            }

            // シーンからオブジェクトを削除
            Object.Destroy(targetView.gameObject);

            return true;
        }

        #endregion

        #region 床配置

        /// 床家具を空き位置に配置する
        Vector3? PlaceFloorFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            var footprintSize = furniture.SceneObject.FootprintSize;
            var availablePos = FindAvailableFloorPosition(footprintSize);
            if (availablePos is null)
            {
                Debug.LogWarning("[FurniturePlacementService] No available floor position found");
                return null;
            }

            return PlaceFloorFurnitureAt(userFurnitureId, furniture, availablePos.Value);
        }

        /// 床家具を指定位置に配置する
        /// 戻り値: 配置したワールド座標（配置失敗時はnull）
        public Vector3? PlaceFloorFurnitureAt(int userFurnitureId, Cat.Furniture.Furniture furniture, Vector2Int gridPos)
        {
            if (furniture.SceneObject is null) return null;

            var footprintSize = furniture.SceneObject.FootprintSize;

            // 配置可能かどうかを検証
            if (!_isoGridService.CanPlaceFloorObject(gridPos, footprintSize))
            {
                Debug.LogWarning($"[FurniturePlacementService] Cannot place floor furniture at {gridPos}: userFurnitureId={userFurnitureId}");
                return null;
            }

            // プレハブをインスタンス化（アクティブシーンがHomeでない場合に備え、明示的にHomeシーンへ移動）
            var instance = Object.Instantiate(furniture.SceneObject);
            SceneManager.MoveGameObjectToScene(instance.gameObject, _isoGridService.HomeScene);
            instance.SetUserFurnitureId(userFurnitureId);
            instance.SetPlacementType(PlacementType.Floor);

            // グリッドにオブジェクトを配置
            _isoGridService.PlaceFloorObject(gridPos, footprintSize, userFurnitureId);

            // ワールド座標を計算してViewを移動
            var pivotOffset = instance.PivotGridPosition;
            var worldPos = _isoGridService.FloorGridToWorld(gridPos + pivotOffset);
            instance.SetPosition(worldPos);

#if UNITY_EDITOR
            var gizmo = instance.GetComponent<IsoDraggableGizmo>();
            if (gizmo != null)
            {
                var settingsView = Object.FindFirstObjectByType<IsoGridSettingsView>();
                gizmo.Init(settingsView, _isoGridService);
            }
#endif

            return worldPos;
        }

        /// 指定サイズの床家具を配置できる空き位置を探す
        Vector2Int? FindAvailableFloorPosition(Vector2Int footprintSize)
        {
            for (var y = 0; y < _isoGridState.Floor.Size.y - footprintSize.y + 1; y++)
            {
                for (var x = 0; x < _isoGridState.Floor.Size.x - footprintSize.x + 1; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (_isoGridService.CanPlaceFloorObject(pos, footprintSize))
                    {
                        return pos;
                    }
                }
            }
            return null;
        }

        #endregion

        #region 壁配置

        /// 壁家具を空き位置に配置する（左壁→右壁の順で探索）
        Vector3? PlaceWallFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            var footprintSize = furniture.SceneObject.FootprintSize;

            // 左壁で空き位置を探す
            var leftPos = FindAvailableWallPosition(WallSide.Left, footprintSize);
            if (leftPos != null)
            {
                return PlaceWallFurnitureAt(userFurnitureId, furniture, WallSide.Left, leftPos.Value);
            }

            // 右壁で空き位置を探す
            var rightPos = FindAvailableWallPosition(WallSide.Right, footprintSize);
            if (rightPos != null)
            {
                return PlaceWallFurnitureAt(userFurnitureId, furniture, WallSide.Right, rightPos.Value);
            }

            Debug.LogWarning("[FurniturePlacementService] No available wall position found");
            return null;
        }

        /// 壁の指定位置に家具を配置する
        public Vector3? PlaceWallFurnitureAt(int userFurnitureId, Cat.Furniture.Furniture furniture, WallSide side, Vector2Int gridPos)
        {
            if (furniture.SceneObject is null) return null;

            var footprintSize = furniture.SceneObject.FootprintSize;

            // 配置可能かどうかを検証
            if (!_isoGridService.CanPlaceWallObject(side, gridPos, footprintSize))
            {
                Debug.LogWarning($"[FurniturePlacementService] Cannot place wall furniture at {side}:{gridPos}");
                return null;
            }

            // プレハブをインスタンス化（アクティブシーンがHomeでない場合に備え、明示的にHomeシーンへ移動）
            var instance = Object.Instantiate(furniture.SceneObject);
            SceneManager.MoveGameObjectToScene(instance.gameObject, _isoGridService.HomeScene);
            instance.SetUserFurnitureId(userFurnitureId);
            instance.SetPlacementType(PlacementType.Wall);
            instance.SetWallSide(side);

            // グリッドにオブジェクトを配置
            _isoGridService.PlaceWallObject(side, gridPos, footprintSize, userFurnitureId);

            // ワールド座標を計算してViewを移動
            var pivotOffset = instance.PivotGridPosition;
            var worldPos = _isoGridService.WallGridToWorld(side, gridPos + pivotOffset);
            instance.SetPosition(worldPos);

#if UNITY_EDITOR
            var gizmo = instance.GetComponent<IsoDraggableGizmo>();
            if (gizmo != null)
            {
                var settingsView = Object.FindFirstObjectByType<IsoGridSettingsView>();
                gizmo.Init(settingsView, _isoGridService);
            }
#endif

            return worldPos;
        }

        /// 壁の空き位置を探す
        Vector2Int? FindAvailableWallPosition(WallSide side, Vector2Int footprintSize)
        {
            var wallEntry = side == WallSide.Left ? _isoGridState.LeftWall : _isoGridState.RightWall;
            var maxWidth = wallEntry.Size.x;
            var maxHeight = wallEntry.Size.y;

            for (var z = 0; z < maxHeight - footprintSize.y + 1; z++)
            {
                for (var pos = 0; pos < maxWidth - footprintSize.x + 1; pos++)
                {
                    var gridPos = new Vector2Int(pos, z);
                    if (_isoGridService.CanPlaceWallObject(side, gridPos, footprintSize))
                    {
                        return gridPos;
                    }
                }
            }
            return null;
        }

        #endregion

        #region FragmentedIsoGrid配置

        /// FragmentedIsoGrid上に家具を配置する
        /// 親家具のUserFurnitureIdと配置するローカル座標を指定
        public Vector3? PlaceFragmentedFurnitureAt(
            int parentUserFurnitureId,
            int userFurnitureId,
            Furniture furniture,
            Vector2Int localGridPos)
        {
            if (furniture.SceneObject is null) return null;

            // 壁家具はFragmentedIsoGrid上に配置不可
            if (furniture.PlacementType == PlacementType.Wall)
            {
                Debug.LogWarning($"[FurniturePlacementService] Cannot place wall furniture on fragmented grid: userFurnitureId={userFurnitureId}");
                return null;
            }

            // 親家具のIsoDraggableViewを探す
            var allDraggables = Object.FindObjectsByType<IsoDraggableView>(FindObjectsSortMode.None);
            IsoDraggableView parentView = null;
            foreach (var draggable in allDraggables)
            {
                if (draggable.UserFurnitureId == parentUserFurnitureId)
                {
                    parentView = draggable;
                    break;
                }
            }

            if (parentView is null)
            {
                Debug.LogWarning($"[FurniturePlacementService] Parent furniture {parentUserFurnitureId} not found");
                return null;
            }

            // FragmentedIsoGridを取得（親自身に紐づくものだけを対象とし、子家具のgridを拾わないようにする）
            FragmentedIsoGrid fragmentedGrid = null;
            var candidateGrids = parentView.GetComponentsInChildren<FragmentedIsoGrid>();
            foreach (var candidate in candidateGrids)
            {
                if (candidate.IsoDraggableView == parentView)
                {
                    fragmentedGrid = candidate;
                    break;
                }
            }
            if (fragmentedGrid is null)
            {
                Debug.LogWarning($"[FurniturePlacementService] FragmentedIsoGrid not found on parent furniture {parentUserFurnitureId}");
                return null;
            }

            var footprintSize = furniture.SceneObject.FootprintSize;

            // 配置可能かチェック
            if (!_isoGridService.CanPlaceFragmentedObject(fragmentedGrid, localGridPos, footprintSize, userFurnitureId))
            {
                Debug.LogWarning($"[FurniturePlacementService] Cannot place fragmented furniture at {localGridPos}: parentId={parentUserFurnitureId}, userFurnitureId={userFurnitureId}");
                return null;
            }

            // プレハブをインスタンス化
            var instance = Object.Instantiate(furniture.SceneObject, fragmentedGrid.transform, true);
            instance.SetUserFurnitureId(userFurnitureId);
            instance.SetPlacementType(PlacementType.Floor);

            // FragmentedIsoGridにオブジェクトを配置（Depthは内部で計算）
            _isoGridService.PlaceFragmentedObject(fragmentedGrid, localGridPos, footprintSize, userFurnitureId);

            // ワールド座標を計算してセット
            var pivotOffset = instance.PivotGridPosition;
            var worldPos = fragmentedGrid.LocalGridToWorld(localGridPos + pivotOffset);
            instance.SetPosition(worldPos);
            instance.SetCurrentFragmentedGrid(fragmentedGrid);

            var sortingOrder = IsoDraggableView.CalculateFragmentedSortingOrder(localGridPos, footprintSize);
            instance.SetSortingOrder(sortingOrder);

#if UNITY_EDITOR
            var gizmo = instance.GetComponent<IsoDraggableGizmo>();
            if (gizmo != null)
            {
                var settingsView = Object.FindFirstObjectByType<IsoGridSettingsView>();
                gizmo.Init(settingsView, _isoGridService);
            }
#endif

            return worldPos;
        }

        #endregion

        #region Base配置

        /// Base家具をRoomBackGroundView.BaseRoot配下に配置する。
        /// 旧Base破棄→新Base生成→State同期を1コール内で同期実行し、中間状態（0個または2個以上）を外部から観測不可にする。
        /// IsoDraggableView / IsoGridState / IsoGridService への参照は持たない（Baseはドラッグ・グリッド占有の対象外）。
        /// 戻り値: 配置に成功したか。falseの場合は既存配置状態は変更されない。
        public bool PlaceBase(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            if (furniture.PlacementType != PlacementType.Base)
            {
                Debug.LogWarning($"[FurniturePlacementService] PlaceBase called with non-Base PlacementType: {furniture.PlacementType}, userFurnitureId={userFurnitureId}");
                return false;
            }

            if (furniture.BaseSceneObject is null)
            {
                Debug.LogWarning($"[FurniturePlacementService] Furniture has no BaseSceneObject: userFurnitureId={userFurnitureId}");
                return false;
            }

            var baseRoot = _roomBackGroundView.BaseRoot;

            // 旧Baseを破棄（BaseRoot配下はBase専用領域。RoomBackGroundView の運用ルールで他GameObjectが置かれていない前提）
            foreach (Transform child in baseRoot)
            {
                Object.Destroy(child.gameObject);
            }

            // 新Baseを生成し、Homeシーンへ移動した上でBaseRoot配下にアタッチする（PlaceFloorFurnitureAt の Instantiate→MoveGameObjectToScene パターンを踏襲）
            var instance = Object.Instantiate(furniture.BaseSceneObject);
            SceneManager.MoveGameObjectToScene(instance.gameObject, _isoGridService.HomeScene);
            instance.SetParent(baseRoot, worldPositionStays: false);

            _roomBaseState.SetPlaced(userFurnitureId);

            return true;
        }

        #endregion
    }
}

