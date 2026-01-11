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
        const int DragSortingOrderBoost = 100;

        readonly IsoInputService _inputService;
        readonly IsoGridSystemView _isoGridSystemView;

        IsoDraggableView _currentIsoDraggableView;

        // ドラッグ状態
        Vector3 _dragOffset;
        Vector2Int _dragStartFootprintPos;

        [Inject]
        public IsoDragService(IsoInputService inputService, IsoGridSystemView isoGridSystemView)
        {
            _inputService = inputService;
            _isoGridSystemView = isoGridSystemView;

            _inputService.OnPointerDown.AddListener(HandlePointerDown);
            _inputService.OnPointerDrag.AddListener(HandlePointerDrag);
            _inputService.OnPointerUp.AddListener(HandlePointerUp);
        }

        public void Start()
        {
        }

        /// ポインター押下時の処理
        void HandlePointerDown(Vector3 worldPos)
        {
            var draggable = RaycastForDraggable(worldPos);
            if (draggable == null) return;

            _currentIsoDraggableView = draggable;
            BeginDrag(worldPos);
        }

        /// <summary>
        /// ポインタードラッグ中の処理
        /// </summary>
        void HandlePointerDrag(Vector3 worldPos)
        {
            if (_currentIsoDraggableView == null) return;
            // 位置更新
            _currentIsoDraggableView.SetPosition(worldPos + _dragOffset);
        }

        /// <summary>
        /// ポインター離した時の処理
        /// </summary>
        void HandlePointerUp()
        {
            if (_currentIsoDraggableView == null) return;
            EndDrag();
            _currentIsoDraggableView = null;
        }

        /// <summary>
        /// ドラッグ開始
        /// </summary>
        void BeginDrag(Vector3 worldPos)
        {
            _currentIsoDraggableView.SetDragging(true);

            // マウス位置とオブジェクト位置の差分を記録
            _dragOffset = _currentIsoDraggableView.Position - worldPos;

            // 現在のグリッド位置を計算
            var gridPos = _isoGridSystemView.WorldToFloorGrid(_currentIsoDraggableView.Position);
            var currentFootprintStartPos = gridPos - _currentIsoDraggableView.PivotGridPosition;

            // ドラッグ開始位置を保存し、現在の位置からオブジェクトを削除
            _dragStartFootprintPos = currentFootprintStartPos;
            if (_currentIsoDraggableView.IsPlacedOnGrid)
            {
                _isoGridSystemView.RemoveObject(currentFootprintStartPos, _currentIsoDraggableView.FootprintSize);
                _currentIsoDraggableView.SetPlacedOnGrid(false);
            }

            // ソートオーダーを一時的に上げる
            _currentIsoDraggableView.BoostSortingOrder(DragSortingOrderBoost);
        }

        /// <summary>
        /// ドラッグ終了
        /// </summary>
        void EndDrag()
        {
            if (_currentIsoDraggableView == null) return;

            // グリッドにスナップして配置
            var gridPos = _isoGridSystemView.WorldToFloorGrid(_currentIsoDraggableView.Position);
            var newFootprintStartPos = gridPos - _currentIsoDraggableView.PivotGridPosition;

            Vector2Int finalFootprintPos;
            // 配置可能かチェック
            if (_isoGridSystemView.CanPlaceObject(newFootprintStartPos, _currentIsoDraggableView.FootprintSize, _currentIsoDraggableView.ObjectId))
            {
                // 新しい位置に配置
                finalFootprintPos = newFootprintStartPos;
            }
            else
            {
                // 配置不可能なら元の位置に戻す
                finalFootprintPos = _dragStartFootprintPos;
            }

            _currentIsoDraggableView.SetPosition(SnapToGrid(finalFootprintPos));
            _isoGridSystemView.PlaceObject(finalFootprintPos, _currentIsoDraggableView.FootprintSize, _currentIsoDraggableView.ObjectId);
            _currentIsoDraggableView.SetPlacedOnGrid(true);

            // ソートオーダーを元に戻す
            _currentIsoDraggableView.ResetSortingOrder();
            _currentIsoDraggableView.SetDragging(false);
        }

        /// フットプリント開始位置からピボット位置のワールド座標を計算
        Vector3 SnapToGrid(Vector2Int footprintStartPos)
        {
            var pivotGridPos = footprintStartPos + _currentIsoDraggableView.PivotGridPosition;
            return _isoGridSystemView.GridToWorld(pivotGridPos);
        }

        /// <summary>
        /// Raycastで最前面のIsoDraggableViewを検出
        /// </summary>
        IsoDraggableView RaycastForDraggable(Vector3 worldPos)
        {
            // 2D Raycastで全てのヒットを取得
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, -1);

            if (hits.Length == 0) return null;

            IsoDraggableView bestDraggable = null;
            float bestY = float.MaxValue;

            // 最も手前(Yが小さい)Draggableを探す
            foreach (var hit in hits)
            {
                Debug.Log(hit.collider.gameObject.name);
                var draggable = hit.collider.GetComponent<IsoDraggableView>();
                if (draggable == null) continue;
                if (draggable.ViewPivotY > bestY) continue;

                bestDraggable = draggable;
                bestY = draggable.ViewPivotY;
            }

            return bestDraggable;
        }
    }
}
