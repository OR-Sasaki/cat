using Home.State;
using Home.View;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Home.Service
{
    public class RedecorateCameraService : ITickable, IInitializable
    {
        readonly HomeState _homeState;
        readonly CameraView _cameraView;

        CinemachineCamera _cinemachineCamera;
        bool _isActive;

        // ズーム設定
        float _initialOrthographicSize;
        const float MinOrthographicSize = 2f;
        const float MaxOrthographicSize = 30f;
        const float ZoomSpeed = 0.005f;

        // パン設定
        const float PanSpeed = 0.0005f;

        // タッチ状態
        float _previousPinchDistance;
        Vector2 _previousTwoFingerCenter;
        bool _wasTwoFingerActive;

        public RedecorateCameraService(HomeState homeState, CameraView cameraView)
        {
            _homeState = homeState;
            _cameraView = cameraView;
        }

        public void Initialize()
        {
            _homeState.OnStateChange.AddListener(OnStateChange);
        }

        void OnStateChange(HomeState.State previous, HomeState.State current)
        {
            if (current == HomeState.State.Redecorate)
            {
                Activate();
            }
            else if (previous == HomeState.State.Redecorate)
            {
                Deactivate();
            }
        }

        void Activate()
        {
            _cinemachineCamera = _cameraView.GetCinemachineCamera(HomeState.State.Redecorate);
            if (_cinemachineCamera == null)
            {
                Debug.LogWarning("[RedecorateCameraService] CinemachineCamera not found for Redecorate state");
                return;
            }

            _initialOrthographicSize = _cinemachineCamera.Lens.OrthographicSize;
            _isActive = true;
            _wasTwoFingerActive = false;
        }

        void Deactivate()
        {
            if (_cinemachineCamera != null)
            {
                // 元のズーム値に戻す
                var lens = _cinemachineCamera.Lens;
                lens.OrthographicSize = _initialOrthographicSize;
                _cinemachineCamera.Lens = lens;
            }

            _isActive = false;
            _cinemachineCamera = null;
        }

        public void Tick()
        {
            if (!_isActive || _cinemachineCamera == null) return;

            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;

            var touch0 = touchscreen.touches[0];
            var touch1 = touchscreen.touches[1];

            var touch0Active = touch0.press.isPressed;
            var touch1Active = touch1.press.isPressed;

            // 2本指が両方アクティブな場合のみ処理
            if (touch0Active && touch1Active)
            {
                var pos0 = touch0.position.ReadValue();
                var pos1 = touch1.position.ReadValue();

                var currentPinchDistance = Vector2.Distance(pos0, pos1);
                var currentCenter = (pos0 + pos1) * 0.5f;

                if (_wasTwoFingerActive)
                {
                    // ピンチズーム処理
                    var pinchDelta = currentPinchDistance - _previousPinchDistance;
                    HandlePinchZoom(pinchDelta);

                    // 2本指スワイプ（パン）処理
                    var panDelta = currentCenter - _previousTwoFingerCenter;
                    HandleTwoFingerPan(panDelta);
                }

                _previousPinchDistance = currentPinchDistance;
                _previousTwoFingerCenter = currentCenter;
                _wasTwoFingerActive = true;
            }
            else
            {
                _wasTwoFingerActive = false;
            }
        }

        void HandlePinchZoom(float pinchDelta)
        {
            var lens = _cinemachineCamera.Lens;
            // ピンチイン（指を近づける）でズームイン（OrthographicSizeを小さく）
            var newSize = lens.OrthographicSize - pinchDelta * ZoomSpeed;
            lens.OrthographicSize = Mathf.Clamp(newSize, MinOrthographicSize, MaxOrthographicSize);
            _cinemachineCamera.Lens = lens;
        }

        void HandleTwoFingerPan(Vector2 panDelta)
        {
            // スクリーン座標のパンをワールド座標に変換
            // カメラが上から見下ろしている前提で、X/Yの移動をワールドのX/Yに対応
            var worldPanDelta = new Vector3(
                -panDelta.x * PanSpeed * _cinemachineCamera.Lens.OrthographicSize,
                -panDelta.y * PanSpeed * _cinemachineCamera.Lens.OrthographicSize,
                0f
            );

            _cinemachineCamera.transform.position += worldPanDelta;
        }
    }
}
