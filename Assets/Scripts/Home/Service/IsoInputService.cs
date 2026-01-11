using Home.State;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Home.Service
{
    /// <summary>
    /// マウス/タッチ入力を検出し、ポインターイベントを発行するService
    /// </summary>
    public class IsoInputService : ITickable, IInitializable
    {
        readonly HomeState _homeState;

        Camera _mainCamera;
        bool _isActive;
        bool _isPointerDown;

        public readonly UnityEvent<Vector3> OnPointerDown = new();
        public readonly UnityEvent<Vector3> OnPointerDrag = new();
        public readonly UnityEvent OnPointerUp = new();

        [Inject]
        public IsoInputService(HomeState homeState)
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

            // ドラッグ中にStateが変わった場合、ポインターUpを発行
            if (current != HomeState.State.Redecorate && previous == HomeState.State.Redecorate)
            {
                if (_isPointerDown)
                {
                    _isPointerDown = false;
                    OnPointerUp.Invoke();
                }
            }
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
                    _isPointerDown = true;
                    OnPointerDown.Invoke(worldPos);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_isPointerDown)
                        OnPointerDrag.Invoke(worldPos);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isPointerDown)
                    {
                        _isPointerDown = false;
                        OnPointerUp.Invoke();
                    }
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
                _isPointerDown = true;
                OnPointerDown.Invoke(worldPos);
            }
            else if (mouse.leftButton.isPressed && _isPointerDown)
            {
                OnPointerDrag.Invoke(worldPos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _isPointerDown)
            {
                _isPointerDown = false;
                OnPointerUp.Invoke();
            }
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
