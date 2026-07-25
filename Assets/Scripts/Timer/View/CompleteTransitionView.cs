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
    /// パネルは進行方向と垂直な辺が先頭になるよう傾けてあるので、
    /// 画面に入ってくるのは斜めの辺だけで、四角形の角は見えない。
    public class CompleteTransitionView : MonoBehaviour
    {
        [SerializeField] RectTransform _canvasRect;
        [SerializeField] SlidePanel[] _panels = Array.Empty<SlidePanel>();
        [SerializeField, Min(0.01f)] float _slideDuration = 0.55f;
        [SerializeField, Min(0f)] float _slideInterval = 0.3f;
        /// パネルの角を画面端から離しておく距離（Canvas ピクセル）。
        /// 覆うのに必要な大きさへ上下左右この分だけ足すので、
        /// 角と、回転の丸め誤差による画面端の隙間がどちらも表に出ない
        [SerializeField, Min(0f)] float _cornerClearance = 48f;
        [SerializeField] RawImage _starLayer;
        /// 星模様のスクロール速度（UV/秒）。画面を邪魔しないよう十分に遅く保つ
        [SerializeField] Vector2 _starScrollSpeed = new Vector2(0.03f, 0.02f);
        /// 星模様 1 タイルの表示サイズ（Canvas ピクセル）。解像度が変わっても密度を保つ
        [SerializeField, Min(1f)] float _starTilePixelSize = 360f;

        static readonly Vector2 FallbackCanvasSize = new Vector2(1080f, 1920f);
        static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

        bool _isStarScrolling;

        void Awake()
        {
            // PlayAsync 以外の経路で起こされても画面を覆ったままにならないよう、ここでも組む
            BuildLayout();
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
            // 演出開始まで非アクティブにしてあるため、ここで起こす
            gameObject.SetActive(true);
            // Canvas の rect は一度描画されるまでシーンに保存された値のままなので、
            // 起動直後に走る Awake の結果は画面比率とずれている場合がある。
            // 傾きと大きさはこの rect から決めるため、演出開始時に組み直す
            BuildLayout();
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

        /// パネルを傾けて大きさを決め、画面外の待機位置まで下げる。
        /// 星模様はパネルの子で一緒に広がるため、パネルを整えたあとにタイリングを決める
        void BuildLayout()
        {
            ApplyPanelTilt();
            ApplyStarTiling();

            foreach (var panel in _panels)
            {
                if (panel == null || panel.Rect == null) continue;
                panel.Rect.anchoredPosition = OffScreenPosition(panel);
            }
        }

        // 進行方向の逆側へ、先頭の辺が画面外へ抜けきる距離だけ下げた位置
        Vector2 OffScreenPosition(SlidePanel panel)
        {
            var screen = CanvasSize();
            var travel = TravelDirection(panel, screen);
            var halfPanel = panel.Rect.rect.height * 0.5f;
            var halfScreen = CoverSize(screen, travel).y * 0.5f;
            return -travel * (halfPanel + halfScreen);
        }

        /// 各パネルを進行方向と垂直な辺が先頭になるよう傾け、
        /// 傾けたまま画面を覆いきる大きさへ広げる。
        /// 先頭の辺は画面を斜めに横切れる長さになるので、角は必ず画面外を通る
        void ApplyPanelTilt()
        {
            var screen = CanvasSize();
            foreach (var panel in _panels)
            {
                if (panel == null || panel.Rect == null) continue;

                var rect = panel.Rect;
                var travel = TravelDirection(panel, screen);
                // ローカルの上方向を進行方向に合わせる = 上辺が先頭の斜め辺になる
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg - 90f);
                // 画面いっぱいに伸ばすアンカーだと大きさが画面比率に引きずられるので、
                // 中央アンカーに寄せて計算した大きさをそのまま持たせる
                rect.anchorMin = CenterAnchor;
                rect.anchorMax = CenterAnchor;
                rect.sizeDelta = CoverSize(screen, travel) + Vector2.one * (_cornerClearance * 2f);
            }
        }

        /// パネルが画面外から画面中央へ向かう向き（Canvas ピクセル空間の単位ベクトル）
        Vector2 TravelDirection(SlidePanel panel, Vector2 screen)
        {
            var direction = panel.FromDirection;
            var from = new Vector2(direction.x * screen.x, direction.y * screen.y);
            if (from.sqrMagnitude <= 0f) return Vector2.up;
            return -from.normalized;
        }

        /// 進行方向へ傾けたパネルが画面を覆うのに必要なサイズ。
        /// 横（先頭の辺の長さ）と縦（進行方向の長さ）をそれぞれ画面の射影から求める
        static Vector2 CoverSize(Vector2 screen, Vector2 travel)
        {
            var alongTravel = screen.x * Mathf.Abs(travel.x) + screen.y * Mathf.Abs(travel.y);
            var acrossTravel = screen.x * Mathf.Abs(travel.y) + screen.y * Mathf.Abs(travel.x);
            return new Vector2(acrossTravel, alongTravel);
        }

        void ApplyStarTiling()
        {
            if (_starLayer == null) return;

            // パネルを広げたぶん星模様の領域も広がるので、自身の大きさから密度を決める
            var size = _starLayer.rectTransform.rect.size;
            if (size.x <= 0f || size.y <= 0f) size = CanvasSize();

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
            /// 画面外の開始位置。(1,-1) なら右下、(-1,1) なら左上から入ってくる。
            /// パネルの傾きもこの向き（進行方向と垂直な辺が先頭）から決まる
            [SerializeField] Vector2 _fromDirection = new Vector2(1f, -1f);

            public RectTransform Rect => _rect;
            public Vector2 FromDirection => _fromDirection;
        }
    }
}
