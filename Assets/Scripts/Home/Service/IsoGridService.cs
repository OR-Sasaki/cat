using Home.State;
using UnityEngine;

namespace Home.Service
{
    /// <summary>
    /// IsoGridのセル操作を行うService
    /// </summary>
    public class IsoGridService
    {
        readonly IsoGridState _state;

        public IsoGridService(IsoGridState state)
        {
            _state = state;
        }

        /// <summary>
        /// セル配列を初期化
        /// </summary>
        public void InitializeCells(int gridWidth, int gridHeight)
        {
            _state.Initialize(gridWidth, gridHeight);
        }

        /// <summary>
        /// グリッド座標が有効範囲内かチェック
        /// </summary>
        public bool IsValidPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _state.GridWidth
                && gridPos.y >= 0 && gridPos.y < _state.GridHeight;
        }

        /// <summary>
        /// 指定セルのObjectIdを取得
        /// </summary>
        public int GetObjectId(Vector2Int gridPos)
        {
            if (!IsValidPosition(gridPos)) return 0;
            return _state.FloorCells[gridPos.x, gridPos.y].ObjectId;
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
                    if (!IsValidPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].ObjectId = objectId;
                }
            }
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
                    if (!IsValidPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].Clear();
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
                    if (!IsValidPosition(cellPos)) return false;

                    var cell = _state.FloorCells[cellPos.x, cellPos.y];
                    if (cell.IsOccupied && cell.ObjectId != selfObjectId) return false;
                }
            }
            return true;
        }
    }
}