using UnityEngine;
using VContainer;

namespace Home.View
{
    // IsoGridをGizmo上で表示する
    // Editor内のみで動作する
    public class IsoGridGizmo : MonoBehaviour
    {
        IsoGridSettingsView _isoGridSettingsView;

        [Header("Gizmo Settings")]
        [SerializeField, Range(1, 10)] int _lineInterval = 1;

        [Header("Floor Grid")]
        [SerializeField] bool _showFloorGrid = true;
        [SerializeField] Color _floorGridColor = Color.green;

        [Header("Wall Grid")]
        [SerializeField] bool _showWallGrid = true;
        [SerializeField] int _wallHeight = 10;
        [SerializeField] Color _leftWallColor = Color.cyan;
        [SerializeField] Color _rightWallColor = Color.magenta;

        [Inject]
        void Init(IsoGridSettingsView isoGridSettingsView)
        {
            _isoGridSettingsView = isoGridSettingsView;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_isoGridSettingsView == null) return;

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

            var angleRad = _isoGridSettingsView.Angle * Mathf.Deg2Rad;
            var cellSize = _isoGridSettingsView.CellSize;
            var xAxis = new Vector3(Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;
            var yAxis = new Vector3(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;

            var origin = _isoGridSettingsView.Origin;
            var gridWidth = _isoGridSettingsView.GridWidth;
            var gridHeight = _isoGridSettingsView.GridHeight;

            // 縦線（右下方向）
            for (var x = 0; x <= gridWidth; x += _lineInterval)
            {
                var start = origin + xAxis * x;
                var end = start + yAxis * gridHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（左下方向）
            for (var y = 0; y <= gridHeight; y += _lineInterval)
            {
                var start = origin + yAxis * y;
                var end = start + xAxis * gridWidth;
                Gizmos.DrawLine(start, end);
            }
        }

        void DrawWallGrid()
        {
            var angleRad = _isoGridSettingsView.Angle * Mathf.Deg2Rad;
            var cellSize = _isoGridSettingsView.CellSize;
            var xAxis = new Vector3(Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;
            var yAxis = new Vector3(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;
            var zAxis = Vector3.up * cellSize;

            var origin = _isoGridSettingsView.Origin;
            var gridWidth = _isoGridSettingsView.GridWidth;
            var gridHeight = _isoGridSettingsView.GridHeight;

            // 左壁（yAxis方向に沿った壁、上に伸びる）
            Gizmos.color = _leftWallColor;
            var leftWallOrigin = origin;

            // 縦線（上方向）
            for (var y = 0; y <= gridHeight; y += _lineInterval)
            {
                var start = leftWallOrigin + yAxis * y;
                var end = start + zAxis * _wallHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（左下方向）
            for (var z = 0; z <= _wallHeight; z += _lineInterval)
            {
                var start = leftWallOrigin + zAxis * z;
                var end = start + yAxis * gridHeight;
                Gizmos.DrawLine(start, end);
            }

            // 右壁（xAxis方向に沿った壁、上に伸びる）
            Gizmos.color = _rightWallColor;
            var rightWallOrigin = origin;

            // 縦線（上方向）
            for (var x = 0; x <= gridWidth; x += _lineInterval)
            {
                var start = rightWallOrigin + xAxis * x;
                var end = start + zAxis * _wallHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（右下方向）
            for (var z = 0; z <= _wallHeight; z += _lineInterval)
            {
                var start = rightWallOrigin + zAxis * z;
                var end = start + xAxis * gridWidth;
                Gizmos.DrawLine(start, end);
            }
        }
    }
#endif
}
