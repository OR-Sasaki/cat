using System;
using Home.State;
using UnityEngine;

namespace Home.Service
{
    /// IsoGridのセル操作と座標変換を行うService
    public class IsoGridService
    {
        readonly IsoGridState _state;

        // グリッド設定
        Vector3 _origin;
        float _cellSize;
        float _angle;

        // 座標変換用キャッシュ
        Vector2 _xAxis;
        Vector2 _yAxis;
        float _determinant;

        // NavMesh再構築用イベント
        public event Action OnObjectPlaced;

        public int GridWidth => _state.GridWidth;
        public int GridHeight => _state.GridHeight;
        public float CellSize => _cellSize;
        public float Angle => _angle;
        public Vector3 Origin => _origin;

        public IsoGridService(IsoGridState state)
        {
            _state = state;
        }

        /// セル配列を初期化
        public void InitializeCells(int gridWidth, int gridHeight)
        {
            _state.Initialize(gridWidth, gridHeight);
        }

        /// グリッド設定を初期化
        public void InitializeGridSettings(Vector3 origin, float cellSize, float angle)
        {
            _origin = origin;
            _cellSize = cellSize;
            _angle = angle;
            UpdateAxisVectors();
        }

        /// グリッドの軸ベクトルと行列式を更新
        void UpdateAxisVectors()
        {
            var angleRad = _angle * Mathf.Deg2Rad;
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _determinant = _xAxis.x * _yAxis.y - _xAxis.y * _yAxis.x;

            if (Mathf.Approximately(_determinant, 0f))
                Debug.LogError($"[IsoGridService] Determinant is zero, cannot convert coordinates.");
        }

        /// グリッド座標をワールド座標に変換
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            var worldOffset = gridPos.x * _xAxis + gridPos.y * _yAxis;
            return _origin + (Vector3)worldOffset;
        }

        /// ワールド座標を床グリッド座標に変換
        public Vector2Int WorldToFloorGrid(Vector3 worldPos)
        {
            var offset = (Vector2)(worldPos - _origin);
            var gridX = (offset.x * _yAxis.y - offset.y * _yAxis.x) / _determinant;
            var gridY = (_xAxis.x * offset.y - _xAxis.y * offset.x) / _determinant;

            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        /// グリッド座標が有効範囲内かチェック
        public bool IsValidPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _state.GridWidth
                && gridPos.y >= 0 && gridPos.y < _state.GridHeight;
        }

        /// 指定セルのObjectIdを取得
        public int GetObjectId(Vector2Int gridPos)
        {
            if (!IsValidPosition(gridPos)) return 0;
            return _state.FloorCells[gridPos.x, gridPos.y].ObjectId;
        }

        /// 指定範囲のセルにオブジェクトを配置
        public void PlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int objectId)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].ObjectId = objectId;
                }
            }

            OnObjectPlaced?.Invoke();
        }

        /// 指定範囲のセルからオブジェクトを削除
        public void RemoveObject(Vector2Int footprintStart, Vector2Int footprintSize)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].Clear();
                }
            }
        }

        /// 指定範囲が配置可能かチェック（自分自身のIDは無視）
        public bool CanPlaceObject(Vector2Int footprintStart, Vector2Int footprintSize, int selfObjectId = 0)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidPosition(cellPos)) return false;

                    var cell = _state.FloorCells[cellPos.x, cellPos.y];
                    if (cell.IsOccupied && cell.ObjectId != selfObjectId) return false;
                }
            }
            return true;
        }
    }
}