using UnityEngine;

namespace Home.View
{
    /// <summary>
    /// IsoDraggableViewのドラッグ中フットプリントをGizmoで表示するコンポーネント
    /// </summary>
    public class IsoDraggableGizmo : MonoBehaviour
    {
        IsoDraggableView _draggable;

        void Awake()
        {
            _draggable = GetComponent<IsoDraggableView>();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_draggable == null || !_draggable.IsDragging || _draggable.GridSystem == null) return;

            var gridSystem = _draggable.GridSystem;
            var footprintSize = _draggable.FootprintSize;
            var pivotGridPosition = _draggable.PivotGridPosition;
            var objectId = _draggable.ObjectId;

            // スナップ先のグリッド座標を取得（pivotの位置）
            var pivotGridPos = gridSystem.WorldToFloorGrid(transform.position);

            // footprintの開始位置（左下）を計算
            var footprintStartPos = pivotGridPos - pivotGridPosition;

            // 軸ベクトルを計算
            var angleRad = gridSystem.Angle * Mathf.Deg2Rad;
            var cellSize = gridSystem.CellSize;
            var xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * cellSize;
            var origin = gridSystem.Origin;

            // 配置可能かチェック
            var canPlace = gridSystem.CanPlaceObject(footprintStartPos, footprintSize, objectId);

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
