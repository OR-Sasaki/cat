using UnityEngine;
using VContainer;

namespace Home.View
{
    /// <summary>
    /// ドラッグ可能なアイソメトリックオブジェクト
    /// Pivotはオブジェクトが床に接している面の中心に設定されている前提
    /// </summary>
    public class IsoDraggableView : MonoBehaviour
    {
        static readonly int DragSortingOrderBoost = 100;

        [Header("Footprint Settings")]
        [SerializeField] Vector2Int _footprintSize = Vector2Int.one;
        [Tooltip("オブジェクト内のピボット位置（IsoGrid座標）。フットプリントの左下を(0,0)とする")]
        [SerializeField] Vector2Int _pivotGridPosition = Vector2Int.zero;
        [SerializeField] Transform _viewPivot;

        [Header("Object Settings")]
        [SerializeField] int _objectId;

        /// <summary>
        /// 現在ドラッグ中かどうか
        /// </summary>
        public bool IsDragging => _isDragging;

        // Gizmo描画用のプロパティ
        public IsoGridSystemView GridSystem => _gridSystem;
        public Vector2Int FootprintSize => _footprintSize;
        public Vector2Int PivotGridPosition => _pivotGridPosition;
        public int ObjectId => _objectId;

        [Inject] IsoGridSystemView _gridSystem;
        Vector2Int _currentFootprintStartPos;
        Vector2Int _dragStartFootprintPos;
        bool _isPlaced;
        bool _isDragging;
        Vector3 _dragOffset;
        SpriteRenderer _spriteRenderer;
        int _originalSortingOrder;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (GetComponent<IsoDraggableGizmo>() == null)
            {
                gameObject.AddComponent<IsoDraggableGizmo>();
            }
#endif
        }

        public float ViewPivotY => _viewPivot.position.y;

        /// <summary>
        /// ドラッグ開始
        /// </summary>
        public void BeginDrag(Vector3 mouseWorldPos)
        {
            _isDragging = true;

            // マウス位置とオブジェクト位置の差分を記録
            _dragOffset = transform.position - mouseWorldPos;

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = _currentFootprintStartPos;
            if (_isPlaced)
            {
                _gridSystem.RemoveObject(_currentFootprintStartPos, _footprintSize);
                _isPlaced = false;
            }

            // ソートオーダーを一時的に上げる
            if (_spriteRenderer != null)
            {
                _originalSortingOrder = _spriteRenderer.sortingOrder;
                _spriteRenderer.sortingOrder += DragSortingOrderBoost;
            }
        }

        /// <summary>
        /// ドラッグ中の更新
        /// </summary>
        public void UpdateDrag(Vector3 mouseWorldPos)
        {
            if (!_isDragging) return;

            transform.position = mouseWorldPos + _dragOffset;
        }

        /// <summary>
        /// ドラッグ終了
        /// </summary>
        public void EndDrag()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // グリッドにスナップして配置
            if (_gridSystem != null)
            {
                var gridPos = _gridSystem.WorldToFloorGrid(transform.position);
                var newFootprintStartPos = gridPos - _pivotGridPosition;

                // 配置可能かチェック
                if (_gridSystem.CanPlaceObject(newFootprintStartPos, _footprintSize, _objectId))
                {
                    // 新しい位置に配置
                    _currentFootprintStartPos = newFootprintStartPos;
                }
                else
                {
                    // 配置不可能なら元の位置に戻す
                    _currentFootprintStartPos = _dragStartFootprintPos;
                }

                transform.position = SnapToGrid(_currentFootprintStartPos);
                _gridSystem.PlaceObject(_currentFootprintStartPos, _footprintSize, _objectId);
                _isPlaced = true;
            }

            // ソートオーダーを元に戻す
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _originalSortingOrder;
            }
        }

        /// <summary>
        /// フットプリント開始位置からピボット位置のワールド座標を計算
        /// </summary>
        Vector3 SnapToGrid(Vector2Int footprintStartPos)
        {
            // 軸ベクトルを計算
            var angleRad = _gridSystem.Angle * Mathf.Deg2Rad;
            var cellSize = _gridSystem.CellSize;
            var xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;

            // ピボット位置のグリッド頂点のワールド座標を計算
            var pivotVertex = (footprintStartPos.x + _pivotGridPosition.x) * xAxis +
                              (footprintStartPos.y + _pivotGridPosition.y) * yAxis;
            return _gridSystem.Origin + (Vector3)pivotVertex;
        }
    }
}
