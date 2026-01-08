using UnityEngine;

namespace Cat
{
    public class IsoGridGizmo : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int _gridWidth = 10;
        [SerializeField] int _gridHeight = 10;
        [SerializeField] float _cellSize = 1f;
        [SerializeField, Range(1, 10)] int _lineInterval = 1;
        [SerializeField, Range(0f, 90f)] float _angle = 30f;

        [Header("Floor Grid")]
        [SerializeField] bool _showFloorGrid = true;
        [SerializeField] Color _floorGridColor = Color.green;

        [Header("Wall Grid")]
        [SerializeField] bool _showWallGrid = true;
        [SerializeField] int _wallHeight = 5;
        [SerializeField] Color _leftWallColor = Color.cyan;
        [SerializeField] Color _rightWallColor = Color.magenta;

        void OnDrawGizmos()
        {
            if (_showFloorGrid)
            {
                DrawFloorGrid();
            }

            if (_showWallGrid)
            {
                DrawWallGrid();
            }
        }

        void DrawFloorGrid()
        {
            Gizmos.color = _floorGridColor;

            var angleRad = _angle * Mathf.Deg2Rad;
            var xAxis = new Vector3(Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * _cellSize;
            var yAxis = new Vector3(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * _cellSize;

            var origin = transform.position;

            // 縦線（右下方向）
            for (var x = 0; x <= _gridWidth; x += _lineInterval)
            {
                var start = origin + xAxis * x;
                var end = start + yAxis * _gridHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（左下方向）
            for (var y = 0; y <= _gridHeight; y += _lineInterval)
            {
                var start = origin + yAxis * y;
                var end = start + xAxis * _gridWidth;
                Gizmos.DrawLine(start, end);
            }
        }

        void DrawWallGrid()
        {
            var angleRad = _angle * Mathf.Deg2Rad;
            var xAxis = new Vector3(Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * _cellSize;
            var yAxis = new Vector3(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * _cellSize;
            var zAxis = Vector3.up * _cellSize;

            var origin = transform.position;

            // 左壁（yAxis方向に沿った壁、上に伸びる）
            Gizmos.color = _leftWallColor;
            var leftWallOrigin = origin;

            // 縦線（上方向）
            for (var y = 0; y <= _gridHeight; y += _lineInterval)
            {
                var start = leftWallOrigin + yAxis * y;
                var end = start + zAxis * _wallHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（左下方向）
            for (var z = 0; z <= _wallHeight; z += _lineInterval)
            {
                var start = leftWallOrigin + zAxis * z;
                var end = start + yAxis * _gridHeight;
                Gizmos.DrawLine(start, end);
            }

            // 右壁（xAxis方向に沿った壁、上に伸びる）
            Gizmos.color = _rightWallColor;
            var rightWallOrigin = origin;

            // 縦線（上方向）
            for (var x = 0; x <= _gridWidth; x += _lineInterval)
            {
                var start = rightWallOrigin + xAxis * x;
                var end = start + zAxis * _wallHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（右下方向）
            for (var z = 0; z <= _wallHeight; z += _lineInterval)
            {
                var start = rightWallOrigin + zAxis * z;
                var end = start + xAxis * _gridWidth;
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
