using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Cat
{
    /// <summary>
    /// Raycastベースのドラッグ管理システム
    /// 前面のColliderを貫通してIsoDraggableを検出する
    /// PC: マウス入力、スマホ: タッチ入力に対応
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
            // タッチ入力を優先してチェック
            if (Touch.activeTouches.Count > 0)
            {
                HandleTouchInput();
            }
            else
            {
                HandleMouseInput();
            }
        }

        void HandleTouchInput()
        {
            var touch = Touch.activeTouches[0];
            var worldPos = ScreenToWorldPosition(touch.screenPosition);

            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    HandlePointerDown(worldPos);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (_currentDraggable != null)
                        HandlePointerDrag(worldPos);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (_currentDraggable != null)
                        HandlePointerUp();
                    break;
            }
        }

        void HandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var worldPos = ScreenToWorldPosition(mouse.position.ReadValue());

            if (mouse.leftButton.wasPressedThisFrame)
            {
                HandlePointerDown(worldPos);
            }
            else if (mouse.leftButton.isPressed && _currentDraggable != null)
            {
                HandlePointerDrag(worldPos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _currentDraggable != null)
            {
                HandlePointerUp();
            }
        }

        /// <summary>
        /// ポインター押下時の処理
        /// </summary>
        void HandlePointerDown(Vector3 worldPos)
        {
            var draggable = RaycastForDraggable(worldPos);
            if (draggable == null) return;

            _currentDraggable = draggable;
            _currentDraggable.BeginDrag(worldPos);
        }

        /// <summary>
        /// ポインタードラッグ中の処理
        /// </summary>
        void HandlePointerDrag(Vector3 worldPos)
        {
            _currentDraggable.UpdateDrag(worldPos);
        }

        /// <summary>
        /// ポインター離した時の処理
        /// </summary>
        void HandlePointerUp()
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
        /// スクリーン座標をワールド座標に変換
        /// </summary>
        Vector3 ScreenToWorldPosition(Vector2 screenPos)
        {
            var pos = new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z);
            return _mainCamera.ScreenToWorldPoint(pos);
        }
    }
}
