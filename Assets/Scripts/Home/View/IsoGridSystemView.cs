using System.Collections;
using Home.Service;
using NavMeshPlus.Components;
using UnityEngine;
using VContainer;

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

        Vector2 _xAxis;
        Vector2 _yAxis;
        float _determinant;

        IsoGridService _isoGridService;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public float Angle => _angle;
        public Vector3 Origin => transform.position;

        [Inject]
        public void Init(IsoGridService service)
        {
            _isoGridService = service;
            _isoGridService.InitializeCells(_gridWidth, _gridHeight);
        }

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
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _determinant = _xAxis.x * _yAxis.y - _xAxis.y * _yAxis.x;

            if (Mathf.Approximately(_determinant, 0f))
                Debug.LogError($"[IsoGridSystem] Determinant is zero, cannot convert coordinates.");
        }

        /// グリッド座標をワールド座標に変換
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            var worldOffset = gridPos.x * _xAxis + gridPos.y * _yAxis;
            return transform.position + (Vector3)worldOffset;
        }

        /// ワールド座標を床グリッド座標に変換
        public Vector2Int WorldToFloorGrid(Vector3 worldPos)
        {
            var offset = (Vector2)(worldPos - transform.position);
            var gridX = (offset.x * _yAxis.y - offset.y * _yAxis.x) / _determinant;
            var gridY = (_xAxis.x * offset.y - _xAxis.y * offset.x) / _determinant;

            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        /// <summary>
        /// 指定範囲のセルにオブジェクトを配置
        /// </summary>
        public void PlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int objectId)
        {
            // 家具配置
            _isoGridService.PlaceObject(footprintStart, footprintSize, objectId);

            // 配置後にNavMesh再構築
            StartCoroutine(BuildNavMeshDelayed());
        }

        IEnumerator BuildNavMeshDelayed()
        {
            // 2フレーム待つ
            yield return null;
            _surface2D.BuildNavMeshAsync();
        }

        /// <summary>
        /// 指定範囲のセルからオブジェクトを削除
        /// </summary>
        public void RemoveObject(Vector2Int footprintStart, Vector2Int footprintSize)
        {
            _isoGridService.RemoveObject(footprintStart, footprintSize);
        }

        /// <summary>
        /// 指定範囲が配置可能かチェック（自分自身のIDは無視）
        /// </summary>
        public bool CanPlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int selfObjectId = 0)
        {
            return _isoGridService.CanPlaceObject(footprintStart, footprintSize, selfObjectId);
        }
    }
}
