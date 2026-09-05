#nullable enable
using System;
using System.Collections.Generic;
using Home.GridPreviewLogic;
using Home.State;
using Home.View;
using UnityEngine;
using VContainer;

namespace Home.Service
{
    /// DropTarget を線・予告面の描画命令に変換する。State/Grid は読み取りのみ
    public sealed class GridPreviewService
    {
        readonly IsoGridService _isoGridService;
        readonly IsoGridState _isoGridState;
        readonly IsoGridSettingsView _isoGridSettingsView;

        GridPreviewView? _view;
        bool _viewWarningLogged;

        IsoDraggableView? _draggable;
        bool _floorLinesBuilt;
        bool _wallLinesBuilt;

        // Evaluate に渡す判定関数（フレーム毎の確保を避けるため使い回す）
        readonly Func<GridCell, bool> _floorInRange;
        readonly Func<GridCell, int> _floorOccupant;
        readonly Func<GridCell, bool> _wallInRange;
        readonly Func<GridCell, int> _wallOccupant;
        readonly Func<GridCell, bool> _fragmentedInRange;
        readonly Func<GridCell, int> _fragmentedOccupant;

        WallSide _moveSide;
        FragmentedIsoGrid? _moveGrid;

        readonly List<Vector3> _footprintCorners = new();

        [Inject]
        public GridPreviewService(IsoGridService isoGridService, IsoGridState isoGridState, IsoGridSettingsView isoGridSettingsView)
        {
            _isoGridService = isoGridService;
            _isoGridState = isoGridState;
            _isoGridSettingsView = isoGridSettingsView;

            _floorInRange = cell => _isoGridService.IsValidFloorPosition(new Vector2Int(cell.X, cell.Y));
            _floorOccupant = cell => _isoGridService.GetFloorUserFurnitureId(new Vector2Int(cell.X, cell.Y));
            _wallInRange = cell => _isoGridService.IsValidWallPosition(_moveSide, new Vector2Int(cell.X, cell.Y));
            _wallOccupant = cell => _isoGridService.GetWallUserFurnitureId(_moveSide, new Vector2Int(cell.X, cell.Y));
            _fragmentedInRange = cell => _moveGrid!.IsValidLocalPosition(new Vector2Int(cell.X, cell.Y));
            _fragmentedOccupant = cell =>
            {
                var parentId = _moveGrid!.GetParentUserFurnitureId();
                return _isoGridState.FragmentedGrids.TryGetValue(parentId, out var entry) ? entry.Cells[cell.X, cell.Y] : 0;
            };
        }

        public void AttachView(GridPreviewView view)
        {
            _view = view;
        }

        public void OnFurnitureDragBegin(IsoDraggableView draggable)
        {
            if (!HasView()) return;

            _draggable = draggable;

            if (draggable.IsWallPlacement)
            {
                EnsureWallLinesBuilt();
                _view!.ShowLines(GridSurface.Walls);
            }
            else
            {
                EnsureFloorLinesBuilt();
                _view!.ShowLines(GridSurface.Floor);
            }
        }

        public void OnFurnitureDragMove(in DropTarget target)
        {
            if (!HasView()) return;
            if (_draggable == null) return;

            var start = new GridCell(target.FootprintStart.x, target.FootprintStart.y);
            var size = new GridCell(_draggable.FootprintSize.x, _draggable.FootprintSize.y);
            var selfId = _draggable.UserFurnitureId;

            Func<GridCell, bool> isInRange;
            Func<GridCell, int> occupantIdOf;
            Transform? parent;

            switch (target.Surface)
            {
                case DropSurface.Floor:
                    isInRange = _floorInRange;
                    occupantIdOf = _floorOccupant;
                    parent = null;
                    break;

                case DropSurface.Wall:
                    _moveSide = target.Side;
                    isInRange = _wallInRange;
                    occupantIdOf = _wallOccupant;
                    parent = null;
                    break;

                case DropSurface.Fragmented:
                    if (target.Grid == null)
                    {
                        Debug.LogError("[GridPreviewService] Fragmented target without grid");
                        _view!.ClearFootprint();
                        return;
                    }
                    _moveGrid = target.Grid;
                    isInRange = _fragmentedInRange;
                    occupantIdOf = _fragmentedOccupant;
                    parent = target.Grid.transform;
                    break;

                default:
                    return;
            }

            var evaluation = FootprintEvaluator.Evaluate(start, size, selfId, isInRange, occupantIdOf);

            if (evaluation.VisibleCells.Count == 0)
            {
                _view!.ClearFootprint();
                return;
            }

            _footprintCorners.Clear();
            foreach (var cell in evaluation.VisibleCells)
            {
                _footprintCorners.Add(CellToWorld(target, new Vector2Int(cell.X, cell.Y)));
                _footprintCorners.Add(CellToWorld(target, new Vector2Int(cell.X + 1, cell.Y)));
                _footprintCorners.Add(CellToWorld(target, new Vector2Int(cell.X + 1, cell.Y + 1)));
                _footprintCorners.Add(CellToWorld(target, new Vector2Int(cell.X, cell.Y + 1)));
            }

            _view!.SetFootprint(_footprintCorners, evaluation.CanPlace, parent);
        }

