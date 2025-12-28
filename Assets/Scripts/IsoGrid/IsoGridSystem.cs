using UnityEngine;

namespace Cat
{
    public class IsoGridSystem : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int _gridWidth = 10;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1f;
        [SerializeField, Range(0f, 90f)] float _angle = 30f;

        [Header("Wall Settings")]
        [SerializeField] int _wallHeight = 5;

        Vector2 _xAxis;
        Vector2 _yAxis;
        Vector2 _zAxis;
        float _determinant;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public float Angle => _angle;
        public int WallHeight => _wallHeight;
        public Vector3 Origin => transform.position;

        void Awake()
        {
            UpdateAxisVectors();
        }

        void OnValidate()
        {
            UpdateAxisVectors();
        }

        /// <summary>
        /// グリッドの軸ベクトルと行列式を更新
        /// </summary>
        void UpdateAxisVectors()
        {
            var angleRad = _angle * Mathf.Deg2Rad;
            // 床グリッドの軸（IsoGridTestと同じ）
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            // 壁方向（上向き）
            _zAxis = Vector2.up * _cellSize;

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
        /// 床グリッド座標をワールド座標に変換
        /// </summary>
        public Vector3 FloorGridToWorld(Vector2Int gridPos)
        {
            var worldOffset = gridPos.x * _xAxis + gridPos.y * _yAxis;
            return transform.position + (Vector3)worldOffset;
        }

        /// <summary>
        /// ワールド座標を最寄りの床グリッド位置にスナップ
        /// </summary>
        public Vector3 SnapToFloorGrid(Vector3 worldPos)
        {
            var gridPos = WorldToFloorGrid(worldPos);
            return FloorGridToWorld(gridPos);
        }

        /// <summary>
        /// 左壁グリッド座標をワールド座標に変換（Y軸方向の壁）
        /// </summary>
        public Vector3 LeftWallGridToWorld(int gridY, int gridZ)
        {
            var worldOffset = gridY * _yAxis + gridZ * _zAxis;
            return transform.position + (Vector3)worldOffset;
        }

        /// <summary>
        /// 右壁グリッド座標をワールド座標に変換（X軸方向の壁）
        /// </summary>
        public Vector3 RightWallGridToWorld(int gridX, int gridZ)
        {
            var worldOffset = gridX * _xAxis + gridZ * _zAxis;
            return transform.position + (Vector3)worldOffset;
        }

        /// <summary>
        /// 床グリッド座標が有効範囲内かチェック
        /// </summary>
        public bool IsValidFloorPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _gridWidth
                && gridPos.y >= 0 && gridPos.y < _gridHeight;
        }

        /// <summary>
        /// 左壁グリッド座標が有効範囲内かチェック
        /// </summary>
        public bool IsValidLeftWallPosition(int gridY, int gridZ)
        {
            return gridY >= 0 && gridY < _gridHeight
                && gridZ >= 0 && gridZ < _wallHeight;
        }

        /// <summary>
        /// 右壁グリッド座標が有効範囲内かチェック
        /// </summary>
        public bool IsValidRightWallPosition(int gridX, int gridZ)
        {
            return gridX >= 0 && gridX < _gridWidth
                && gridZ >= 0 && gridZ < _wallHeight;
        }
    }
}