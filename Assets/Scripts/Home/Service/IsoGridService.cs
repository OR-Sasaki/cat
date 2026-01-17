using System;
using Cysharp.Threading.Tasks;
using Home.State;
using Home.View;
using UnityEngine;

namespace Home.Service
{
    /// IsoGridのセル操作と座標変換を行うService
    public class IsoGridService
    {
        readonly IsoGridState _state;
        readonly IsoGridSettingsView _isoGridSettingsView;

        // グリッド設定
        readonly Vector3 _origin;
        readonly float _cellSize;
        readonly float _angle;

        // 座標変換用キャッシュ
        readonly Vector2 _xAxis;
        readonly Vector2 _yAxis;
        readonly float _determinant;

        // NavMesh再構築用イベント
        public event Action OnObjectPlaced;

        public IsoGridService(IsoGridState state, IsoGridSettingsView isoGridSettingsView)
        {
            _state = state;
            _isoGridSettingsView = isoGridSettingsView;

            // State初期化
            _state.Initialize(_isoGridSettingsView.GridWidth, _isoGridSettingsView.GridHeight);

            // グリッド設定を初期化
            _origin = _isoGridSettingsView.Origin;
            _cellSize = _isoGridSettingsView.CellSize;
            _angle = _isoGridSettingsView.Angle;

            // 座標キャッシュ更新
            var angleRad = _angle * Mathf.Deg2Rad;
            _xAxis = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _yAxis = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * _cellSize;
            _determinant = _xAxis.x * _yAxis.y - _xAxis.y * _yAxis.x;

            if (Mathf.Approximately(_determinant, 0f))
                Debug.LogError($"[IsoGridService] Determinant is zero, cannot convert coordinates.");

            // オブジェクトが置かれた時にNavMeshを再ビルドする
            // BuildNavMeshは、シーン上に配置されたColliderによってビルドされるが、
            // オブジェクトが動いた時に、それが反映されるまでに少しラグがあるため10フレーム待っている
            OnObjectPlaced += async () =>
            {
                await UniTask.DelayFrame(10);
                _isoGridSettingsView.Surface2D.BuildNavMesh();
            };
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
