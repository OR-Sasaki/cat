using System.Collections.Generic;
using Home.Service;
using UnityEngine;
using VContainer;

namespace Home.View
{
    /// 家具上面に配置可能なグリッド領域を管理するコンポーネント
    [RequireComponent(typeof(Collider2D))]
    public class FragmentedIsoGrid : MonoBehaviour
    {
        [SerializeField] Vector2Int _size = Vector2Int.one;
        [SerializeField] IsoDraggableView _isoDraggableView;

        IsoCoordinateConverterService _converter;

        /// グリッドセル配列（IsoGridStateと同様のパターン）
        int[,] _cells;

        /// 配置されたオブジェクトのID→位置マッピング
        Dictionary<int, Vector2Int> _placedObjectPositions;

        public Vector2Int Size => _size;
        public IsoDraggableView IsoDraggableView => _isoDraggableView;
        public float CellSize => IsoGridSettingsView.CellSize;

        void Awake()
        {
            _converter = new IsoCoordinateConverterService(
                IsoGridSettingsView.CellSize,
                IsoGridSettingsView.Angle
            );
            _cells = new int[_size.x, _size.y];
            _placedObjectPositions = new Dictionary<int, Vector2Int>();
        }

        void Start()
        {
#if UNITY_EDITOR
            AttachGizmo();
#endif
        }

#if UNITY_EDITOR
        void AttachGizmo()
        {
            if (GetComponent<FragmentedIsoGridGizmo>() != null) return;

            var gizmo = gameObject.AddComponent<FragmentedIsoGridGizmo>();
            gizmo.Initialize(this);
        }
#endif

        #region 座標変換

        /// ローカルグリッド座標をワールド座標に変換
        public Vector3 LocalGridToWorld(Vector2Int localGridPos)
        {
            var offset = _converter.GridToOffset(localGridPos);
            return transform.position + (Vector3)offset;
        }

        /// ワールド座標をローカルグリッド座標に変換
        public Vector2Int WorldToLocalGrid(Vector3 worldPos)
        {
            var offset = (Vector2)(worldPos - transform.position);
            return _converter.OffsetToGrid(offset);
        }

        /// ローカルグリッド座標が有効範囲内かチェック
        public bool IsValidLocalPosition(Vector2Int localGridPos)
        {
            return localGridPos.x >= 0 && localGridPos.x < _size.x
                && localGridPos.y >= 0 && localGridPos.y < _size.y;
        }

        #endregion

        #region 配置管理

        /// 指定位置に家具が配置可能かどうかを判定
        /// セル占有状態、footprintサイズ、IsWallPlacement制約をチェック
        public bool CanPlace(Vector2Int localGridPos, Vector2Int footprint, bool isWallPlacement, int selfUserFurnitureId = 0)
        {
            // 壁配置家具は配置不可
            if (isWallPlacement) return false;

            // footprint範囲が有効範囲内かチェック
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!IsValidLocalPosition(cellPos)) return false;

                    var cellValue = _cells[cellPos.x, cellPos.y];
                    if (cellValue != 0 && cellValue != selfUserFurnitureId) return false;
                }
            }

            return true;
        }

        /// 家具をこのグリッドの指定位置に配置
        public void PlaceObject(Vector2Int localGridPos, Vector2Int footprint, int userFurnitureId)
        {
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!IsValidLocalPosition(cellPos)) continue;

                    _cells[cellPos.x, cellPos.y] = userFurnitureId;
                }
            }

            _placedObjectPositions[userFurnitureId] = localGridPos;
        }

        /// 指定IDのオブジェクトを解除
        public void RemoveObject(int userFurnitureId, Vector2Int footprint)
        {
            if (!_placedObjectPositions.TryGetValue(userFurnitureId, out var localGridPos))
            {
                Debug.LogWarning($"[FragmentedIsoGrid] Object {userFurnitureId} not found");
                return;
            }

            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!IsValidLocalPosition(cellPos)) continue;

                    _cells[cellPos.x, cellPos.y] = 0;
                }
            }

            _placedObjectPositions.Remove(userFurnitureId);
        }

        /// 指定IDのオブジェクトのフットプリント開始位置を取得
        public Vector2Int GetObjectFootprintStart(int userFurnitureId)
        {
            return _placedObjectPositions[userFurnitureId];
        }

        /// 指定IDのオブジェクトがこのグリッドに配置されているか
        public bool HasObject(int userFurnitureId)
        {
            return _placedObjectPositions.ContainsKey(userFurnitureId);
        }

        /// 配置されているオブジェクトの位置一覧を取得
        public IReadOnlyDictionary<int, Vector2Int> GetPlacedObjectPositions()
        {
            return _placedObjectPositions;
        }

        /// 親家具のUserFurnitureIdを取得
        public int GetParentUserFurnitureId()
        {
            return _isoDraggableView != null ? _isoDraggableView.UserFurnitureId : 0;
        }

        #endregion
    }
}
