using Cat.Furniture;
using Home.State;
using Home.View;
using Root.Service;
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
        readonly RedecorateCameraService _redecorateCameraService;
        readonly FurnitureStowService _furnitureStowService;
        readonly IAudioService _audioService;
        readonly GridPreviewService _gridPreviewService;

        IsoDraggableView _currentIsoDraggableView;

        // ドラッグ中の現在の落下先
        DropTarget _currentDropTarget;

        // ドラッグ状態
        Vector3 _dragOffset;
        Vector2Int _dragStartFootprintPos;
        WallSide _dragStartWallSide;

        // FragmentedIsoGrid用ドラッグ状態
        FragmentedIsoGrid _dragStartFragmentedGrid;
        Vector2Int _dragStartLocalGridPos;

        [Inject]
        public IsoDragService(IsoInputService isoInputService, IsoGridService isoGridService, RedecorateCameraService redecorateCameraService, FurnitureStowService furnitureStowService, IAudioService audioService, GridPreviewService gridPreviewService)
        {
            _isoInputService = isoInputService;
            _isoGridService = isoGridService;
            _redecorateCameraService = redecorateCameraService;
            _furnitureStowService = furnitureStowService;
            _audioService = audioService;
            _gridPreviewService = gridPreviewService;
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

            // 家具を持ち上げた時点でしまうバーをうっすら表示する
            _furnitureStowService.OnFurnitureDragMove(_isoInputService.PointerScreenPosition);
        }

        /// ポインタードラッグ中の処理
        void HandlePointerDrag(Vector3 worldPos)
        {
            if (_currentIsoDraggableView == null) return;

            // 画面際までドラッグしたらカメラをゆっくりスクロールさせる
            _redecorateCameraService.OnFurnitureDragMove(_isoInputService.PointerScreenPosition);

            // 画面下部のしまうゾーン判定とUI表示を更新する
            _furnitureStowService.OnFurnitureDragMove(_isoInputService.PointerScreenPosition);

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
            }

            // WallSide反転後に落下先を解決する
            _currentDropTarget = ResolveDropTarget(newPos);
            _gridPreviewService.OnFurnitureDragMove(_currentDropTarget);

            if (_currentIsoDraggableView.IsWallPlacement)
            {
                // 壁配置の場合はSortingOrder 0
                _currentIsoDraggableView.SetSortingOrder(0);
            }
            else
            {
                // 床配置の場合、FragmentedIsoGridへの配置可能性をチェック
                UpdateDragSortingOrder(_currentDropTarget);
            }
        }

        /// ドラッグ中のSortingOrderと親子関係を更新
        void UpdateDragSortingOrder(in DropTarget target)
        {
            if (target.Surface == DropSurface.Fragmented)
            {
                // 配置可能な場合、FragmentedIsoGridの子に移動
                _currentIsoDraggableView.transform.SetParent(target.Grid.transform);

                var sortingOrder = IsoDraggableView.CalculateFragmentedSortingOrder(
                    target.FootprintStart, _currentIsoDraggableView.FootprintSize);
                _currentIsoDraggableView.SetSortingOrder(sortingOrder);
                return;
            }

            // 配置不可能またはFragmentedIsoGrid外の場合はRootに移動してSortingOrder 0
            _currentIsoDraggableView.transform.SetParent(null);
            _currentIsoDraggableView.SetSortingOrder(0);
        }

        /// 今離した場合の落下先を解決する（副作用なし）
        DropTarget ResolveDropTarget(Vector3 worldPos)
        {
            var footprintSize = _currentIsoDraggableView.FootprintSize;
            var pivotGridPosition = _currentIsoDraggableView.PivotGridPosition;
            var userFurnitureId = _currentIsoDraggableView.UserFurnitureId;

            if (_currentIsoDraggableView.IsWallPlacement)
            {
                var side = _currentIsoDraggableView.WallSide;
                var footprintStart = _isoGridService.WorldToWallGrid(side, worldPos) - pivotGridPosition;
                var canPlace = _isoGridService.CanPlaceWallObject(side, footprintStart, footprintSize, userFurnitureId);
                return new DropTarget(DropSurface.Wall, side, null, footprintStart, canPlace);
            }

            var fragmentedGrid = RaycastForFragmentedGrid(worldPos);
            if (fragmentedGrid is not null)
            {
                var localGridPos = fragmentedGrid.WorldToLocalGrid(worldPos);
                var fragmentedFootprintStart = localGridPos - pivotGridPosition;

                if (_isoGridService.CanPlaceFragmentedObject(fragmentedGrid, fragmentedFootprintStart, footprintSize, userFurnitureId))
                {
                    return new DropTarget(DropSurface.Fragmented, default, fragmentedGrid, fragmentedFootprintStart, true);
                }
            }

            var floorFootprintStart = _isoGridService.WorldToFloorGrid(worldPos) - pivotGridPosition;
            var canPlaceFloor = _isoGridService.CanPlaceFloorObject(floorFootprintStart, footprintSize, userFurnitureId);
            return new DropTarget(DropSurface.Floor, default, null, floorFootprintStart, canPlaceFloor);
        }

        /// ポインター離した時の処理
        void HandlePointerUp()
        {
            // ドラッグ終了と同時に画面際の自動スクロールも止める
            _redecorateCameraService.OnFurnitureDragEnd();

            // しまうゾーン内で離したかどうかを、UIを閉じる前に確定させる
            var stowRequested = _furnitureStowService.IsPointerInZone;
            _furnitureStowService.OnFurnitureDragEnd();
            _gridPreviewService.OnFurnitureDragEnd();

            if (_currentIsoDraggableView == null) return;

            if (stowRequested)
            {
                // ドラッグ開始時にグリッドから除去済みなので、配置せずシーンから除去してしまう
                _furnitureStowService.Stow(_currentIsoDraggableView);
            }
            else
            {
                EndDrag();
            }

            _currentIsoDraggableView = null;
        }

        /// ドラッグ開始
        void BeginDrag(Vector3 worldPos)
        {
            _currentIsoDraggableView.SetDragging(true);
            _audioService.PlaySe(SeId.RoomPick);

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

            _gridPreviewService.OnFurnitureDragBegin(_currentIsoDraggableView);
        }

        void BeginFloorDrag()
        {
            // FragmentedIsoGrid上にあるかチェック
            var fragmentedGrid = _currentIsoDraggableView.CurrentFragmentedGrid;
            if (fragmentedGrid is not null)
            {
                // FragmentedIsoGrid上からドラッグ開始
                _dragStartFragmentedGrid = fragmentedGrid;
                _dragStartLocalGridPos = _isoGridService.GetFragmentedObjectFootprintStart(fragmentedGrid, _currentIsoDraggableView.UserFurnitureId);
                _dragStartFootprintPos = Vector2Int.zero; // 床には配置されていない

                // 位置取得後にStateから削除
                _isoGridService.RemoveFragmentedObject(fragmentedGrid, _currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);

                _currentIsoDraggableView.SetCurrentFragmentedGrid(null);
                return;
            }

            // 床からドラッグ開始
            _dragStartFragmentedGrid = null;

            // Stateから現在のフットプリント開始位置を取得
            var currentFootprintStartPos = _isoGridService.GetFloorObjectFootprintStart(_currentIsoDraggableView.UserFurnitureId);

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = currentFootprintStartPos;
            _isoGridService.RemoveFloorObject(_currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);
        }

        void BeginWallDrag()
        {
            // Stateから現在のフットプリント開始位置を取得
            var (side, position) = _isoGridService.GetWallObjectFootprintStart(_currentIsoDraggableView.UserFurnitureId);

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = position;
            _dragStartWallSide = side;
            _isoGridService.RemoveWallObject(_currentIsoDraggableView.UserFurnitureId, _currentIsoDraggableView.FootprintSize);
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

            _audioService.PlaySe(SeId.RoomPlace);
        }

        void EndFloorDrag()
        {
            _currentIsoDraggableView.SetDragging(false);

            var target = ResolveDropTarget(_currentIsoDraggableView.Position);

            if (target.Surface == DropSurface.Fragmented && target.CanPlace)
            {
                PlaceOnFragmentedGrid(target.Grid, target.FootprintStart);
                return;
            }

            if (target.Surface == DropSurface.Floor && target.CanPlace)
            {
                PlaceOnFloor(target.FootprintStart);
                return;
            }

            // 配置不可能なら元の位置に戻す
            if (_dragStartFragmentedGrid is not null)
            {
                PlaceOnFragmentedGrid(_dragStartFragmentedGrid, _dragStartLocalGridPos);
            }
            else
            {
                PlaceOnFloor(_dragStartFootprintPos);
            }
        }

        void PlaceOnFragmentedGrid(FragmentedIsoGrid grid, Vector2Int footprintStart)
        {
            var userFurnitureId = _currentIsoDraggableView.UserFurnitureId;
            var footprintSize = _currentIsoDraggableView.FootprintSize;
            var pivotGridPosition = _currentIsoDraggableView.PivotGridPosition;

            _isoGridService.PlaceFragmentedObject(grid, footprintStart, footprintSize, userFurnitureId);

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
            _currentIsoDraggableView.SetSortingOrder(0);
        }

        /// RayCastでFragmentedIsoGridを検出（自身のColliderは除外）
        FragmentedIsoGrid RaycastForFragmentedGrid(Vector3 worldPos)
        {
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, -1);

            foreach (var hit in hits)
            {
                var fragmentedGrid = hit.collider.GetComponent<FragmentedIsoGrid>();
                if (fragmentedGrid is null) continue;

                // 自身のColliderに属するFragmentedIsoGridは除外
                var draggableInParent = hit.collider.GetComponentInParent<IsoDraggableView>();
                if (draggableInParent == _currentIsoDraggableView) continue;

                return fragmentedGrid;
            }

            return null;
        }

        void EndWallDrag()
        {
            var footprintSize = _currentIsoDraggableView.FootprintSize;

            var target = ResolveDropTarget(_currentIsoDraggableView.Position);

            Vector2Int finalFootprintPos;
            WallSide finalWallSide;

            // 同じ壁面で配置可能かチェック
            if (target.CanPlace)
            {
                finalFootprintPos = target.FootprintStart;
                finalWallSide = target.Side;
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
            while (current.CurrentFragmentedGrid is not null)
            {
                var parentDraggable = current.CurrentFragmentedGrid.IsoDraggableView;
                if (parentDraggable is null) break;
                current = parentDraggable;
                depth++;
            }
            return (current, depth);
        }

    }
}
