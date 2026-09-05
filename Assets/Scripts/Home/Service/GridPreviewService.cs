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
        readonly IsoGridSettingsView _isoGridSettingsView;

        GridPreviewView? _view;
        bool _viewWarningLogged;

        IsoDraggableView? _draggable;
        bool _linesBuilt;

        DropTarget _target;

        // Evaluate に渡す判定関数（フレーム毎の確保を避けるため使い回す）
        readonly Func<GridCell, bool> _isInRange;
        readonly Func<GridCell, int> _occupantIdOf;

        readonly List<Vector3> _footprintCorners = new();

        [Inject]
        public GridPreviewService(IsoGridService isoGridService, IsoGridSettingsView isoGridSettingsView)
        {
            _isoGridService = isoGridService;
            _isoGridSettingsView = isoGridSettingsView;

            _isInRange = cell => IsInRange(_target, new Vector2Int(cell.X, cell.Y));
            _occupantIdOf = cell => OccupantIdOf(_target, new Vector2Int(cell.X, cell.Y));
        }

        public void AttachView(GridPreviewView view)
        {
            _view = view;
        }

        public void OnFurnitureDragBegin(IsoDraggableView draggable)
        {
            if (!HasView()) return;

            _draggable = draggable;

            BuildLines();
            _view!.ShowLines(draggable.IsWallPlacement ? GridSurface.Walls : GridSurface.Floor);
        }

        public void OnFurnitureDragMove(in DropTarget target)
        {
            if (!HasView()) return;
            if (_draggable == null) return;

            _target = target;

            if (target.Surface == DropSurface.Fragmented && target.Grid == null)
            {
                Debug.LogError("[GridPreviewService] Fragmented target without grid");
                _view!.ClearFootprint();
                return;
            }

            var parent = target.Surface == DropSurface.Fragmented ? target.Grid!.transform : null;

            var start = new GridCell(target.FootprintStart.x, target.FootprintStart.y);
            var size = new GridCell(_draggable.FootprintSize.x, _draggable.FootprintSize.y);
            var selfId = _draggable.UserFurnitureId;

            var evaluation = FootprintEvaluator.Evaluate(start, size, selfId, _isInRange, _occupantIdOf);

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

        /// 落下先の面ごとの範囲内チェック
        bool IsInRange(in DropTarget target, Vector2Int cell)
        {
            switch (target.Surface)
            {
                case DropSurface.Floor: return _isoGridService.IsValidFloorPosition(cell);
                case DropSurface.Wall: return _isoGridService.IsValidWallPosition(target.Side, cell);
                case DropSurface.Fragmented: return target.Grid!.IsValidLocalPosition(cell);
                default: throw new ArgumentOutOfRangeException(nameof(target.Surface));
            }
        }

        /// 落下先の面ごとの占有UserFurnitureId取得
        int OccupantIdOf(in DropTarget target, Vector2Int cell)
        {
            switch (target.Surface)
            {
                case DropSurface.Floor: return _isoGridService.GetFloorUserFurnitureId(cell);
                case DropSurface.Wall: return _isoGridService.GetWallUserFurnitureId(target.Side, cell);
                case DropSurface.Fragmented: return _isoGridService.GetFragmentedUserFurnitureId(target.Grid!, cell);
                default: throw new ArgumentOutOfRangeException(nameof(target.Surface));
            }
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

        void BuildLines()
        {
            if (_linesBuilt) return;
            _linesBuilt = true;

            var gridWidth = _isoGridSettingsView.GridWidth;
            var gridHeight = _isoGridSettingsView.GridHeight;
            var wallHeight = _isoGridSettingsView.WallHeight;

            var floorSegments = new List<(Vector3 Start, Vector3 End)>();
            for (var x = 0; x <= gridWidth; x++)
            {
                floorSegments.Add((_isoGridService.FloorGridToWorld(new Vector2Int(x, 0)), _isoGridService.FloorGridToWorld(new Vector2Int(x, gridHeight))));
            }
            for (var y = 0; y <= gridHeight; y++)
            {
                floorSegments.Add((_isoGridService.FloorGridToWorld(new Vector2Int(0, y)), _isoGridService.FloorGridToWorld(new Vector2Int(gridWidth, y))));
            }
            _view!.SetLines(GridSurface.Floor, floorSegments);

            var wallSegments = new List<(Vector3 Start, Vector3 End)>();

            void AddWallLines(List<(Vector3 Start, Vector3 End)> segments, WallSide side, int span)
            {
                for (var i = 0; i <= span; i++)
                {
                    segments.Add((_isoGridService.WallGridToWorld(side, new Vector2Int(i, 0)), _isoGridService.WallGridToWorld(side, new Vector2Int(i, wallHeight))));
                }
                for (var z = 0; z <= wallHeight; z++)
                {
                    segments.Add((_isoGridService.WallGridToWorld(side, new Vector2Int(0, z)), _isoGridService.WallGridToWorld(side, new Vector2Int(span, z))));
                }
            }

            // 左壁: x 方向の広がりは GridHeight
            AddWallLines(wallSegments, WallSide.Left, gridHeight);
            // 右壁: x 方向の広がりは GridWidth
            AddWallLines(wallSegments, WallSide.Right, gridWidth);

            _view!.SetLines(GridSurface.Walls, wallSegments);
        }
    }
}
