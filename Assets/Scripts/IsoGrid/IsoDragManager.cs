using System;
using UnityEngine;

namespace Cat
{
    public class IsoDragManager : MonoBehaviour
    {
        [SerializeField] IsoGridSystem _gridSystem;

        IsoDraggable _currentDraggable;

        public event Action<IsoDraggable> OnDragStarted;
        public event Action<IsoDraggable> OnDragEnded;

        public IsoDraggable CurrentDraggable => _currentDraggable;
        public bool IsDragging => _currentDraggable != null;

        /// <summary>
        /// ドラッグ開始を通知
        /// </summary>
        public void NotifyDragStarted(IsoDraggable draggable)
        {
            _currentDraggable = draggable;
            OnDragStarted?.Invoke(draggable);
        }

        /// <summary>
        /// ドラッグ終了を通知
        /// </summary>
        public void NotifyDragEnded(IsoDraggable draggable)
        {
            if (_currentDraggable == draggable)
            {
                _currentDraggable = null;
            }
            OnDragEnded?.Invoke(draggable);
        }

        /// <summary>
        /// 指定位置に他のオブジェクトが存在するかチェック
        /// </summary>
        public bool IsPositionOccupied(Vector2Int gridPos, IsoDraggable excludeObject = null)
        {
            var draggables = FindObjectsByType<IsoDraggable>(FindObjectsSortMode.None);

            foreach (var draggable in draggables)
            {
                if (draggable == excludeObject) continue;

                var otherGridPos = _gridSystem.WorldToFloorGrid(draggable.transform.position);
                if (otherGridPos == gridPos)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定位置が配置可能かチェック
        /// </summary>
        public bool CanPlaceAt(Vector2Int gridPos, IsoDraggable draggable)
        {
            if (!_gridSystem.IsValidFloorPosition(gridPos))
            {
                return false;
            }

            if (IsPositionOccupied(gridPos, draggable))
            {
                return false;
            }

            return true;
        }
    }
}