#nullable enable
using System;
using System.Collections.Generic;
using Cat.Character;
using DG.Tweening;
using Home.OutfitEffectLogic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Home.View
{
    /// 着せ替え時のもくもく雲 + キラキラ + 肉球演出。スプライトはプール再利用し都度 Instantiate/Destroy しない
    public sealed class OutfitChangeEffectView : MonoBehaviour
    {
        [SerializeField] Sprite _cloudSprite = null!;
        [SerializeField] Sprite _glintSprite = null!;
        [SerializeField] Sprite _pawSprite = null!;

        /// 雲とキラキラで共有するパステル色
        [SerializeField, FormerlySerializedAs("_cloudColors")]
        Color[] _pastelColors =
        {
            new(1f, 0.75f, 0.85f),
            new(0.68f, 0.92f, 0.78f),
            new(1f, 0.90f, 0.55f),
            new(0.62f, 0.85f, 0.98f),
            new(0.82f, 0.72f, 0.96f),
            Color.white,
        };

        [SerializeField]
        Color[] _pawColors =
        {
            new(1f, 0.4f, 0.55f, 1f),
            new(0.25f, 0.85f, 0.45f, 1f),
            new(1f, 0.8f, 0.15f, 1f),
            new(0.25f, 0.65f, 0.95f, 1f),
            new(0.65f, 0.45f, 0.9f, 1f),
        };

        /// 家具の SortingGroup ((x+y)*1000+x) より確実に手前に描く
        [SerializeField] int _sortingOrder = 32000;

        const float CoverDuration = 0.14f;
        const float ClearStart = 0.16f;
        const float ClearDuration = 0.28f; // ClearStart + ClearDuration = 0.44s
        const float DriftRatio = 0.12f;

        /// Cloud スプライトの内接する不透明円の半径 (localScale=1 のとき)
        const float CloudInnerRadiusRatio = 0.36f;

        const int GlintCount = 5;
        const float GlintStagger = 0.025f;

        const int PawCount = 3;
        const float PawStart = 0.2f;
        const float PawStagger = 0.05f;
        static readonly float[] PawFactors = { 0.2f, 0.4f, 0.6f };

        const float FlowMoveDuration = 0.28f;
        const float JitterRange = 0.15f;
        const float RootLifetime = 0.6f;

        readonly List<SpriteRenderer> _cloudPool = new();
        readonly List<SpriteRenderer> _pawPool = new();
        readonly List<GlintUnit> _glintPool = new();

        /// 演出スプライトの親。キャラの移動差分だけ動かして、再生中に歩いても演出が置き去りにならないようにする
        Transform? _container;
        Vector3 _lastCharacterPosition;

        Sequence? _sequence;
        Transform? _character;
        Vector3 _characterOriginalScale;
        Action? _pendingSwapOutfit;

        sealed class GlintUnit
        {
            public Transform Root = null!;
            public Transform Core = null!;
            public SpriteRenderer Tinted = null!;
            public SpriteRenderer White = null!;
        }

        /// もくもく雲でキャラを覆い隠して着せ替え、キラキラと肉球を流しながら晴れる一連の演出を再生する
        public void Play(CharacterView character, Action swapOutfit)
        {
            var characterTransform = character.transform;

            // 再生中に呼び出された場合は前回分を即時確定してから中断する
            if (_sequence is not null && _sequence.IsActive())
            {
                _pendingSwapOutfit?.Invoke();
                _sequence.Kill();
                if (_character != null)
                {
                    _character.localScale = _characterOriginalScale;
                }

                ReturnAllToPool();
            }

            _character = characterTransform;
            _characterOriginalScale = characterTransform.localScale;
            _pendingSwapOutfit = swapOutfit;
            _container ??= CreateContainer();
            // 以降の座標はコンテナ原点基準 (= ワールド) で置くため、毎回原点へ戻す
            _container.localPosition = Vector3.zero;
            _lastCharacterPosition = characterTransform.position;

            var bounds = character.CalculateOutfitBounds();
            var h = bounds.size.y;
            var center = bounds.center;
            var extents = bounds.extents;
            var random = new Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence = sequence;

            var placements = CloudCoveragePlanner.Plan(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y, h, random);
            foreach (var placement in placements)
            {
                var position = new Vector3(placement.X, placement.Y, 0f);
                var offset = position - center;
                var driftDir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.up;
                var targetScale = placement.InnerRadius / CloudInnerRadiusRatio;

                var renderer = GetPooledCloud();
                renderer.sprite = _cloudSprite;
                renderer.color = RandomColor(_pastelColors);
                renderer.transform.localPosition = position;
                renderer.transform.localScale = Vector3.zero;

                sequence.Insert(0f, renderer.transform.DOScale(targetScale, CoverDuration).SetEase(Ease.OutQuad));
                sequence.Insert(ClearStart, renderer.transform.DOLocalMove(position + driftDir * DriftRatio * h, ClearDuration).SetEase(Ease.InQuad));
                sequence.Insert(ClearStart, renderer.DOFade(0f, ClearDuration));
            }

            // 完全に隠れているタイミングで着せ替える
            sequence.InsertCallback(CoverDuration, () =>
            {
                _pendingSwapOutfit?.Invoke();
                _pendingSwapOutfit = null;
            });

            // 晴れると同時にキャラがきゅっと縮んで戻るお披露目リアクション
            var originalScale = _characterOriginalScale;
            var squashed = new Vector3(originalScale.x * 1.06f, originalScale.y * 0.94f, originalScale.z);
            sequence.Insert(ClearStart, characterTransform.DOScale(originalScale, 0.15f).From(squashed, false));

            // 右上から左下へ流れる方向。キラキラと肉球はこの向き・速度を共有する
            var diagDir = new Vector3(-1f, -1f, 0f).normalized;
            var perpDir = new Vector3(1f, -1f, 0f).normalized;
            var upperRight = center + new Vector3(extents.x * 1.08f, extents.y * 1.08f, 0f);
            var lowerLeft = center - new Vector3(extents.x * 1.08f, extents.y * 1.08f, 0f);
            var flowMoveDistance = 0.35f * h;

            for (var i = 0; i < GlintCount; i++)
            {
                var delay = ClearStart + i * GlintStagger;
                var t = 0.05f + 0.5f * i / (GlintCount - 1);
                var spawn = FlowStart(upperRight, lowerLeft, t, perpDir, JitterRange * h);
                var target = spawn + diagDir * flowMoveDistance;
                var visualScale = 0.18f * h;

                var glint = GetPooledGlint();
                glint.Tinted.sprite = _glintSprite;
                glint.Tinted.color = RandomColor(_pastelColors);
                glint.White.sprite = _glintSprite;
                glint.White.color = Color.white;
                glint.Root.localPosition = spawn;
                glint.Core.localPosition = Vector3.zero;
                glint.Core.rotation = Quaternion.identity;
                glint.Tinted.transform.localScale = Vector3.one * visualScale;
                glint.White.transform.localScale = Vector3.one * visualScale * 0.55f;
                glint.Core.localScale = Vector3.zero;

                sequence.Insert(delay, glint.Root.DOLocalMove(target, FlowMoveDuration).SetEase(Ease.OutCubic));
                sequence.Insert(delay, glint.Core.DOScale(Vector3.one * 1.25f, 0.05f).SetEase(Ease.OutQuad));
                sequence.Insert(delay + 0.05f, glint.Core.DOScale(Vector3.one, 0.04f));
                sequence.Insert(delay, glint.Core.DORotate(new Vector3(0f, 0f, 30f), FlowMoveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
                sequence.Insert(delay + FlowMoveDuration - 0.1f, glint.Core.DOScale(Vector3.zero, 0.1f));
            }

            for (var i = 0; i < PawCount; i++)
            {
                var delay = PawStart + i * PawStagger;
                var spawn = FlowStart(upperRight, lowerLeft, PawFactors[i], perpDir, JitterRange * h);
                var target = spawn + diagDir * flowMoveDistance;
                var targetScale = 0.08f * h;

                var renderer = GetPooledPaw();
                renderer.sprite = _pawSprite;
                renderer.color = RandomColor(_pawColors);
                renderer.transform.localPosition = spawn;
                renderer.transform.localScale = Vector3.zero;
                renderer.transform.rotation = Quaternion.identity;

                var fadeStart = delay + 0.08f;
                var fadeDuration = Mathf.Max(0.05f, 0.5f - fadeStart);

                sequence.Insert(delay, renderer.transform.DOScale(targetScale, 0.08f).SetEase(Ease.OutBack));
                sequence.Insert(delay, renderer.transform.DOLocalMove(target, FlowMoveDuration).SetEase(Ease.OutCubic));
                sequence.Insert(fadeStart, renderer.DOFade(0f, fadeDuration));
            }

            sequence.InsertCallback(RootLifetime, OnSequenceComplete);
        }

        void OnSequenceComplete()
        {
            ReturnAllToPool();
            _sequence = null;
            _pendingSwapOutfit = null;
        }

        void LateUpdate()
        {
            if (_container == null || _character == null || _sequence is null || !_sequence.IsActive())
            {
                return;
            }

            var current = _character.position;
            _container.localPosition += current - _lastCharacterPosition;
            _lastCharacterPosition = current;
        }

        /// この View 自体は原点・等倍に置く前提 (コンテナのローカル座標をワールド座標として扱う)
        Transform CreateContainer()
        {
            var go = new GameObject("Container");
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void OnDestroy()
        {
            _sequence?.Kill();
        }

        /// upperRight から lowerLeft への進行度 t の位置に、進行方向と垂直な向きへランダムなジッターを加えた開始位置を返す
        static Vector3 FlowStart(Vector3 upperRight, Vector3 lowerLeft, float t, Vector3 perpDir, float jitterRange) =>
            Vector3.Lerp(upperRight, lowerLeft, t) + perpDir * UnityEngine.Random.Range(-jitterRange, jitterRange);

        Color RandomColor(Color[] colors) => colors[UnityEngine.Random.Range(0, colors.Length)];

        SpriteRenderer GetPooledCloud() => GetFromPool(_cloudPool, "Cloud", _sortingOrder);

        SpriteRenderer GetPooledPaw() => GetFromPool(_pawPool, "Paw", _sortingOrder);

        SpriteRenderer GetFromPool(List<SpriteRenderer> pool, string name, int sortingOrder)
        {
            foreach (var renderer in pool)
            {
                if (!renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(true);
                    return renderer;
                }
            }

            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(_container, false);
            var newRenderer = go.GetComponent<SpriteRenderer>();
            newRenderer.sortingOrder = sortingOrder;
            pool.Add(newRenderer);
            return newRenderer;
        }

        GlintUnit GetPooledGlint()
        {
            foreach (var glint in _glintPool)
            {
                if (!glint.Root.gameObject.activeSelf)
                {
                    glint.Root.gameObject.SetActive(true);
                    return glint;
                }
            }

            var unit = CreateGlintUnit();
            _glintPool.Add(unit);
            return unit;
        }

        GlintUnit CreateGlintUnit()
        {
            var rootGo = new GameObject("Glint");
            rootGo.transform.SetParent(_container, false);

            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(rootGo.transform, false);

            var tinted = CreateChildSprite("Tinted", coreGo.transform, _sortingOrder + 1);
            var white = CreateChildSprite("White", coreGo.transform, _sortingOrder + 2);

            return new GlintUnit { Root = rootGo.transform, Core = coreGo.transform, Tinted = tinted, White = white };
        }

        static SpriteRenderer CreateChildSprite(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        void ReturnAllToPool()
        {
            foreach (var renderer in _cloudPool) renderer.gameObject.SetActive(false);
            foreach (var renderer in _pawPool) renderer.gameObject.SetActive(false);
            foreach (var glint in _glintPool) glint.Root.gameObject.SetActive(false);
        }
    }
}
