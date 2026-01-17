using UnityEngine;

namespace Home.View
{
    // ドラッグ可能なアイソメトリックオブジェクト
    // Pivotはオブジェクトが床に接している面の中心に設定されている前提
    public class IsoDraggableView : MonoBehaviour
    {
        [Header("Footprint Settings")]
        [SerializeField] Vector2Int _footprintSize = Vector2Int.one;
        [Tooltip("オブジェクト内のピボット位置（IsoGrid座標）。フットプリントの左下を(0,0)とする")]
        [SerializeField] Vector2Int _pivotGridPosition = Vector2Int.zero;
        [SerializeField] Transform _viewPivot;

        [Header("Object Settings")]
        [SerializeField] int _userFurnitureId;

        SpriteRenderer _spriteRenderer;
        int _originalSortingOrder;
        bool _isDragging;
        bool _isPlacedOnGrid;

        public bool IsDragging => _isDragging;
        public bool IsPlacedOnGrid => _isPlacedOnGrid;
        public Vector2Int FootprintSize => _footprintSize;
        public Vector2Int PivotGridPosition => _pivotGridPosition;
        public int UserFurnitureId => _userFurnitureId;
        public float ViewPivotY => _viewPivot.position.y;
        public Vector3 Position => transform.position;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalSortingOrder = _spriteRenderer.sortingOrder;
            }

#if UNITY_EDITOR
            if (GetComponent<IsoDraggableGizmo>() == null)
            {
                var c = gameObject.AddComponent<IsoDraggableGizmo>();
                c.SetIsoDraggableView(this);
            }
#endif
        }

        // 位置を設定
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        // ソートオーダーを上昇させる（ドラッグ中に前面表示）
        public void BoostSortingOrder(int boost)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _originalSortingOrder + boost;
            }
        }

        // ソートオーダーを元に戻す
        public void ResetSortingOrder()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _originalSortingOrder;
            }
        }

        // ドラッグ状態を設定
        public void SetDragging(bool isDragging)
        {
            _isDragging = isDragging;
        }

        // グリッド配置状態を設定
        public void SetPlacedOnGrid(bool isPlaced)
        {
            _isPlacedOnGrid = isPlaced;
        }
    }
}
