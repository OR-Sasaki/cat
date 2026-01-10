using Home.State;
using Home.View;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Home.Service
{
    /// <summary>
    /// Raycastベースのドラッグ管理システム
    /// 前面のColliderを貫通してIsoDraggableViewを検出する
    /// PC: マウス入力、スマホ: タッチ入力に対応
    /// </summary>
    public class IsoDragService : ITickable, IInitializable
    {
        readonly HomeState _homeState;

        Camera _mainCamera;
        IsoDraggableView _currentDraggable;
        bool _isActive;
        LayerMask _draggableLayerMask = -1;

        public IsoDragService(HomeState homeState)
        {
            _homeState = homeState;
        }

        public void Initialize()
        {
            _mainCamera = Camera.main;
            _homeState.OnStateChange.AddListener(OnStateChange);
        }

        void OnStateChange(HomeState.State previous, HomeState.State current)
        {
            _isActive = current == HomeState.State.Redecorate;
        }

        public void Tick()
        {
            if (!_isActive) return;

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
                case TouchPhase.Began:
                    HandlePointerDown(worldPos);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_currentDraggable != null)
                        HandlePointerDrag(worldPos);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
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
        /// Raycastで最前面のIsoDraggableViewを検出
        /// </summary>
        IsoDraggableView RaycastForDraggable(Vector3 worldPos)
        {
            // 2D Raycastで全てのヒットを取得
            var hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0f, _draggableLayerMask);

            if (hits.Length == 0) return null;

            IsoDraggableView bestDraggable = null;
            float bestY = float.MaxValue;

            // 最も手前(Yが小さい)Draggableを探す
            foreach (var hit in hits)
            {
                var draggable = hit.collider.GetComponent<IsoDraggableView>();
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
