using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Timer.View
{
    /// タイマー完了時に画面を覆うトランジションパネル群。
    /// 各パネルは指定した画面外の角から時間差でスライドインし、
    /// 最後に重なる 1 枚の上では淡い星模様がゆっくり流れ続ける。
    public class CompleteTransitionView : MonoBehaviour
    {
        [SerializeField] RectTransform _canvasRect;
        [SerializeField] SlidePanel[] _panels = Array.Empty<SlidePanel>();
        [SerializeField, Min(0.01f)] float _slideDuration = 0.55f;
        [SerializeField, Min(0f)] float _slideInterval = 0.3f;
        [SerializeField] RawImage _starLayer;
        /// 星模様のスクロール速度（UV/秒）。画面を邪魔しないよう十分に遅く保つ
        [SerializeField] Vector2 _starScrollSpeed = new Vector2(0.03f, 0.02f);
        /// 星模様 1 タイルの表示サイズ（Canvas ピクセル）。解像度が変わっても密度を保つ
        [SerializeField, Min(1f)] float _starTilePixelSize = 360f;

        static readonly Vector2 FallbackCanvasSize = new Vector2(1080f, 1920f);

        bool _isStarScrolling;

        void Awake()
        {
            ApplyStarTiling();
            MoveAllOffScreen();
        }

        void Update()
        {
            if (!_isStarScrolling || _starLayer == null) return;

            var uvRect = _starLayer.uvRect;
            uvRect.position += _starScrollSpeed * Time.deltaTime;
            // 長時間の加算で float 精度が落ちるため、1 タイル進むごとに巻き戻す
            uvRect.x -= Mathf.Floor(uvRect.x);
            uvRect.y -= Mathf.Floor(uvRect.y);
            _starLayer.uvRect = uvRect;
        }

        /// 全パネルを時間差でスライドインさせ、画面が覆われるまで待つ
        public async UniTask PlayAsync(CancellationToken cancellationToken)
        {
            // 演出開始まで非アクティブにしてあるため、ここで起こす。
            // Canvas のサイズが確定した状態で Awake が走るので、画面外へ確実に逃がせる
            gameObject.SetActive(true);
            _isStarScrolling = true;

            var slides = new UniTask[_panels.Length];
            for (int i = 0; i < _panels.Length; i++)
            {
                slides[i] = SlideInAsync(_panels[i], _slideInterval * i, cancellationToken);
            }
            await UniTask.WhenAll(slides);
        }

        async UniTask SlideInAsync(SlidePanel panel, float delay, CancellationToken cancellationToken)
        {
            if (panel == null || panel.Rect == null) return;

            var rect = panel.Rect;
            var from = OffScreenPosition(panel);
            rect.anchoredPosition = from;

            if (delay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            }

            var elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _slideDuration);
                rect.anchoredPosition =
                    Vector2.LerpUnclamped(from, Vector2.zero, CompleteEase.OutCubic(t));
            }

            rect.anchoredPosition = Vector2.zero;
        }

        void MoveAllOffScreen()
        {
            foreach (var panel in _panels)
            {
                if (panel == null || panel.Rect == null) continue;
                panel.Rect.anchoredPosition = OffScreenPosition(panel);
            }
        }

        // パネルは画面と同サイズのため、画面幅・高さぶん動かせば完全に外へ出る
        Vector2 OffScreenPosition(SlidePanel panel)
        {
            var size = CanvasSize();
            var direction = panel.FromDirection;
            return new Vector2(direction.x * size.x, direction.y * size.y);
        }

        void ApplyStarTiling()
        {
            if (_starLayer == null) return;

            var size = CanvasSize();
            var uvRect = _starLayer.uvRect;
            uvRect.width = size.x / _starTilePixelSize;
            uvRect.height = size.y / _starTilePixelSize;
            _starLayer.uvRect = uvRect;
        }

        Vector2 CanvasSize()
        {
            if (_canvasRect == null) return FallbackCanvasSize;

            var size = _canvasRect.rect.size;
            if (size.x <= 0f || size.y <= 0f) return FallbackCanvasSize;
            return size;
        }

        [Serializable]
        class SlidePanel
        {
            [SerializeField] RectTransform _rect;
            /// 画面外の開始位置。(1,-1) なら右下、(-1,1) なら左上から入ってくる
            [SerializeField] Vector2 _fromDirection = new Vector2(1f, -1f);

            public RectTransform Rect => _rect;
            public Vector2 FromDirection => _fromDirection;
        }
    }
}
