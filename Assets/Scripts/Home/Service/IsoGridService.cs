using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Home.State;
using Home.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Home.Service
{
    /// IsoGridのセル操作と座標変換を行うService
    /// ここではあくまでstateベースでIsoGridを管理している
    /// シーン上に配置されているオブジェクトの管理については、IsoDragServiceやFurniturePlacementServiceなどで行なっている
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

        /// Homeシーンへの参照（Instantiate先の指定に使用）
        public Scene HomeScene => _isoGridSettingsView.gameObject.scene;

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
            return gridPos.x >= 0 && gridPos.x < _state.Floor.Size.x
                && gridPos.y >= 0 && gridPos.y < _state.Floor.Size.y;
        }

        /// 床の指定セルのUserFurnitureIdを取得
        public int GetFloorUserFurnitureId(Vector2Int gridPos)
        {
            if (!IsValidFloorPosition(gridPos)) return 0;
            return _state.Floor.Cells[gridPos.x, gridPos.y];
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

                    _state.Floor.Cells[cellPos.x, cellPos.y] = userFurnitureId;
                }
            }

            // オブジェクトのフットプリント開始位置を記録
            _state.Floor.ObjectPositions[userFurnitureId] = new ObjectPlacement
            {
                Position = footprintStart,
                Depth = 0,
            };

            // 自分の上に積まれている子孫オブジェクトのDepthを追従させる
            UpdateDescendantDepths(userFurnitureId, 0);

            OnObjectPlaced?.Invoke();
        }

        /// 床の指定範囲のセルからオブジェクトを削除
        public void RemoveFloorObject(int userFurnitureId, Vector2Int footprintSize)
        {
            var footprintStart = _state.Floor.ObjectPositions[userFurnitureId].Position;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidFloorPosition(cellPos)) continue;

                    _state.Floor.Cells[cellPos.x, cellPos.y] = 0;
                }
            }

            // オブジェクトのフットプリント開始位置を削除
            _state.Floor.ObjectPositions.Remove(userFurnitureId);
        }

        /// 床のUserFurnitureIdからフットプリント開始位置を取得
        public Vector2Int GetFloorObjectFootprintStart(int userFurnitureId)
        {
            return _state.Floor.ObjectPositions[userFurnitureId].Position;
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

                    var cellValue = _state.Floor.Cells[cellPos.x, cellPos.y];
                    if (cellValue != 0 && cellValue != selfUserFurnitureId) return false;
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
            var wallEntry = side == WallSide.Left ? _state.LeftWall : _state.RightWall;
            return gridPos.x >= 0 && gridPos.x < wallEntry.Size.x
                && gridPos.y >= 0 && gridPos.y < wallEntry.Size.y;
        }

        /// 壁の指定セルのUserFurnitureIdを取得
        public int GetWallUserFurnitureId(WallSide side, Vector2Int gridPos)
        {
            if (!IsValidWallPosition(side, gridPos)) return 0;
            var cells = (side == WallSide.Left ? _state.LeftWall : _state.RightWall).Cells;
            return cells[gridPos.x, gridPos.y];
        }

        /// 壁への配置可能チェック（自分自身のIDは無視）
        public bool CanPlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int selfUserFurnitureId = 0)
        {
            var cells = (side == WallSide.Left ? _state.LeftWall : _state.RightWall).Cells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidWallPosition(side, cellPos)) return false;

                    var cellValue = cells[cellPos.x, cellPos.y];
                    if (cellValue != 0 && cellValue != selfUserFurnitureId) return false;
                }
            }
            return true;
        }

        /// 壁にオブジェクトを配置
        public void PlaceWallObject(WallSide side, Vector2Int footprintStart, Vector2Int footprintSize, int userFurnitureId)
        {
            var cells = (side == WallSide.Left ? _state.LeftWall : _state.RightWall).Cells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(footprintStart.x + x, footprintStart.y + y);
                    if (!IsValidWallPosition(side, cellPos)) continue;

                    cells[cellPos.x, cellPos.y] = userFurnitureId;
                }
            }

            // 壁オブジェクトの位置を記録
            var wallEntry = side == WallSide.Left ? _state.LeftWall : _state.RightWall;
            wallEntry.ObjectPositions[userFurnitureId] = new ObjectPlacement
            {
                Position = footprintStart,
                Depth = 0,
            };

            OnObjectPlaced?.Invoke();
        }

        /// 壁からオブジェクトを削除
        public void RemoveWallObject(int userFurnitureId, Vector2Int footprintSize)
        {
            WallSide side;
            Vector2Int position;
            if (_state.LeftWall.ObjectPositions.TryGetValue(userFurnitureId, out var leftPlacement))
            {
                side = WallSide.Left;
                position = leftPlacement.Position;
            }
            else if (_state.RightWall.ObjectPositions.TryGetValue(userFurnitureId, out var rightPlacement))
            {
                side = WallSide.Right;
                position = rightPlacement.Position;
            }
            else
            {
                Debug.LogWarning($"[IsoGridService] WallObject {userFurnitureId} not found");
                return;
            }

            var wallEntry = side == WallSide.Left ? _state.LeftWall : _state.RightWall;
            var cells = wallEntry.Cells;

            for (var x = 0; x < footprintSize.x; x++)
            {
                for (var y = 0; y < footprintSize.y; y++)
                {
                    var cellPos = new Vector2Int(position.x + x, position.y + y);
                    if (!IsValidWallPosition(side, cellPos)) continue;

                    cells[cellPos.x, cellPos.y] = 0;
                }
            }

            wallEntry.ObjectPositions.Remove(userFurnitureId);
        }

        /// 壁のUserFurnitureIdからフットプリント開始位置を取得
        public (WallSide Side, Vector2Int Position) GetWallObjectFootprintStart(int userFurnitureId)
        {
            if (_state.LeftWall.ObjectPositions.TryGetValue(userFurnitureId, out var left))
            {
                return (WallSide.Left, left.Position);
            }
            if (_state.RightWall.ObjectPositions.TryGetValue(userFurnitureId, out var right))
            {
                return (WallSide.Right, right.Position);
            }
            throw new KeyNotFoundException($"WallObject {userFurnitureId} not found");
        }

        #endregion

        #region FragmentedIsoGrid操作

        /// FragmentedIsoGridのGridEntry を取得（なければ生成）
        GridEntry GetOrCreateFragmentedGridEntry(FragmentedIsoGrid grid)
        {
            var parentId = grid.GetParentUserFurnitureId();
            if (!_state.FragmentedGrids.TryGetValue(parentId, out var entry))
            {
                entry = new GridEntry(grid.Size);
                _state.FragmentedGrids[parentId] = entry;
            }
            return entry;
        }

        /// FragmentedIsoGrid上の指定位置に家具が配置可能かチェック（自分自身のIDは無視）
        public bool CanPlaceFragmentedObject(FragmentedIsoGrid grid, Vector2Int localGridPos, Vector2Int footprint, int selfUserFurnitureId = 0)
        {
            var entry = GetOrCreateFragmentedGridEntry(grid);

            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!grid.IsValidLocalPosition(cellPos)) return false;

                    var cellValue = entry.Cells[cellPos.x, cellPos.y];
                    if (cellValue != 0 && cellValue != selfUserFurnitureId) return false;
                }
            }

            return true;
        }

        /// FragmentedIsoGrid上にオブジェクトを配置
        public void PlaceFragmentedObject(FragmentedIsoGrid grid, Vector2Int localGridPos, Vector2Int footprint, int userFurnitureId)
        {
            var entry = GetOrCreateFragmentedGridEntry(grid);

            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!grid.IsValidLocalPosition(cellPos)) continue;

                    entry.Cells[cellPos.x, cellPos.y] = userFurnitureId;
                }
            }

            var depth = CalculateFragmentedGridDepth(grid);
            entry.ObjectPositions[userFurnitureId] = new ObjectPlacement
            {
                Position = localGridPos,
                Depth = depth,
            };

            // 自分の上に積まれている子孫オブジェクトのDepthを追従させる
            UpdateDescendantDepths(userFurnitureId, depth);
        }

        /// 指定オブジェクトを根とするサブツリー（そのFragmentedGrid上の家具とその子孫）のDepthを再帰的に更新する
        /// 全オブジェクトを再計算せず、影響範囲（子孫のみ）に限定する
        void UpdateDescendantDepths(int parentUserFurnitureId, int parentDepth)
        {
            if (!_state.FragmentedGrids.TryGetValue(parentUserFurnitureId, out var entry)) return;

            var newDepth = parentDepth + 1;
            // Dictionary の値を書き換えるため、キーのスナップショットを取ってから走査する
            var childIds = new List<int>(entry.ObjectPositions.Keys);
            foreach (var childId in childIds)
            {
                var placement = entry.ObjectPositions[childId];
                placement.Depth = newDepth;
                entry.ObjectPositions[childId] = placement;

                UpdateDescendantDepths(childId, newDepth);
            }
        }

        /// FragmentedIsoGrid上からオブジェクトを削除
        public void RemoveFragmentedObject(FragmentedIsoGrid grid, int userFurnitureId, Vector2Int footprint)
        {
            var parentId = grid.GetParentUserFurnitureId();
            if (!_state.FragmentedGrids.TryGetValue(parentId, out var entry))
            {
                Debug.LogError($"[IsoGridService] FragmentedGrid {parentId} not found");
                return;
            }

            if (!entry.ObjectPositions.TryGetValue(userFurnitureId, out var placement))
            {
                Debug.LogWarning($"[IsoGridService] FragmentedObject {userFurnitureId} not found on parent {parentId}");
                return;
            }

            var localGridPos = placement.Position;
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cellPos = new Vector2Int(localGridPos.x + x, localGridPos.y + y);
                    if (!grid.IsValidLocalPosition(cellPos)) continue;

                    entry.Cells[cellPos.x, cellPos.y] = 0;
                }
            }

            entry.ObjectPositions.Remove(userFurnitureId);
        }

        /// FragmentedIsoGrid上のオブジェクトのフットプリント開始位置を取得
        public Vector2Int GetFragmentedObjectFootprintStart(FragmentedIsoGrid grid, int userFurnitureId)
        {
            var parentId = grid.GetParentUserFurnitureId();
            return _state.FragmentedGrids[parentId].ObjectPositions[userFurnitureId].Position;
        }

        /// FragmentedIsoGridのStateエントリを破棄（親家具削除時に呼ぶ）
        public void UnregisterFragmentedGrid(FragmentedIsoGrid grid)
        {
            var parentId = grid.GetParentUserFurnitureId();
            _state.FragmentedGrids.Remove(parentId);
        }

        /// FragmentedIsoGridの入れ子の深さを計算
        /// 床に直接配置された家具上のFragmentedIsoGrid = 1
        /// その上に配置された家具上のFragmentedIsoGrid = 2 ...
        public int CalculateFragmentedGridDepth(FragmentedIsoGrid grid)
        {
            var depth = 1;
            var currentGrid = grid;

            while (currentGrid is not null)
            {
                var parentView = currentGrid.IsoDraggableView;
                if (parentView is null) break;

                var parentGrid = parentView.CurrentFragmentedGrid;
                if (parentGrid is null) break;

                depth++;
                currentGrid = parentGrid;
            }

            return depth;
        }

        #endregion
    }
}
