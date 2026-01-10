using System.Collections;
using Home.State;
using NavMeshPlus.Components;
using UnityEngine;

namespace Home.View
{
    public class IsoGridSystemView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int _gridWidth = 10;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1f;
        [SerializeField, Range(0f, 90f)] float _angle = 30f;

        [Header("NavMesh Settings")]
        [SerializeField] NavMeshSurface _surface2D;

        [Header("Debug")]
        [SerializeField] string _floorCellsDebugView = "";

        Vector2 _xAxis;
        Vector2 _yAxis;
        float _determinant;

        IsoGridCell[,] _floorCells;

        public float CellSize => _cellSize;
        public float Angle => _angle;
        public Vector3 Origin => transform.position;

        void Awake()
        {
            UpdateAxisVectors();
            InitializeCells();
        }

        /// <summary>
        /// セル配列を初期化
        /// </summary>
        void InitializeCells()
        {
            _floorCells = new IsoGridCell[_gridWidth, _gridHeight];
        }

        void OnValidate()
        {
            UpdateAxisVectors();
        }

        void Update()
        {
            UpdateFloorCellsDebugView();
        }

        /// <summary>
        /// デバッグ用にセル配列の内容を文字列化
        /// </summary>
        void UpdateFloorCellsDebugView()
        {
            if (_floorCells == null) return;

            var sb = new System.Text.StringBuilder();
            // Y軸を上から下に表示（グリッド座標のY=maxが上）
            for (var y = _gridHeight - 1; y >= 0; y--)
            {
                for (var x = 0; x < _gridWidth; x++)
                {
                    var objectId = _floorCells[x, y].ObjectId;
                    sb.Append(objectId == 0 ? "." : objectId.ToString());
                    if (x < _gridWidth - 1) sb.Append(" ");
                }
                if (y > 0) sb.AppendLine();
            }
            _floorCellsDebugView = sb.ToString();
        }

        /// <summary>
        /// グリッドの軸ベクトルと行列式を更新
        /// </summary>
        void UpdateAxisVectors()
        {
            var angleRad = _angle * Mathf.Deg2Rad;
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _determinant = _xAxis.x * _yAxis.y - _xAxis.y * _yAxis.x;
        }

        /// <summary>
        /// ワールド座標を床グリッド座標に変換
        /// </summary>
        public Vector2Int WorldToFloorGrid(Vector3 worldPos)
        {
            var offset = (Vector2)(worldPos - transform.position);

            if (Mathf.Approximately(_determinant, 0f))
            {
                Debug.LogError($"[IsoGridSystem] Determinant is zero, cannot convert coordinates.");
                return Vector2Int.zero;
            }

            var gridX = (offset.x * _yAxis.y - offset.y * _yAxis.x) / _determinant;
            var gridY = (_xAxis.x * offset.y - _xAxis.y * offset.x) / _determinant;

            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        /// <summary>
        /// 床グリッド座標が有効範囲内かチェック
        /// </summary>
        bool IsValidFloorPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _gridWidth
                && gridPos.y >= 0 && gridPos.y < _gridHeight;
        }

        /// <summary>
        /// 指定範囲のセルにオブジェクトを配置
        /// </summary>
        public void PlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int objectId)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) continue;

                    _floorCells[cellPos.x, cellPos.y].ObjectId = objectId;
                }
            }

            StartCoroutine(BuildNavMeshDelayed());
        }

        IEnumerator BuildNavMeshDelayed()
        {
            yield return null;
            _surface2D.BuildNavMeshAsync();
        }

        /// <summary>
        /// 指定範囲のセルからオブジェクトを削除
        /// </summary>
        public void RemoveObject(Vector2Int footprintStart, Vector2Int footprintSize)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) continue;

                    _floorCells[cellPos.x, cellPos.y].Clear();
                }
            }
        }

        /// <summary>
        /// 指定範囲が配置可能かチェック（自分自身のIDは無視）
        /// </summary>
        public bool CanPlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int selfObjectId = 0)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) return false;

                    var cell = _floorCells[cellPos.x, cellPos.y];
                    if (cell.IsOccupied && cell.ObjectId != selfObjectId) return false;
                }
            }
            return true;
        }
    }
}
