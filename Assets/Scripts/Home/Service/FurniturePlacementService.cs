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

        /// 家具を空き位置に配置する
        public void PlaceFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            if (furniture.SceneObject == null) return;

            var footprintSize = furniture.SceneObject.FootprintSize;
            var availablePos = FindAvailablePosition(footprintSize);
            if (availablePos == null)
            {
                Debug.LogWarning("[FurniturePlacementService] No available position found");
                return;
            }

            PlaceFurnitureAt(userFurnitureId, furniture, availablePos.Value);
        }

        /// 家具を指定位置に配置する
        public void PlaceFurnitureAt(int userFurnitureId, Cat.Furniture.Furniture furniture, Vector2Int gridPos)
        {
            if (furniture.SceneObject == null) return;

            var footprintSize = furniture.SceneObject.FootprintSize;

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

        /// 家具をシーンとグリッドから削除する
        public bool RemoveFurniture(int userFurnitureId, Cat.Furniture.Furniture furniture)
        {
            if (furniture.SceneObject == null) return false;

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
                Debug.LogWarning($"[FurniturePlacementService] IsoDraggableView with UserFurnitureId {userFurnitureId} not found");
                return false;
            }

            // グリッドからオブジェクトを削除
            _isoGridService.RemoveObject(userFurnitureId, targetView.FootprintSize);

            // シーンからオブジェクトを削除
            Object.Destroy(targetView.gameObject);

            return true;
        }

        /// 指定サイズの家具を配置できる空き位置を探す
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
    }
}
