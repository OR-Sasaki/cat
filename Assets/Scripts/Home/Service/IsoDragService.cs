using Cat.Furniture;
using Home.State;
using Home.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Home.Service
{
    /// ドラッグによるオブジェクト移動を管理するService
    /// Raycastで最前面のIsoDraggableViewを検出し、グリッドへの配置を行う
    public class IsoDragService : IStartable
    {
        readonly IsoGridService _isoGridService;
        readonly IsoInputService _isoInputService;

        IsoDraggableView _currentIsoDraggableView;

        // ドラッグ状態
        Vector3 _dragOffset;
        Vector2Int _dragStartFootprintPos;
        WallSide _dragStartWallSide;

        // FragmentedIsoGrid用ドラッグ状態
        FragmentedIsoGrid _dragStartFragmentedGrid;
        Vector2Int _dragStartLocalGridPos;

        [Inject]
        public IsoDragService(IsoInputService isoInputService, IsoGridService isoGridService)
        {
            _isoInputService = isoInputService;
            _isoGridService = isoGridService;
        }

        public void Start()
        {
            _isoInputService.OnPointerDown.AddListener(HandlePointerDown);
            _isoInputService.OnPointerDrag.AddListener(HandlePointerDrag);
            _isoInputService.OnPointerUp.AddListener(HandlePointerUp);
        }

        /// ポインター押下時の処理
        void HandlePointerDown(Vector3 worldPos)
        {
            var draggable = RaycastForDraggable(worldPos);
            if (draggable == null) return;

            _currentIsoDraggableView = draggable;
            BeginDrag(worldPos);
        }

        /// ポインタードラッグ中の処理
        void HandlePointerDrag(Vector3 worldPos)
        {
            if (_currentIsoDraggableView == null) return;

            var newPos = worldPos + _dragOffset;
            _currentIsoDraggableView.SetPosition(newPos);

            // 壁オブジェクトの場合、IsoGrid座標のX座標がマイナスになったらWallSideを切り替える
            if (_currentIsoDraggableView.IsWallPlacement)
            {
                var currentWallSide = _currentIsoDraggableView.WallSide;
                var gridPos = _isoGridService.WorldToWallGridNotRound(currentWallSide, newPos);

                if (gridPos.x < 0)
                {
                    var newWallSide = currentWallSide == WallSide.Left ? WallSide.Right : WallSide.Left;
                    _currentIsoDraggableView.SetWallSide(newWallSide);
                }

                // 壁配置の場合はSortingOrder 0
                _currentIsoDraggableView.SetSortingOrder(0);
            }
            else
            {
                // 床配置の場合、FragmentedIsoGridへの配置可能性をチェック
                UpdateDragSortingOrder(newPos);
            }
        }

        /// ドラッグ中のSortingOrderと親子関係を更新
        void UpdateDragSortingOrder(Vector3 worldPos)
        {
            var fragmentedGrid = RaycastForFragmentedGrid(worldPos);
            if (fragmentedGrid != null)
            {
                var localGridPos = fragmentedGrid.WorldToLocalGrid(worldPos);
                var footprintStart = localGridPos - _currentIsoDraggableView.PivotGridPosition;

                if (fragmentedGrid.CanPlace(
                    footprintStart,
                    _currentIsoDraggableView.FootprintSize,
                    _currentIsoDraggableView.IsWallPlacement,
                    _currentIsoDraggableView.UserFurnitureId))
                {
                    // 配置可能な場合、FragmentedIsoGridの子に移動
                    _currentIsoDraggableView.transform.SetParent(fragmentedGrid.transform);

                    // ローカルグリッド座標のx + yをSortingOrderに設定
                    var sortingOrder = footprintStart.x + footprintStart.y;
                    _currentIsoDraggableView.SetSortingOrder(sortingOrder);
                    return;
                }
            }

            // 配置不可能またはFragmentedIsoGrid外の場合はRootに移動してSortingOrder 0
            _currentIsoDraggableView.transform.SetParent(null);
            _currentIsoDraggableView.SetSortingOrder(0);
        }

        /// ポインター離した時の処理
        void HandlePointerUp()
        {
            if (_currentIsoDraggableView == null) return;
            EndDrag();
            _currentIsoDraggableView = null;
        }

        /// ドラッグ開始
        void BeginDrag(Vector3 worldPos)
        {
            _currentIsoDraggableView.SetDragging(true);

            // マウス位置とオブジェクト位置の差分を記録
            _dragOffset = _currentIsoDraggableView.Position - worldPos;

            if (_currentIsoDraggableView.IsWallPlacement)
            {
                BeginWallDrag();
            }
            else
            {
                BeginFloorDrag();
            }
        }

        void BeginFloorDrag()
        {
            // FragmentedIsoGrid上にあるかチェック
            var fragmentedGrid = _currentIsoDraggableView.CurrentFragmentedGrid;
            if (fragmentedGrid != null)
            {
                // FragmentedIsoGrid上からドラッグ開始
                _dragStartFragmentedGrid = fragmentedGrid;
                _dragStartLocalGridPos = fragmentedGrid.GetObjectFootprintStart(_currentIsoDraggableView.UserFurnitureId);
                _dragStartFootprintPos = Vector2Int.zero; // 床には配置されていない

                // 位置取得後にRemove
                fragmentedGrid.RemoveObject(_currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);

                // Stateからも削除
                var parentId = fragmentedGrid.GetParentUserFurnitureId();
                if (parentId != 0)
                {
                    _isoGridService.RemoveFragmentedObject(parentId, _currentIsoDraggableView.UserFurnitureId);
                }

                _currentIsoDraggableView.SetPlacedOnGrid(false);
                _currentIsoDraggableView.SetCurrentFragmentedGrid(null);
                return;
            }

            // 床からドラッグ開始
            _dragStartFragmentedGrid = null;

            // Stateから現在のフットプリント開始位置を取得
            var currentFootprintStartPos = _isoGridService.GetFloorObjectFootprintStart(_currentIsoDraggableView.UserFurnitureId);

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = currentFootprintStartPos;
            if (_currentIsoDraggableView.IsPlacedOnGrid)
            {
                _isoGridService.RemoveFloorObject(_currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);
                _currentIsoDraggableView.SetPlacedOnGrid(false);
            }
        }

        void BeginWallDrag()
        {
            // Stateから現在のフットプリント開始位置を取得
            var wallObjectPos = _isoGridService.GetWallObjectFootprintStart(_currentIsoDraggableView.UserFurnitureId);

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = wallObjectPos.Position;
            _dragStartWallSide = wallObjectPos.Side;
            if (_currentIsoDraggableView.IsPlacedOnGrid)
            {
                _isoGridService.RemoveWallObject(_currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);
                _currentIsoDraggableView.SetPlacedOnGrid(false);
            }
        }

        /// ドラッグ終了
        void EndDrag()
        {
            if (_currentIsoDraggableView == null) return;

            if (_currentIsoDraggableView.IsWallPlacement)
            {
                EndWallDrag();
            }
            else
            {
                EndFloorDrag();
            }
        }

        void EndFloorDrag()
        {
            var userFurnitureId = _currentIsoDraggableView.UserFurnitureId;
            var footprintSize = _currentIsoDraggableView.FootprintSize;
            var pivotGridPosition = _currentIsoDraggableView.PivotGridPosition;
            var isWallPlacement = _currentIsoDraggableView.IsWallPlacement;

            // FragmentedIsoGridへの配置を試行
            var fragmentedGrid = RaycastForFragmentedGrid(_currentIsoDraggableView.Position);
            if (fragmentedGrid != null)
            {
                var localGridPos = fragmentedGrid.WorldToLocalGrid(_currentIsoDraggableView.Position);
                var footprintStart = localGridPos - pivotGridPosition;

                if (fragmentedGrid.CanPlace(footprintStart, footprintSize, isWallPlacement, userFurnitureId))
                {
                    PlaceOnFragmentedGrid(fragmentedGrid, footprintStart);
                    return;
                }
            }

            // 床への配置を試行
            var gridPos = _isoGridService.WorldToFloorGrid(_currentIsoDraggableView.Position);
            var newFootprintStart = gridPos - pivotGridPosition;

            if (_isoGridService.CanPlaceFloorObject(newFootprintStart, footprintSize, userFurnitureId))
            {
                PlaceOnFloor(newFootprintStart);
                return;
            }

            // 配置不可能なら元の位置に戻す
            if (_dragStartFragmentedGrid != null)
            {
                PlaceOnFragmentedGrid(_dragStartFragmentedGrid, _dragStartLocalGridPos);
            }
            else
            {
                PlaceOnFloor(_dragStartFootprintPos);
            }

            _currentIsoDraggableView.SetPlacedOnGrid(true);
            _currentIsoDraggableView.SetSortingOrder(0);
            _currentIsoDraggableView.SetDragging(false);
        }

        void PlaceOnFragmentedGrid(FragmentedIsoGrid grid, Vector2Int footprintStart)
        {
            var userFurnitureId = _currentIsoDraggableView.UserFurnitureId;
            var footprintSize = _currentIsoDraggableView.FootprintSize;
            var pivotGridPosition = _currentIsoDraggableView.PivotGridPosition;

            grid.PlaceObject(footprintStart, footprintSize, userFurnitureId);

            var parentId = grid.GetParentUserFurnitureId();
            if (parentId != 0)
            {
                var depth = _isoGridService.CalculateFragmentedGridDepth(grid);
                _isoGridService.PlaceFragmentedObject(parentId, userFurnitureId, footprintStart, depth);
            }

            _currentIsoDraggableView.transform.SetParent(grid.transform);
            var snapPos = grid.LocalGridToWorld(footprintStart + pivotGridPosition);
            _currentIsoDraggableView.SetPosition(snapPos);
            _currentIsoDraggableView.SetCurrentFragmentedGrid(grid);
        }

        void PlaceOnFloor(Vector2Int footprintStart)
        {
            _currentIsoDraggableView.transform.SetParent(null);
            _isoGridService.PlaceFloorObject(
                footprintStart,
                _currentIsoDraggableView.FootprintSize,
                _currentIsoDraggableView.UserFurnitureId);
            _currentIsoDraggableView.SetPosition(SnapToFloorGrid(footprintStart));
            _currentIsoDraggableView.SetCurrentFragmentedGrid(null);
        }

        /// RayCastでFragmentedIsoGridを検出（自身のColliderは除外）
        FragmentedIsoGrid RaycastForFragmentedGrid(Vector3 worldPos)
        {
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, -1);

            foreach (var hit in hits)
            {
                var fragmentedGrid = hit.collider.GetComponent<FragmentedIsoGrid>();
                if (fragmentedGrid == null) continue;

                // 自身のColliderに属するFragmentedIsoGridは除外
                var draggableInParent = hit.collider.GetComponentInParent<IsoDraggableView>();
                if (draggableInParent == _currentIsoDraggableView) continue;

                return fragmentedGrid;
            }

            return null;
        }

        void EndWallDrag()
        {
            var wallSide = _currentIsoDraggableView.WallSide;
            var footprintSize = _currentIsoDraggableView.FootprintSize;

            // 壁グリッド上での新しい位置を計算
            var newFootprintStartPos = _isoGridService.WorldToWallGrid(wallSide, _currentIsoDraggableView.Position) - _currentIsoDraggableView.PivotGridPosition;

            Vector2Int finalFootprintPos;
            WallSide finalWallSide;

            // 同じ壁面で配置可能かチェック
            if (_isoGridService.CanPlaceWallObject(wallSide, newFootprintStartPos, footprintSize, _currentIsoDraggableView.UserFurnitureId))
            {
                finalFootprintPos = newFootprintStartPos;
                finalWallSide = wallSide;
            }
            else
            {
                // 配置不可能なら元の位置に戻す
                finalFootprintPos = _dragStartFootprintPos;
                finalWallSide = _dragStartWallSide;
            }

            _isoGridService.PlaceWallObject(finalWallSide, finalFootprintPos, footprintSize, _currentIsoDraggableView.UserFurnitureId);
            _currentIsoDraggableView.SetPosition(SnapToWallGrid(finalWallSide, finalFootprintPos));
            _currentIsoDraggableView.SetWallSide(finalWallSide);
            _currentIsoDraggableView.SetPlacedOnGrid(true);
            _currentIsoDraggableView.SetSortingOrder(0);
            _currentIsoDraggableView.SetDragging(false);
        }

        /// 床のフットプリント開始位置からピボット位置のワールド座標を計算
        Vector3 SnapToFloorGrid(Vector2Int footprintStartPos)
        {
            var pivotGridPos = footprintStartPos + _currentIsoDraggableView.PivotGridPosition;
            return _isoGridService.FloorGridToWorld(pivotGridPos);
        }

        /// 壁のフットプリント開始位置からピボット位置のワールド座標を計算
        Vector3 SnapToWallGrid(WallSide side, Vector2Int footprintStartPos)
        {
            var pivotGridPos = footprintStartPos + _currentIsoDraggableView.PivotGridPosition;
            return _isoGridService.WallGridToWorld(side, pivotGridPos);
        }

        /// Raycastで最前面のIsoDraggableViewを検出
        IsoDraggableView RaycastForDraggable(Vector3 worldPos)
        {
            // 2D Raycastで全てのヒットを取得
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, -1);

            if (hits.Length == 0) return null;

            IsoDraggableView bestDraggable = null;
            float bestY = float.MaxValue;
            int bestDepth = -1;

            // 最も手前(Yが小さい)Draggableを探す
            // CurrentFragmentedGridが設定されている場合は、再帰的に最も親のIsoDraggableViewを探索して、そのYを採用する
            // また、最も深い位置にあるオブジェクトが優先される
            foreach (var hit in hits)
            {
                var draggable = hit.collider.GetComponentInParent<IsoDraggableView>();
                if (draggable == null) continue;

                // 比較用Yは最も親のIsoDraggableViewのViewPivotYを使用
                var (rootDraggable, depth) = GetRootIsoDraggableViewAndDepth(draggable);
                var compareY = rootDraggable.ViewPivotY;

                // Yが小さい方が優先、同じYなら深い方が優先
                const float epsilon = 0.001f;
                if (compareY > bestY + epsilon) continue;
                if (Mathf.Abs(compareY - bestY) < epsilon && depth <= bestDepth) continue;

                bestDraggable = draggable;
                bestY = compareY;
                bestDepth = depth;
            }

            return bestDraggable;
        }

        /// 再帰的に最も親のIsoDraggableViewと階層の深さを取得
        (IsoDraggableView Root, int Depth) GetRootIsoDraggableViewAndDepth(IsoDraggableView draggable)
        {
            var depth = 0;
            var current = draggable;
            while (current.CurrentFragmentedGrid != null)
            {
                var parentDraggable = current.CurrentFragmentedGrid.IsoDraggableView;
                if (parentDraggable == null) break;
                current = parentDraggable;
                depth++;
            }
            return (current, depth);
        }

    }
}
