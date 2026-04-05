using UnityEngine;

namespace Home.View
{
    /// FragmentedIsoGridをGizmo上で表示する
    /// Editor内のみで動作する
    public class FragmentedIsoGridGizmo : MonoBehaviour
    {
        [SerializeField] FragmentedIsoGrid _fragmentedIsoGrid;
        [SerializeField] float _angle;

        [Header("Gizmo Settings")]
        [SerializeField] Color _gridColor = Color.yellow;
        [SerializeField] bool _showGrid = true;

        public void Initialize(FragmentedIsoGrid fragmentedIsoGrid)
        {
            _fragmentedIsoGrid = fragmentedIsoGrid;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_fragmentedIsoGrid == null) return;
            if (!_showGrid) return;

            DrawGrid();
        }

        void DrawGrid()
        {
            Gizmos.color = _gridColor;

            var angleRad = _angle * Mathf.Deg2Rad;
            var cellSize = _fragmentedIsoGrid.CellSize;
            var xAxis = new Vector3(Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;
            var yAxis = new Vector3(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad), 0f) * cellSize;

            var origin = _fragmentedIsoGrid.transform.position;
            var gridWidth = _fragmentedIsoGrid.Size.x;
            var gridHeight = _fragmentedIsoGrid.Size.y;

            // 縦線（右下方向）
            for (var x = 0; x <= gridWidth; x++)
            {
                var start = origin + xAxis * x;
                var end = start + yAxis * gridHeight;
                Gizmos.DrawLine(start, end);
            }

            // 横線（左下方向）
            for (var y = 0; y <= gridHeight; y++)
            {
                var start = origin + yAxis * y;
                var end = start + xAxis * gridWidth;
                Gizmos.DrawLine(start, end);
            }
        }
#endif
    }
}
