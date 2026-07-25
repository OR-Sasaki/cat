using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Timer.View
{
    /// タイマー完了時、走っていたキャラクターをいったん画面下へ引っ込め、
    /// 大きくズームした状態で顔だけを画面下から飛び出させる演出。
    public class CompleteCharacterPopView : MonoBehaviour
    {
        [SerializeField] SortingGroup _sortingGroup;
        [SerializeField] Camera _camera;
        [SerializeField] Animator _animator;
        /// リザルト表示で静止させるステート名
        [SerializeField] string _idleStateName = "Idle";
        /// 原点から頭頂までの高さ（キャラクターのローカル単位）。
        /// パーツのスプライトは余白の多い正方形なので Renderer.bounds では顔の位置が取れず、
        /// 待機ポーズで静止させた状態の実測値をここに持たせて拡大率に比例させる
        [SerializeField] float _headTopLocalY = 8f;
        /// 飛び出す際の拡大率（通常表示のスケールに対する倍率）
        [SerializeField, Min(1f)] float _popScaleMultiplier = 4.2f;
        /// 飛び出した後の描画順。トランジションパネルより手前、完了 UI より奥に置く
        [SerializeField] int _popSortingOrder = 8;
        /// 飛び出し切ったときの頭頂の高さ（ビューポート座標）
        [SerializeField, Range(0f, 1f)] float _headTopViewportY = 0.4f;
        /// 飛び出す位置のワールド X。0 で画面中央
        [SerializeField] float _popCenterX;
        [SerializeField, Min(0.01f)] float _duckDuration = 0.32f;
        [SerializeField, Min(0f)] float _duckOvershoot = 1.2f;
        [SerializeField, Min(0.01f)] float _popDuration = 0.62f;
        [SerializeField, Min(0f)] float _popOvershoot = 1.35f;

        /// 画面外へ隠すときの頭頂の高さ。下端に張り付かないよう少しだけ外へ出す
        const float HiddenViewportY = -0.02f;

        Vector3 _baseScale = Vector3.one;
        int _idleStateHash;

        void Awake()
        {
            _baseScale = transform.localScale;
            _idleStateHash = Animator.StringToHash(_idleStateName);
            if (_camera == null) _camera = Camera.main;
        }

        /// 走行中の見た目のまま画面下へ滑り落ちて退場する
        public async UniTask DuckOutAsync(CancellationToken cancellationToken)
        {
            var from = transform.localPosition.y;
            var to = SolveLocalYForHeadTop(HiddenViewportY);
            if (to >= from) return;

            var elapsed = 0f;
            while (elapsed < _duckDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _duckDuration);
                SetLocalY(Mathf.LerpUnclamped(from, to, CompleteEase.InBack(t, _duckOvershoot)));
            }

            SetLocalY(to);
        }

        /// 拡大した状態で画面下から勢いよく飛び出す
        public async UniTask PopAsync(CancellationToken cancellationToken)
        {
            ApplyPopPose();

            // 拡大後の頭頂位置で計算するため、必ずポーズ確定後に求める
            var from = SolveLocalYForHeadTop(HiddenViewportY);
            var to = SolveLocalYForHeadTop(_headTopViewportY);
            SetLocalY(from);

            var elapsed = 0f;
            while (elapsed < _popDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _popDuration);
                SetLocalY(Mathf.LerpUnclamped(from, to, CompleteEase.OutBack(t, _popOvershoot)));
            }

            SetLocalY(to);
        }

        void ApplyPopPose()
        {
            if (_sortingGroup != null)
            {
                _sortingGroup.sortingOrder = _popSortingOrder;
            }

            StopAnimation();

            transform.localScale = _baseScale * _popScaleMultiplier;

            var position = transform.localPosition;
            position.x = _popCenterX;
            transform.localPosition = position;
        }

        // リザルト表示中はキャラクターを静止させる。
        // その場で止めると走行の途中姿勢で固まり顔の位置も毎回変わるため、
        // 待機ポーズの先頭へ移してから停止して、_headTopLocalY の前提を一定に保つ
        void StopAnimation()
        {
            if (_animator == null) return;

            _animator.Play(_idleStateHash, 0, 0f);
            _animator.speed = 0f;
        }

        void SetLocalY(float y)
        {
            var position = transform.localPosition;
            position.y = y;
            transform.localPosition = position;
        }

        // 頭頂がビューポート上の viewportY の高さに来る localPosition.y を返す
        float SolveLocalYForHeadTop(float viewportY)
        {
            return ViewportToWorldY(viewportY) - _headTopLocalY * transform.localScale.y;
        }

        float ViewportToWorldY(float viewportY)
        {
            if (_camera == null) return transform.localPosition.y;

            // キャラクターは z=0 平面上に居るため、カメラからの距離をそのまま渡す
            var distance = Mathf.Abs(_camera.transform.position.z);
            return _camera.ViewportToWorldPoint(new Vector3(0.5f, viewportY, distance)).y;
        }
    }
}
