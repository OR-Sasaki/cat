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

        // エッジパン設定（家具ドラッグ中に画面際までポインターを寄せるとゆっくりスクロールする）
        const float EdgeThresholdRatio = 0.1f; // 画面の短辺に対するエッジ判定領域の割合
        const float EdgePanSpeed = 0.8f;       // スクロール速度（OrthographicSizeを掛けた値がワールド速度/秒）

        // タッチ状態
        float _previousPinchDistance;
        Vector2 _previousTwoFingerCenter;
        bool _wasTwoFingerActive;

        // エッジパン状態（-1〜1のスクロール方向・強度。Vector2.zeroなら停止）
        Vector2 _edgePanIntensity;

        // カメラ移動
        Vector3? _targetPosition;
        Vector3 _velocity;
        const float SmoothTime = 0.1f;

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
            _edgePanIntensity = Vector2.zero;
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
            _targetPosition =  null;
            _edgePanIntensity = Vector2.zero;
        }

        public void Tick()
        {
            if (!_isActive || _cinemachineCamera == null) return;

            // ターゲット位置への移動処理
            if (_targetPosition.HasValue)
            {
                var currentPos = _cinemachineCamera.transform.position;
                var newPos = Vector3.SmoothDamp(currentPos, _targetPosition.Value, ref _velocity, SmoothTime);
                _cinemachineCamera.transform.position = newPos;

                // 十分近づいたら完了
                if (Vector3.Distance(newPos, _targetPosition.Value) < 0.01f)
                {
                    _cinemachineCamera.transform.position = _targetPosition.Value;
                    _targetPosition = null;
                }
            }

            var touchscreen = Touchscreen.current;

            var touch0Active = touchscreen != null && touchscreen.touches[0].press.isPressed;
            var touch1Active = touchscreen != null && touchscreen.touches[1].press.isPressed;
            var isTwoFingerActive = touch0Active && touch1Active;

            // 家具ドラッグ中の画面際スクロール（2本指のピンチ／パン中は無効化して競合を避ける）
            if (!isTwoFingerActive)
            {
                ApplyEdgePan();
            }

            if (touchscreen == null) return;

            // 2本指が両方アクティブな場合のみ処理
            if (isTwoFingerActive)
            {
                var pos0 = touchscreen.touches[0].position.ReadValue();
                var pos1 = touchscreen.touches[1].position.ReadValue();

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

        /// 家具ドラッグ中のポインター位置を受け取り、画面際の自動スクロール方向を更新する
        public void OnFurnitureDragMove(Vector2 pointerScreenPosition)
        {
            _edgePanIntensity = CalculateEdgePanIntensity(pointerScreenPosition);
        }

        /// 家具ドラッグ終了時に画面際の自動スクロールを停止する
        public void OnFurnitureDragEnd()
        {
            _edgePanIntensity = Vector2.zero;
        }

        /// エッジパン強度に応じてカメラをゆっくりスクロールさせる
        void ApplyEdgePan()
        {
            if (_cinemachineCamera == null) return;
            if (_edgePanIntensity == Vector2.zero) return;

            // ズーム倍率に応じて速度を調整し、見た目のスクロール速度を一定に近づける
            var orthographicSize = _cinemachineCamera.Lens.OrthographicSize;
            var worldDelta = new Vector3(_edgePanIntensity.x, _edgePanIntensity.y, 0f)
                             * (EdgePanSpeed * orthographicSize * Time.deltaTime);

            _cinemachineCamera.transform.position += worldDelta;
        }

        /// ポインターのスクリーン座標から画面際の自動スクロール強度を算出する
        /// 画面際に近いほど絶対値が大きくなり、範囲は各軸 -1〜1（画面外は最大強度で頭打ち）
        Vector2 CalculateEdgePanIntensity(Vector2 screenPos)
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0) return Vector2.zero;

            // 画面の短辺を基準にエッジ判定領域の幅を決める
            var edgeThreshold = Mathf.Min(screenWidth, screenHeight) * EdgeThresholdRatio;
            if (edgeThreshold <= 0f) return Vector2.zero;

            var intensity = Vector2.zero;

            // 左右の画面際
            if (screenPos.x < edgeThreshold)
            {
                intensity.x = -(edgeThreshold - screenPos.x) / edgeThreshold;
            }
            else if (screenPos.x > screenWidth - edgeThreshold)
            {
                intensity.x = (screenPos.x - (screenWidth - edgeThreshold)) / edgeThreshold;
            }

            // 上下の画面際
            if (screenPos.y < edgeThreshold)
            {
                intensity.y = -(edgeThreshold - screenPos.y) / edgeThreshold;
            }
            else if (screenPos.y > screenHeight - edgeThreshold)
            {
                intensity.y = (screenPos.y - (screenHeight - edgeThreshold)) / edgeThreshold;
            }

            // 画面外までポインターが出ても強度は最大1に制限する
            intensity.x = Mathf.Clamp(intensity.x, -1f, 1f);
            intensity.y = Mathf.Clamp(intensity.y, -1f, 1f);
            return intensity;
        }

        /// カメラを指定のワールド座標に滑らかに移動する
        public void MoveTo(Vector3 worldPosition)
        {
            if (_cinemachineCamera == null) return;

            var cameraPos = _cinemachineCamera.transform.position;
            _targetPosition = new Vector3(worldPosition.x, worldPosition.y, cameraPos.z);
            _velocity = Vector3.zero;
        }
    }
}
