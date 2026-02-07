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
        readonly IsoCoordinateConverterService _converter;

        // グリッド設定
        readonly Vector3 _origin;
        readonly float _cellSize;

        // NavMesh再構築用イベント
        public event Action OnObjectPlaced;

        public IsoGridService(IsoGridState state, IsoGridSettingsView isoGridSettingsView)
        {
            _state = state;
            _isoGridSettingsView = isoGridSettingsView;

            // State初期化
            _state.Initialize(
                _isoGridSettingsView.GridWidth,
                _isoGridSettingsView.GridHeight,
                _isoGridSettingsView.WallHeight
            );

            // グリッド設定を初期化
            _origin = _isoGridSettingsView.Origin;
            _cellSize = IsoGridSettingsView.CellSize;

            // 座標変換サービスを初期化
            _converter = new IsoCoordinateConverterService(_cellSize, IsoGridSettingsView.Angle);

            // オブジェクトが置かれた時にNavMeshを再ビルドする
            // BuildNavMeshは、シーン上に配置されたColliderによってビルドされるが、
            // オブジェクトが動いた時に、それが反映されるまでに少しラグがあるため10フレーム待っている
            OnObjectPlaced += async () =>
            {
                await UniTask.DelayFrame(10);
                _isoGridSettingsView.Surface2D.BuildNavMesh();
            };
        }

        #region 床グリッド操作

        /// 床グリッド座標をワールド座標に変換
        public Vector3 FloorGridToWorld(Vector2Int gridPos)
        {
            return _origin + (Vector3)_converter.GridToOffset(gridPos);
        }

        /// ワールド座標を床グリッド座標に変換
        public Vector2Int WorldToFloorGrid(Vector3 worldPos)
        {
            var offset = (Vector2)(worldPos - _origin);
            return _converter.OffsetToGrid(offset);
        }

        /// 床グリッド座標が有効範囲内かチェック
        public bool IsValidFloorPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _state.GridWidth
                && gridPos.y >= 0 && gridPos.y < _state.GridHeight;
        }

        /// 床の指定セルのUserFurnitureIdを取得
        public int GetFloorUserFurnitureId(Vector2Int gridPos)
        {
            if (!IsValidFloorPosition(gridPos)) return 0;
            return _state.FloorCells[gridPos.x, gridPos.y].UserFurnitureId;
        }

        /// 床の指定範囲のセルにオブジェクトを配置
        public void PlaceFloorObject(Vector2Int footprintStart, Vector2Int footprintSize, int userFurnitureId)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].UserFurnitureId = userFurnitureId;
                }
            }

            // オブジェクトのフットプリント開始位置を記録
            _state.ObjectFootprintStartPositions[userFurnitureId] = footprintStart;

            OnObjectPlaced?.Invoke();
        }

        /// 床の指定範囲のセルからオブジェクトを削除
        public void RemoveFloorObject(int userFurnitureId, Vector2Int footprintSize)
        {
            var footprintStart = _state.ObjectFootprintStartPositions[userFurnitureId];

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) continue;

                    _state.FloorCells[cellPos.x, cellPos.y].Clear();
                }
            }

            // オブジェクトのフットプリント開始位置を削除
            _state.ObjectFootprintStartPositions.Remove(userFurnitureId);
        }

        /// 床のUserFurnitureIdからフットプリント開始位置を取得
        public Vector2Int GetFloorObjectFootprintStart(int userFurnitureId)
        {
            return _state.ObjectFootprintStartPositions[userFurnitureId];
        }

        /// 床の指定範囲が配置可能かチェック（自分自身のIDは無視）
        public bool CanPlaceFloorObject(Vector2Int footprintStart, Vector2Int footprintSize, int selfUserFurnitureId = 0)
        {
            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) return false;

                    var cell = _state.FloorCells[cellPos.x, cellPos.y];
                    if (cell.IsOccupied && cell.UserFurnitureId != selfUserFurnitureId) return false;
                }
            }
            return true;
        }

        #endregion

        #region 壁グリッド操作

        /// 壁グリッド座標をワールド座標に変換
        public Vector3 WallGridToWorld(WallSide side, Vector2Int gridPos)
        {
            var zOffset = Vector3.up * gridPos.y * _cellSize;
            if (side == WallSide.Left)
            {
                var worldOffset = gridPos.x * _converter.YAxis;
                return _origin + (Vector3)worldOffset + zOffset;
            }
            else
            {
                var worldOffset = gridPos.x * _converter.XAxis;
                return _origin + (Vector3)worldOffset + zOffset;
            }
        }

        /// ワールド座標を壁グリッド座標に変換(値を丸めない)
        public Vector2 WorldToWallGridNotRound(WallSide side, Vector3 worldPos)
        {
            // 壁面上の2D座標オフセット
            var offset = (Vector2)(worldPos - _origin);
            var wallAxis = side == WallSide.Left ? _converter.YAxis : _converter.XAxis;

            // 壁の座標系での逆変換（2x2行列の逆行列を使用）
            // [wallAxis.x, 0        ] [wallGrid]   [offset.x]
            // [wallAxis.y, _cellSize] [zGrid   ] = [offset.y]
            // determinant = wallAxis.x * _cellSize
            var determinant = wallAxis.x * _cellSize;

            // 壁面方向のグリッド座標
            var wallGrid = offset.x * _cellSize / determinant;

            // 高さ方向のグリッド座標（壁軸のY成分の寄与を除去）
            var zGrid = (wallAxis.x * offset.y - wallAxis.y * offset.x) / determinant;

            return new Vector2(wallGrid, zGrid);
        }

        /// ワールド座標を壁グリッド座標に変換
        public Vector2Int WorldToWallGrid(WallSide side, Vector3 worldPos)
        {
            var gridNotRound = WorldToWallGridNotRound(side, worldPos);
            return new Vector2Int(Mathf.RoundToInt(gridNotRound.x), Mathf.RoundToInt(gridNotRound.y));
        }

        /// 壁グリッド座標が有効範囲内かチェック
        public bool IsValidWallPosition(WallSide side, Vector2Int gridPos)
        {
            var maxWidth = side == WallSide.Left ? _state.GridHeight : _state.GridWidth;
            return gridPos.x >= 0 && gridPos.x < maxWidth
                && gridPos.y >= 0 && gridPos.y < _state.WallHeight;
        }

        /// 壁の指定セルのUserFurnitureIdを取得
        public int GetWallUserFurnitureId(WallSide side, Vector2Int gridPos)
        {
            if (!IsValidWallPosition(side, gridPos)) return 0;
            var cells = side == WallSide.Left ? _state.LeftWallCells : _state.RightWallCells;
            return cells[gridPos.x, gridPos.y].UserFurnitureId;
        }

        /// 壁への配置可能チェック（自分自身のIDは無視）
        public bool CanPlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int selfUserFurnitureId = 0)
        {
            var cells = side == WallSide.Left ? _state.LeftWallCells : _state.RightWallCells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidWallPosition(side, cellPos)) return false;

                    var cell = cells[cellPos.x, cellPos.y];
                    if (cell.IsOccupied && cell.UserFurnitureId != selfUserFurnitureId) return false;
                }
            }
            return true;
        }

        /// 壁にオブジェクトを配置
        public void PlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int userFurnitureId)
        {
            var cells = side == WallSide.Left ? _state.LeftWallCells : _state.RightWallCells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidWallPosition(side, cellPos)) continue;

                    cells[cellPos.x, cellPos.y].UserFurnitureId = userFurnitureId;
                }
            }

            // 壁オブジェクトの位置を記録
            _state.WallObjectFootprintStartPositions[userFurnitureId] = new WallObjectPosition
            {
                Side = side,
                Position = footprintStart
            };

            OnObjectPlaced?.Invoke();
        }

        /// 壁からオブジェクトを削除
        public void RemoveWallObject(int userFurnitureId, Vector2Int footprintSize)
        {
            if (!_state.WallObjectFootprintStartPositions.TryGetValue(userFurnitureId, out var wallPos))
            {
                Debug.LogWarning($"[IsoGridService] WallObject {userFurnitureId} not found");
                return;
            }

            var cells = wallPos.Side == WallSide.Left ? _state.LeftWallCells : _state.RightWallCells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(wallPos.Position.x + x, wallPos.Position.y + y);
                    if (!IsValidWallPosition(wallPos.Side, cellPos)) continue;

                    cells[cellPos.x, cellPos.y].Clear();
                }
            }

            _state.WallObjectFootprintStartPositions.Remove(userFurnitureId);
        }

        /// 壁のUserFurnitureIdからフットプリント開始位置を取得
        public WallObjectPosition GetWallObjectFootprintStart(int userFurnitureId)
        {
            return _state.WallObjectFootprintStartPositions[userFurnitureId];
        }

        #endregion

        #region FragmentedIsoGrid操作

        /// FragmentedIsoGrid上にオブジェクトを配置したことをStateに記録
        public void PlaceFragmentedObject(int parentUserFurnitureId, int userFurnitureId, Vector2Int localGridPos, int depth)
        {
            if (!_state.FragmentedGridObjectPositions.TryGetValue(parentUserFurnitureId, out var childPositions))
            {
                childPositions = new System.Collections.Generic.Dictionary<int, FragmentedObjectData>();
                _state.FragmentedGridObjectPositions[parentUserFurnitureId] = childPositions;
            }

            childPositions[userFurnitureId] = new FragmentedObjectData
            {
                Position = localGridPos,
                Depth = depth
            };
        }

        /// FragmentedIsoGrid上からオブジェクトを削除したことをStateに記録
        public void RemoveFragmentedObject(int parentUserFurnitureId, int userFurnitureId)
        {
            if (!_state.FragmentedGridObjectPositions.TryGetValue(parentUserFurnitureId, out var childPositions))
            {
                return;
            }

            childPositions.Remove(userFurnitureId);

            // 子が空になったら親のエントリも削除
            if (childPositions.Count == 0)
            {
                _state.FragmentedGridObjectPositions.Remove(parentUserFurnitureId);
            }
        }

        /// FragmentedIsoGrid用のStateを取得
        public System.Collections.Generic.IReadOnlyDictionary<int, System.Collections.Generic.Dictionary<int, FragmentedObjectData>> GetFragmentedGridObjectPositions()
        {
            return _state.FragmentedGridObjectPositions;
        }

        #endregion
    }
}
