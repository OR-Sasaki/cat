using UnityEngine;

namespace Home.Service
{
    /// アイソメトリック座標変換のコアロジックを提供するユーティリティ
    /// IsoGridServiceとFragmentedIsoGridで共通使用される
    public class IsoCoordinateConverterService
    {
        readonly Vector2 _xAxis;
        readonly Vector2 _yAxis;
        readonly float _determinant;
        readonly float _cellSize;

        public IsoCoordinateConverterService(float cellSize, float angle)
        {
            _cellSize = cellSize;
            var angleRad = angle * Mathf.Deg2Rad;
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            _determinant = _xAxis.x * _yAxis.y - _xAxis.y * _yAxis.x;

            if (Mathf.Approximately(_determinant, 0f))
                Debug.LogError($"[IsoCoordinateConverterService] Determinant is zero, cannot convert coordinates.");
        }

        /// グリッド座標をオフセット（origin基準）に変換
        public Vector2 GridToOffset(Vector2Int gridPos)
        {
            return gridPos.x * _xAxis + gridPos.y * _yAxis;
        }

        /// オフセットをグリッド座標に変換
        public Vector2Int OffsetToGrid(Vector2 offset)
        {
            var gridX = (offset.x * _yAxis.y - offset.y * _yAxis.x) / _determinant;
            var gridY = (_xAxis.x * offset.y - _xAxis.y * offset.x) / _determinant;
            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        public Vector2 XAxis => _xAxis;
        public Vector2 YAxis => _yAxis;
        public float CellSize => _cellSize;
    }
}