        public void OnFurnitureDragEnd()
        {
            if (!HasView()) return;

            _view!.Hide();
            _draggable = null;
        }

        /// 落下先の面に応じたグリッド座標→ワールド座標変換
        Vector3 CellToWorld(in DropTarget target, Vector2Int cell)
        {
            switch (target.Surface)
            {
                case DropSurface.Floor: return _isoGridService.FloorGridToWorld(cell);
                case DropSurface.Wall: return _isoGridService.WallGridToWorld(target.Side, cell);
                case DropSurface.Fragmented: return target.Grid!.LocalGridToWorld(cell);
                default: throw new ArgumentOutOfRangeException(nameof(target.Surface));
            }
        }

        bool HasView()
        {
            if (_view != null) return true;

            if (!_viewWarningLogged)
            {
                Debug.LogWarning("[GridPreviewService] GridPreviewView is not attached");
                _viewWarningLogged = true;
            }
            return false;
        }

        void EnsureFloorLinesBuilt()
        {
            if (_floorLinesBuilt) return;
            _floorLinesBuilt = true;

            var width = _isoGridSettingsView.GridWidth;
            var height = _isoGridSettingsView.GridHeight;
            var segments = new List<(Vector3 Start, Vector3 End)>();

            for (var x = 0; x <= width; x++)
            {
                segments.Add((_isoGridService.FloorGridToWorld(new Vector2Int(x, 0)), _isoGridService.FloorGridToWorld(new Vector2Int(x, height))));
            }
            for (var y = 0; y <= height; y++)
            {
                segments.Add((_isoGridService.FloorGridToWorld(new Vector2Int(0, y)), _isoGridService.FloorGridToWorld(new Vector2Int(width, y))));
            }

            _view!.SetLines(GridSurface.Floor, segments);
        }

        void EnsureWallLinesBuilt()
        {
            if (_wallLinesBuilt) return;
            _wallLinesBuilt = true;

            var gridWidth = _isoGridSettingsView.GridWidth;
            var gridHeight = _isoGridSettingsView.GridHeight;
            var wallHeight = _isoGridSettingsView.WallHeight;
            var segments = new List<(Vector3 Start, Vector3 End)>();

            // 左壁: x 方向の広がりは GridHeight
            for (var i = 0; i <= gridHeight; i++)
            {
                segments.Add((_isoGridService.WallGridToWorld(WallSide.Left, new Vector2Int(i, 0)), _isoGridService.WallGridToWorld(WallSide.Left, new Vector2Int(i, wallHeight))));
            }
            for (var z = 0; z <= wallHeight; z++)
            {
                segments.Add((_isoGridService.WallGridToWorld(WallSide.Left, new Vector2Int(0, z)), _isoGridService.WallGridToWorld(WallSide.Left, new Vector2Int(gridHeight, z))));
            }

            // 右壁: x 方向の広がりは GridWidth
            for (var i = 0; i <= gridWidth; i++)
            {
                segments.Add((_isoGridService.WallGridToWorld(WallSide.Right, new Vector2Int(i, 0)), _isoGridService.WallGridToWorld(WallSide.Right, new Vector2Int(i, wallHeight))));
            }
            for (var z = 0; z <= wallHeight; z++)
            {
                segments.Add((_isoGridService.WallGridToWorld(WallSide.Right, new Vector2Int(0, z)), _isoGridService.WallGridToWorld(WallSide.Right, new Vector2Int(gridWidth, z))));
            }

            _view!.SetLines(GridSurface.Walls, segments);
        }
    }
}
