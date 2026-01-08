using UnityEngine;
using UnityEngine.InputSystem;

namespace Cat
{
    /// <summary>
    /// Raycastベースのドラッグ管理システム
    /// 前面のColliderを貫通してIsoDraggableを検出する
    /// </summary>
    public class IsoDragManager : MonoBehaviour
    {
        [SerializeField] LayerMask _draggableLayerMask = -1;

        Camera _mainCamera;
        IsoDraggable _currentDraggable;

        void Awake()
        {
            _mainCamera = Camera.main;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mouseWorldPos = GetMouseWorldPosition(mouse);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                HandleMouseDown(mouseWorldPos);
            }
            else if (mouse.leftButton.isPressed && _currentDraggable != null)
            {
                HandleMouseDrag(mouseWorldPos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _currentDraggable != null)
            {
                HandleMouseUp();
            }
        }

        /// <summary>
        /// マウスダウン時の処理
        /// </summary>
        void HandleMouseDown(Vector3 mouseWorldPos)
        {
            var draggable = RaycastForDraggable(mouseWorldPos);
            if (draggable == null) return;

            _currentDraggable = draggable;
            _currentDraggable.BeginDrag(mouseWorldPos);
        }

        /// <summary>
        /// マウスドラッグ中の処理
        /// </summary>
        void HandleMouseDrag(Vector3 mouseWorldPos)
        {
            _currentDraggable.UpdateDrag(mouseWorldPos);
        }

        /// <summary>
        /// マウスアップ時の処理
        /// </summary>
        void HandleMouseUp()
        {
            _currentDraggable.EndDrag();
            _currentDraggable = null;
        }

        /// <summary>
        /// Raycastで最前面のIsoDraggableを検出
        /// </summary>
        IsoDraggable RaycastForDraggable(Vector3 worldPos)
        {
            // 2D Raycastで全てのヒットを取得
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, _draggableLayerMask);

            if (hits.Length == 0) return null;

            IsoDraggable bestDraggable = null;
            float bestY = float.MaxValue;

            // 最も手前(Yが小さい)Draggableを探す
            foreach (var hit in hits)
            {
                var draggable = hit.collider.GetComponent<IsoDraggable>();
                if (draggable == null) continue;
                if (draggable.ViewPivotY > bestY) continue;

                bestDraggable = draggable;
                bestY = draggable.ViewPivotY;
            }

            return bestDraggable;
        }

        /// <summary>
        /// マウスのワールド座標を取得
        /// </summary>
        Vector3 GetMouseWorldPosition(Mouse mouse)
        {
            var mousePos = mouse.position.ReadValue();
            var screenPos = new Vector3(mousePos.x, mousePos.y, -_mainCamera.transform.position.z);
            return _mainCamera.ScreenToWorldPoint(screenPos);
        }
    }
}
