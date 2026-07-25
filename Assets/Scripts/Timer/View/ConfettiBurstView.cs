using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Timer.View
{
    /// クラッカーのように画面左右の下端から色とりどりの紙吹雪を打ち上げる演出。
    /// 紙片はスプライトを持たない Image をプールして使い回すため、
    /// 専用のテクスチャもパーティクルシステムも要らない。
    public class ConfettiBurstView : MonoBehaviour
    {
        [SerializeField] RectTransform _container;
        [SerializeField, Min(1)] int _piecesPerSide = 26;
        /// 打ち上げ回数。少し間を空けて重ねると本物のクラッカーらしい散り方になる
        [SerializeField, Min(1)] int _burstCount = 2;
        [SerializeField, Min(0f)] float _burstInterval = 0.16f;
        [SerializeField] Vector2 _leftOriginViewport = new Vector2(0.02f, 0.06f);
        [SerializeField] Vector2 _rightOriginViewport = new Vector2(0.98f, 0.06f);
        /// 打ち上げ角度の範囲（度）。左側の値で、右側は左右反転して使う
        [SerializeField] Vector2 _launchAngleRange = new Vector2(48f, 84f);
        /// 初速の範囲（Canvas ピクセル/秒）。画面高さの半分〜全部まで上がる程度に取る
        [SerializeField] Vector2 _launchSpeedRange = new Vector2(2800f, 3800f);
        [SerializeField] Vector2 _pieceWidthRange = new Vector2(20f, 40f);
        [SerializeField] Vector2 _pieceAspectRange = new Vector2(0.5f, 1.3f);
        [SerializeField] float _gravity = 2800f;
        /// 空気抵抗。1 秒あたりに失う速度の割合
        [SerializeField, Range(0f, 4f)] float _drag = 0.6f;
        [SerializeField] Vector2 _spinRange = new Vector2(-540f, 540f);
        /// ひらひら反転する速さ（回転/秒）の範囲
        [SerializeField] Vector2 _flutterRange = new Vector2(1.5f, 4f);
        [SerializeField] Vector2 _lifetimeRange = new Vector2(2.6f, 3.8f);
        [SerializeField, Min(0.01f)] float _fadeOutSeconds = 0.8f;
        [SerializeField] Color[] _colors =
        {
            new Color(0.98f, 0.42f, 0.45f),
            new Color(1f, 0.76f, 0.29f),
            new Color(0.99f, 0.93f, 0.45f),
            new Color(0.58f, 0.84f, 0.42f),
            new Color(0.38f, 0.75f, 0.93f),
            new Color(0.65f, 0.58f, 0.93f),
            new Color(1f, 0.62f, 0.80f),
        };

        static readonly Vector2 FallbackContainerSize = new Vector2(1080f, 1920f);

        readonly List<Piece> _pieces = new List<Piece>();
        int _pendingBursts;
        float _nextBurstTimer;

        /// クラッカーを打ち上げる
        public void Burst()
        {
            _pendingBursts = _burstCount;
            _nextBurstTimer = 0f;
        }

        void Awake()
        {
            if (_container == null) _container = transform as RectTransform;
        }

        void Update()
        {
            if (_container == null) return;

            var deltaTime = Time.deltaTime;
            UpdatePendingBursts(deltaTime);
            UpdatePieces(deltaTime);
        }

        void UpdatePendingBursts(float deltaTime)
        {
            if (_pendingBursts <= 0) return;

            _nextBurstTimer -= deltaTime;
            if (_nextBurstTimer > 0f) return;

            SpawnWave();
            _pendingBursts--;
            _nextBurstTimer = _burstInterval;
        }

        void SpawnWave()
        {
            for (int i = 0; i < _piecesPerSide; i++)
            {
                Launch(_leftOriginViewport, false);
                Launch(_rightOriginViewport, true);
            }
        }

        void Launch(Vector2 originViewport, bool towardLeft)
        {
            var piece = RentPiece();
            var angle = Random.Range(_launchAngleRange.x, _launchAngleRange.y) * Mathf.Deg2Rad;
            var speed = Random.Range(_launchSpeedRange.x, _launchSpeedRange.y);
            var horizontal = Mathf.Cos(angle) * (towardLeft ? -1f : 1f);

            piece.Velocity = new Vector2(horizontal, Mathf.Sin(angle)) * speed;
            piece.Position = ViewportToLocal(originViewport);
            piece.Rotation = Random.Range(0f, 360f);
            piece.Spin = Random.Range(_spinRange.x, _spinRange.y);
            piece.Flutter = Random.Range(_flutterRange.x, _flutterRange.y);
            piece.FlutterPhase = Random.Range(0f, Mathf.PI * 2f);
            piece.Life = Random.Range(_lifetimeRange.x, _lifetimeRange.y);
            piece.Age = 0f;

            var width = Random.Range(_pieceWidthRange.x, _pieceWidthRange.y);
            var height = width * Random.Range(_pieceAspectRange.x, _pieceAspectRange.y);
            piece.Rect.sizeDelta = new Vector2(width, height);

            piece.Color = _colors.Length > 0 ? _colors[Random.Range(0, _colors.Length)] : Color.white;
            piece.Image.color = piece.Color;

            piece.Rect.anchoredPosition = piece.Position;
            piece.Rect.gameObject.SetActive(true);
        }

        void UpdatePieces(float deltaTime)
        {
            var bottom = -_container.pivot.y * ContainerSize().y;

            foreach (var piece in _pieces)
            {
                if (!piece.Rect.gameObject.activeSelf) continue;

                piece.Age += deltaTime;
                if (piece.Age >= piece.Life)
                {
                    piece.Rect.gameObject.SetActive(false);
                    continue;
                }

                piece.Velocity.y -= _gravity * deltaTime;
                piece.Velocity -= piece.Velocity * Mathf.Min(1f, _drag * deltaTime);
                piece.Position += piece.Velocity * deltaTime;

                // 画面下に抜けきった紙片は寿命を待たずに回収する
                if (piece.Position.y < bottom - piece.Rect.sizeDelta.y)
                {
                    piece.Rect.gameObject.SetActive(false);
                    continue;
                }

                piece.Rotation += piece.Spin * deltaTime;
                piece.Rect.anchoredPosition = piece.Position;
                piece.Rect.localRotation = Quaternion.Euler(0f, 0f, piece.Rotation);

                // 横幅を周期的に潰すことで、紙が裏返りながら舞う様子を安価に表現する
                var flutter = Mathf.Cos(piece.FlutterPhase + piece.Age * piece.Flutter * Mathf.PI * 2f);
                piece.Rect.localScale = new Vector3(flutter, 1f, 1f);

                var remaining = piece.Life - piece.Age;
                var color = piece.Color;
                color.a = _fadeOutSeconds > 0f ? Mathf.Clamp01(remaining / _fadeOutSeconds) : 1f;
                piece.Image.color = color;
            }
        }

        Piece RentPiece()
        {
            foreach (var piece in _pieces)
            {
                if (!piece.Rect.gameObject.activeSelf) return piece;
            }

            var created = CreatePiece();
            _pieces.Add(created);
            return created;
        }

        Piece CreatePiece()
        {
            var gameObject = new GameObject("ConfettiPiece", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(_container, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = gameObject.GetComponent<Image>();
            image.raycastTarget = false;

            gameObject.SetActive(false);
            return new Piece { Rect = rect, Image = image };
        }

        Vector2 ViewportToLocal(Vector2 viewport)
        {
            var size = ContainerSize();
            return new Vector2(
                (viewport.x - _container.pivot.x) * size.x,
                (viewport.y - _container.pivot.y) * size.y);
        }

        Vector2 ContainerSize()
        {
            if (_container == null) return FallbackContainerSize;

            var size = _container.rect.size;
            if (size.x <= 0f || size.y <= 0f) return FallbackContainerSize;
            return size;
        }

        class Piece
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public Color Color;
            public float Rotation;
            public float Spin;
            public float Flutter;
            public float FlutterPhase;
            public float Age;
            public float Life;
        }
    }
}
