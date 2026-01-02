using UnityEngine;

namespace Cat
{
    /// <summary>
    /// ドラッグ可能なアイソメトリックオブジェクト
    /// Pivotはオブジェクトが床に接している面の中心に設定されている前提
    /// </summary>
    public class IsoDraggable : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] int _dragSortingOrderBoost = 100;

        [Header("Footprint Settings")]
        [SerializeField] Vector2Int _footprintSize = Vector2Int.one;
        [Tooltip("オブジェクト内のピボット位置（IsoGrid座標）。フットプリントの左下を(0,0)とする")]
        [SerializeField] Vector2Int _pivotGridPosition = Vector2Int.zero;

        IsoGridSystem _gridSystem;
        bool _isDragging;
        Vector3 _dragOffset;
        Camera _mainCamera;
        SpriteRenderer _spriteRenderer;
        int _originalSortingOrder;

        void Awake()
        {
            _mainCamera = Camera.main;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _gridSystem = FindFirstObjectByType<IsoGridSystem>();

            if (_gridSystem == null)
            {
                Debug.LogError($"[IsoDraggable] IsoGridSystem not found in scene.");
            }
        }

        void OnMouseDown()
        {
            if (_gridSystem == null)
            {
                Debug.LogError($"[IsoDraggable] IsoGridSystem is not assigned.");
                return;
            }

            _isDragging = true;

            // マウス位置とオブジェクト位置の差分を記録
            var mouseWorldPos = GetMouseWorldPosition();
            _dragOffset = transform.position - mouseWorldPos;

            // ソートオーダーを一時的に上げる
            if (_spriteRenderer != null)
            {
                _originalSortingOrder = _spriteRenderer.sortingOrder;
                _spriteRenderer.sortingOrder += _dragSortingOrderBoost;
            }
        }

        void OnMouseDrag()
        {
            if (!_isDragging) return;

            var mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos + _dragOffset;
        }

        void OnMouseUp()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // グリッドにスナップ（pivotOffsetを考慮）
            if (_gridSystem != null)
            {
                transform.position = SnapToGridWithPivot(transform.position);
            }

            // ソートオーダーを元に戻す
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _originalSortingOrder;
            }
        }

        /// <summary>
        /// ピボット位置を考慮してグリッド頂点にスナップ
        /// </summary>
        Vector3 SnapToGridWithPivot(Vector3 worldPos)
        {
            // 現在のワールド位置をグリッド座標に変換
            var gridPos = _gridSystem.WorldToFloorGrid(worldPos);

            // 軸ベクトルを計算
            var angleRad = _gridSystem.Angle * Mathf.Deg2Rad;
            var cellSize = _gridSystem.CellSize;
            var xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;

            // フットプリントの左下位置を計算
            var footprintStartPos = gridPos - _pivotGridPosition;

            // ピボット位置のグリッド頂点のワールド座標を計算
            var pivotVertex = (footprintStartPos.x + _pivotGridPosition.x) * xAxis +
                              (footprintStartPos.y + _pivotGridPosition.y) * yAxis;
            return _gridSystem.Origin + (Vector3)pivotVertex;
        }

        /// <summary>
        /// マウスのワールド座標を取得
        /// </summary>
        Vector3 GetMouseWorldPosition()
        {
            var mousePos = Input.mousePosition;
            mousePos.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(mousePos);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!_isDragging || _gridSystem == null) return;

            // スナップ先のグリッド座標を取得（pivotの位置）
            var pivotGridPos = _gridSystem.WorldToFloorGrid(transform.position);

            // footprintの開始位置（左下）を計算
            var footprintStartPos = pivotGridPos - _pivotGridPosition;

            // 軸ベクトルを計算
            var angleRad = _gridSystem.Angle * Mathf.Deg2Rad;
            var cellSize = _gridSystem.CellSize;
            var xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var origin = _gridSystem.Origin;

            // 全セルが有効かチェック
            var allValid = true;
            for (var x = 0; x < _footprintSize.x; x++)
            {
                for (var y = 0; y < _footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStartPos.x + x, footprintStartPos.y + y);
                    if (!_gridSystem.IsValidFloorPosition(cellPos))
                    {
                        allValid = false;
                        break;
                    }
                }
                if (!allValid) break;
            }

            // 有効な位置なら緑、無効なら赤
            var color = allValid ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            Gizmos.color = color;

            // footprintSize分のセルを描画
            for (var x = 0; x < _footprintSize.x; x++)
            {
                for (var y = 0; y < _footprintSize.y; y++)
                {
                    var cellOrigin = origin + (Vector3)((footprintStartPos.x + x) * xAxis + (footprintStartPos.y + y) * yAxis);

                    var corner0 = cellOrigin;
                    var corner1 = cellOrigin + (Vector3)xAxis;
                    var corner2 = cellOrigin + (Vector3)xAxis + (Vector3)yAxis;
                    var corner3 = cellOrigin + (Vector3)yAxis;

                    // 塗りつぶし
                    DrawFilledQuad(corner0, corner1, corner2, corner3, color);
                }
            }

            // 外周のアウトラインを描画
            var footprintOrigin = origin + (Vector3)(footprintStartPos.x * xAxis + footprintStartPos.y * yAxis);
            var outerCorner0 = footprintOrigin;
            var outerCorner1 = footprintOrigin + (Vector3)(xAxis * _footprintSize.x);
            var outerCorner2 = footprintOrigin + (Vector3)(xAxis * _footprintSize.x + yAxis * _footprintSize.y);
            var outerCorner3 = footprintOrigin + (Vector3)(yAxis * _footprintSize.y);

            Gizmos.color = allValid ? Color.green : Color.red;
            Gizmos.DrawLine(outerCorner0, outerCorner1);
            Gizmos.DrawLine(outerCorner1, outerCorner2);
            Gizmos.DrawLine(outerCorner2, outerCorner3);
            Gizmos.DrawLine(outerCorner3, outerCorner0);
        }

        void DrawFilledQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color color)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { p0, p1, p2, p3 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

            Gizmos.color = color;
            Gizmos.DrawMesh(mesh);
        }
#endif
    }
}
