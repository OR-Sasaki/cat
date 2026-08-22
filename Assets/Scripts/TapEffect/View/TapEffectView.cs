#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace TapEffect.View
{
    /// 画面のどこを押しても、指先で波紋 (リング) が広がり数個の粒が弾ける演出
    /// RootScope.prefab にネストした DontDestroyOnLoad の Canvas (TapEffectCanvas) に置くので全シーンで動く
    /// Canvas の sortingOrder はプレハブ側で 30000 にして、フェード (999) やダイアログ (1000+) より常に手前に描く
    /// GraphicRaycaster を持たず各 Image も raycastTarget = false のため、
    /// 入力は見ているだけで消費せず、ボタン等の UI 操作を一切妨げない
    public class TapEffectView : MonoBehaviour
    {
        [SerializeField] Canvas? _canvas;
        /// 演出を生成する親。Canvas 全面に張ったピボット中央の RectTransform
        [SerializeField] RectTransform? _container;
        [SerializeField] Sprite? _ringSprite;
        [SerializeField] Sprite? _dotSprite;

        [Header("Ring")]
        [SerializeField, Min(0.01f)] float _ringDuration = 0.36f;
        /// リングの直径 (Canvas px)。x = 押した瞬間、y = 消える直前
        [SerializeField] Vector2 _ringSizeRange = new(56f, 176f);
        [SerializeField] Color _ringColor = new(1f, 0.55f, 0.65f, 0.95f);

        [Header("Dots")]
        [SerializeField, Min(0)] int _dotCount = 6;
        [SerializeField, Min(0.01f)] float _dotDuration = 0.45f;
        /// 粒の直径 (Canvas px) の範囲。粒ごとにこの範囲からランダムに取る
        [SerializeField] Vector2 _dotSizeRange = new(14f, 24f);
        /// 粒が最終的に中心から離れる距離 (Canvas px) の範囲
        [SerializeField] Vector2 _dotDistanceRange = new(80f, 120f);
        /// 粒は等間隔の角度に並べた上で、この幅 (度) だけランダムにずらす
        [SerializeField, Range(0f, 180f)] float _dotAngleJitter = 18f;
        /// リングと同じピンクは壁の上で沈むので、粒はミント / 黄 / 空色 / 薄紫 / 白で散らす
        [SerializeField] Color[] _dotColors =
        {
            new Color(0.62f, 0.90f, 0.72f),
            new Color(1f, 0.85f, 0.42f),
            new Color(0.55f, 0.80f, 0.98f),
            new Color(0.75f, 0.66f, 0.96f),
            Color.white,
        };

        /// 使い回すバースト (リング 1 枚 + 粒 N 個) のプール。非アクティブなものが空き
        readonly List<Burst> _bursts = new();

        InputAction? _pressAction;

        void Awake()
        {
            _canvas ??= GetComponentInParent<Canvas>();
            _container ??= transform as RectTransform;
        }

        void OnEnable()
        {
            // 押した瞬間だけ拾いたいが、wasPressedThisFrame のポーリングだと同じフレーム内で
            // 押して離した短いタップを取りこぼす。Action の変化通知なら押下イベントごとに呼ばれる
            // PassThrough にして、複数の指が同時に触れてもそれぞれの押下を独立に受け取る
            _pressAction = new InputAction("TapEffectPress", InputActionType.PassThrough);
            _pressAction.AddBinding("<Touchscreen>/touch*/press");
            _pressAction.AddBinding("<Mouse>/leftButton");
            _pressAction.AddBinding("<Pen>/tip");
            _pressAction.performed += OnPressChanged;
            _pressAction.Enable();
        }

        void OnDisable()
        {
            if (_pressAction == null)
            {
                return;
            }

            _pressAction.performed -= OnPressChanged;
            _pressAction.Disable();
            _pressAction.Dispose();
            _pressAction = null;
        }

        void Update()
        {
            // ダイアログの開閉と同じく Time.timeScale の影響を受けないようにする
            Advance(Time.unscaledDeltaTime);
        }

        /// 任意のスクリーン座標で演出を再生する。入力以外のきっかけで鳴らしたいときにも使える
        public void Play(Vector2 screenPosition)
        {
            if (_container == null)
            {
                return;
            }

            // Overlay の Canvas はカメラなしで変換する
            var camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _container, screenPosition, camera, out var localPoint))
            {
                return;
            }

            Begin(Rent(), localPoint);
        }

        void OnPressChanged(InputAction.CallbackContext context)
        {
            // PassThrough は離したときも呼ばれるので、押した側だけ通す
            if (!context.ReadValueAsButton())
            {
                return;
            }

            // タッチは指ごとの位置、マウス / ペンはそのデバイスのポインター位置を使う
            if (context.control.parent is TouchControl touch)
            {
                Play(touch.position.ReadValue());
            }
            else if (context.control.device is Pointer pointer)
            {
                Play(pointer.position.ReadValue());
            }
        }

        /// 1 タップ = リング 1 枚 + 粒 N 個を 1 本の経過時間で動かすため、粒ごとに Tween を張るより
        /// Update で進める方が単純で GC も出ない。イージングの計算だけ DOTween に任せる
        void Advance(float deltaTime)
        {
            var duration = Mathf.Max(_ringDuration, _dotDuration);
            foreach (var burst in _bursts)
            {
                if (!burst.Root.gameObject.activeSelf)
                {
                    continue;
                }

                burst.Age += deltaTime;
                if (burst.Age >= duration)
                {
                    burst.Root.gameObject.SetActive(false);
                    continue;
                }

                Apply(burst);
            }
        }

        void Begin(Burst burst, Vector2 localPoint)
        {
            burst.Age = 0f;
            burst.Root.anchoredPosition = localPoint;
            // 後から押した方を手前に描く
            burst.Root.SetAsLastSibling();

            // 毎回同じ形にならないよう、粒の並び全体をランダムに回してから角度をばらす
            var baseAngle = Random.Range(0f, 360f);
            var step = burst.Dots.Length > 0 ? 360f / burst.Dots.Length : 0f;
            for (var i = 0; i < burst.Dots.Length; i++)
            {
                var dot = burst.Dots[i];
                var angle = (baseAngle + step * i + Random.Range(-_dotAngleJitter, _dotAngleJitter)) * Mathf.Deg2Rad;
                dot.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                dot.Distance = Random.Range(_dotDistanceRange.x, _dotDistanceRange.y);
                dot.Size = Random.Range(_dotSizeRange.x, _dotSizeRange.y);
                dot.Color = _dotColors.Length > 0 ? _dotColors[Random.Range(0, _dotColors.Length)] : Color.white;
            }

            // 前回の見た目が 1 フレーム映らないよう、有効化する前に開始時点の見た目を反映する
            Apply(burst);
            burst.Root.gameObject.SetActive(true);
        }

        void Apply(Burst burst)
        {
            ApplyRing(burst);
            ApplyDots(burst);
        }

        void ApplyRing(Burst burst)
        {
            var t = Mathf.Clamp01(burst.Age / _ringDuration);
            // 勢いよく広がって減速しながら、終盤にすっと消える
            var size = DOVirtual.EasedValue(_ringSizeRange.x, _ringSizeRange.y, t, Ease.OutCubic);
            burst.RingRect.sizeDelta = new Vector2(size, size);

            var color = _ringColor;
            color.a = DOVirtual.EasedValue(_ringColor.a, 0f, t, Ease.InQuad);
            burst.Ring.color = color;
        }

        void ApplyDots(Burst burst)
        {
            var t = Mathf.Clamp01(burst.Age / _dotDuration);
            var travel = DOVirtual.EasedValue(0f, 1f, t, Ease.OutCubic);
            var shrink = DOVirtual.EasedValue(1f, 0f, t, Ease.InQuad);
            var alpha = DOVirtual.EasedValue(1f, 0f, t, Ease.InQuad);
            // 粒はリングの初期半径の位置から飛び出す
            var startDistance = _ringSizeRange.x * 0.5f;

            foreach (var dot in burst.Dots)
            {
                var distance = Mathf.Lerp(startDistance, dot.Distance, travel);
                dot.Rect.anchoredPosition = dot.Direction * distance;

                var size = dot.Size * shrink;
                dot.Rect.sizeDelta = new Vector2(size, size);

                var color = dot.Color;
                color.a *= alpha;
                dot.Image.color = color;
            }
        }

        Burst Rent()
        {
            foreach (var burst in _bursts)
            {
                if (!burst.Root.gameObject.activeSelf)
                {
                    return burst;
                }
            }

            var created = CreateBurst();
            _bursts.Add(created);
            return created;
        }

        Burst CreateBurst()
        {
            var root = new GameObject("Burst", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(_container, false);
            SetCentered(rootRect);

            var ring = CreateImage("Ring", rootRect, _ringSprite);
            var dots = new Dot[_dotCount];
            for (var i = 0; i < dots.Length; i++)
            {
                dots[i] = new Dot(CreateImage($"Dot{i}", rootRect, _dotSprite));
            }

            root.SetActive(false);
            return new Burst(rootRect, ring, dots);
        }

        static Image CreateImage(string name, RectTransform parent, Sprite? sprite)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            SetCentered(rect);

            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            // 演出は入力を奪わない
            image.raycastTarget = false;
            return image;
        }

        static void SetCentered(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        class Burst
        {
            public readonly RectTransform Root;
            public readonly Image Ring;
            public readonly RectTransform RingRect;
            public readonly Dot[] Dots;
            public float Age;

            public Burst(RectTransform root, Image ring, Dot[] dots)
            {
                Root = root;
                Ring = ring;
                RingRect = ring.rectTransform;
                Dots = dots;
            }
        }

        class Dot
        {
            public readonly Image Image;
            public readonly RectTransform Rect;
            public Vector2 Direction;
            public float Distance;
            public float Size;
            public Color Color;

            public Dot(Image image)
            {
                Image = image;
                Rect = image.rectTransform;
            }
        }
    }
}
