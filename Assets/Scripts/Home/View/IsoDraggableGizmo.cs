using Home.Service;
using UnityEngine;
using VContainer;

namespace Home.View
{
    // IsoDraggableViewのドラッグ中フットプリントをGizmoで表示するコンポーネント
    // Editor内でのみ動作する
    public class IsoDraggableGizmo : MonoBehaviour
    {
        IsoDraggableView _isoDraggableView;
        IsoGridSettingsView _isoGridSettingsView;
        IsoGridService _isoGridService;

        public void Init(
            IsoGridSettingsView isoGridSettingsView,
            IsoGridService isoGridService)
        {
            _isoGridSettingsView = isoGridSettingsView;
            _isoGridService = isoGridService;
        }

        public void SetIsoDraggableView(IsoDraggableView isoDraggableView)
        {
            _isoDraggableView = isoDraggableView;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_isoDraggableView == null || !_isoDraggableView.IsDragging) return;

            // GridSettingsをキャッシュ
            if (_isoGridSettingsView == null)
            {
                _isoGridSettingsView = Object.FindFirstObjectByType<IsoGridSettingsView>();
            }
            if (_isoGridSettingsView == null) return;

            var footprintSize = _isoDraggableView.FootprintSize;
            var pivotGridPosition = _isoDraggableView.PivotGridPosition;
            var userFurnitureId = _isoDraggableView.UserFurnitureId;

            // 軸ベクトルを計算
            var angleRad = _isoGridSettingsView.Angle * Mathf.Deg2Rad;
            var cellSize = _isoGridSettingsView.CellSize;
            var xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var origin = _isoGridSettingsView.Origin;

            // スナップ先のグリッド座標を取得（pivotの位置）
            var pivotGridPos = WorldToFloorGrid(transform.position, origin, xAxis, yAxis);

            // footprintの開始位置（左下）を計算
            var footprintStartPos = pivotGridPos - pivotGridPosition;

            // 配置可能かチェック
            var canPlace = _isoGridService.CanPlaceObject(footprintStartPos, footprintSize, userFurnitureId);

            // 配置可能なら緑、不可能なら赤
            var color = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            Gizmos.color = color;

            // footprintSize分のセルを描画
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
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
            var outerCorner1 = footprintOrigin + (Vector3)(xAxis * footprintSize.x);
            var outerCorner2 = footprintOrigin + (Vector3)(xAxis * footprintSize.x + yAxis * footprintSize.y);
            var outerCorner3 = footprintOrigin + (Vector3)(yAxis * footprintSize.y);

            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.DrawLine(outerCorner0, outerCorner1);
            Gizmos.DrawLine(outerCorner1, outerCorner2);
            Gizmos.DrawLine(outerCorner2, outerCorner3);
            Gizmos.DrawLine(outerCorner3, outerCorner0);
        }

        Vector2Int WorldToFloorGrid(Vector3 worldPos, Vector3 origin, Vector2 xAxis, Vector2 yAxis)
        {
            var determinant = xAxis.x * yAxis.y - xAxis.y * yAxis.x;
            var offset = (Vector2)(worldPos - origin);
            var gridX = (offset.x * yAxis.y - offset.y * yAxis.x) / determinant;
            var gridY = (xAxis.x * offset.y - xAxis.y * offset.x) / determinant;
            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
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
