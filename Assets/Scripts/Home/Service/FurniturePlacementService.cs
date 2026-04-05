using System.Collections.Generic;
using System.Linq;
using Cat.Furniture;
using Home.State;
using Home.View;
using UnityEngine;

namespace Home.Service
{
    /// 家具のシーンへの配置・削除を行うService
    /// Drag時はIsoDragService内で処理しており、このクラスは使っていない
    public class FurniturePlacementService
    {
        readonly IsoGridState _isoGridState;
        readonly IsoGridService _isoGridService;

        public FurniturePlacementService(
            IsoGridState isoGridState,
            IsoGridService isoGridService)
        {
            _isoGridState = isoGridState;
            _isoGridService = isoGridService;
        }

        #region 共通

        /// 家具を空き位置に配置する（PlacementTypeに基づいて床/壁を自動判定）
        /// 戻り値: 配置したワールド座標（配置失敗時はnull）
        public Vector3? PlaceFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
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
                    _isoGridService.RemoveFragmentedObject(targetView.CurrentFragmentedGrid.GetParentUserFurnitureId(), userFurnitureId);
                }
            }

            // 自身が持つFragmentedIsoGridに載っている子家具のStateエントリを全て除去
            // （GameObjectはDestroyで一緒に破棄されるが、Stateに孤児エントリが残るのを防ぐ）
            var childGrids = targetView.GetComponentsInChildren<FragmentedIsoGrid>();
            foreach (var grid in childGrids)
            {
                var parentId = grid.GetParentUserFurnitureId();
                if (parentId == 0) continue;

                var childIds = grid.GetPlacedObjectPositions().Keys.ToList();
                foreach (var childId in childIds)
                {
                    _isoGridService.RemoveFragmentedObject(parentId, childId);
                }
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

            // プレハブをインスタンス化
            var instance = Object.Instantiate(furniture.SceneObject);
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
            for (var y = 0; y < _isoGridState.GridHeight - footprintSize.y + 1; y++)
            {
                for (var x = 0; x < _isoGridState.GridWidth - footprintSize.x + 1; x++)
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

            // プレハブをインスタンス化
            var instance = Object.Instantiate(furniture.SceneObject);
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
            var maxWidth = side == WallSide.Left ? _isoGridState.GridHeight : _isoGridState.GridWidth;
            var maxHeight = _isoGridState.WallHeight;

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

            // FragmentedIsoGridを取得
            var fragmentedGrid = parentView.GetComponentInChildren<FragmentedIsoGrid>();
            if (fragmentedGrid is null)
            {
                Debug.LogWarning($"[FurniturePlacementService] FragmentedIsoGrid not found on parent furniture {parentUserFurnitureId}");
                return null;
            }

            var footprintSize = furniture.SceneObject.FootprintSize;

            // 配置可能かチェック
            if (!fragmentedGrid.CanPlace(localGridPos, footprintSize, false, userFurnitureId))
            {
                Debug.LogWarning($"[FurniturePlacementService] Cannot place fragmented furniture at {localGridPos}: parentId={parentUserFurnitureId}, userFurnitureId={userFurnitureId}");
                return null;
            }

            // プレハブをインスタンス化
            var instance = Object.Instantiate(furniture.SceneObject, fragmentedGrid.transform, true);
            instance.SetUserFurnitureId(userFurnitureId);
            instance.SetPlacementType(PlacementType.Floor);

            // FragmentedIsoGridにオブジェクトを配置
            fragmentedGrid.PlaceObject(localGridPos, footprintSize, userFurnitureId);

            // Depthを計算（親のFragmentedIsoGridの入れ子の深さ）
            var depth = _isoGridService.CalculateFragmentedGridDepth(fragmentedGrid);

            // Stateにも記録
            _isoGridService.PlaceFragmentedObject(parentUserFurnitureId, userFurnitureId, localGridPos, depth);

            // ワールド座標を計算してセット
            var pivotOffset = instance.PivotGridPosition;
            var worldPos = fragmentedGrid.LocalGridToWorld(localGridPos + pivotOffset);
            instance.SetPosition(worldPos);
            instance.SetCurrentFragmentedGrid(fragmentedGrid);

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
    }
}

