using Home.Service;
using UnityEngine;

namespace Home.View
{
    /// 家具上面に配置可能なグリッド領域を表すView
    /// 状態はIsoGridState/IsoGridServiceが保持し、ここは座標変換と参照のみを提供する
    [RequireComponent(typeof(Collider2D))]
    public class FragmentedIsoGrid : MonoBehaviour
    {
        [SerializeField] Vector2Int _size = Vector2Int.one;
        [SerializeField] IsoDraggableView _isoDraggableView;

        IsoCoordinateConverterService _converter;

        public Vector2Int Size => _size;
        public IsoDraggableView IsoDraggableView => _isoDraggableView;
        public float CellSize => IsoGridSettingsView.CellSize;

        void Awake()
        {
            _converter = new IsoCoordinateConverterService(
                IsoGridSettingsView.CellSize,
                IsoGridSettingsView.Angle
            );
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

        /// 親家具のUserFurnitureIdを取得
        public int GetParentUserFurnitureId()
        {
            return _isoDraggableView != null ? _isoDraggableView.UserFurnitureId : 0;
        }
    }
}
