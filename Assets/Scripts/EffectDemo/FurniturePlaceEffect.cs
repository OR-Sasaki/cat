#nullable enable
using System;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EffectDemo
{
    public enum FurnitureEffectKind
    {
        BounceLanding,
        SparkleShower,
        PopConfetti,
        PawRing,
    }

    /// 家具設置演出 4 種。生成物は一時ルートにまとめ、演出終了で破棄する
    public static class FurniturePlaceEffect
    {
        static readonly Color[] PastelColors =
        {
            new(1f, 0.75f, 0.85f), // pink
            new(0.68f, 0.92f, 0.78f), // mint
            new(1f, 0.90f, 0.55f), // yellow
            new(0.62f, 0.85f, 0.98f), // sky
            new(0.82f, 0.72f, 0.96f), // lavender
        };

        public static void Play(Transform furniture, Vector3 basePosition, FurnitureEffectKind kind)
        {
            DOTween.Kill(furniture);
            furniture.localScale = Vector3.one;
            furniture.position = basePosition;

            var bounds = ComputeBounds(furniture);

            switch (kind)
            {
                case FurnitureEffectKind.BounceLanding:
                    BounceLanding(furniture, basePosition, bounds);
                    break;
                case FurnitureEffectKind.SparkleShower:
                    SparkleShower(furniture, basePosition, bounds);
                    break;
                case FurnitureEffectKind.PopConfetti:
                    PopConfetti(furniture, basePosition, bounds);
                    break;
                case FurnitureEffectKind.PawRing:
                    PawRing(furniture, basePosition, bounds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        static Bounds ComputeBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        /// 上から落下 → 着地で潰れて弾む + 平たいリングと砂ぼこりが広がる
        static void BounceLanding(Transform furniture, Vector3 basePosition, Bounds bounds)
        {
            var f = Mathf.Max(bounds.size.x, bounds.size.y);
            var original = furniture.localScale;
            var start = basePosition + Vector3.up * 0.25f * f;
            furniture.position = start;

            DOTween.Sequence().SetLink(furniture.gameObject).SetId(furniture)
                .Append(furniture.DOMove(basePosition, 0.22f).SetEase(Ease.InQuad))
                .AppendCallback(() => SpawnLandingBurst(furniture, basePosition, bounds))
                .Append(furniture.DOScale(new Vector3(original.x * 1.15f, original.y * 0.85f, original.z), 0.08f))
                .Append(furniture.DOScale(original, 0.22f).SetEase(Ease.OutElastic, 1.1f))
                .OnKill(() =>
                {
                    furniture.localScale = original;
                    furniture.position = basePosition;
                });
        }

        static void SpawnLandingBurst(Transform furniture, Vector3 basePosition, Bounds bounds)
        {
            var w = bounds.size.x;
            var f = Mathf.Max(w, bounds.size.y);
            var root = new GameObject("BounceLandingFx");
            var ring = CreateSprite("Ring", root.transform, EffectSprites.Ring, RandomPastel(), 10000, basePosition);
            ring.transform.localScale = new Vector3(0.1f, 0.05f, 1f);
            DOTween.Sequence().SetLink(root)
                .Append(ring.transform.DOScale(new Vector3(w * 2.2f, w * 1.1f, 1f), 0.35f).SetEase(Ease.OutCubic))
                .Join(ring.DOFade(0f, 0.35f));

            const int dustCount = 6;
            for (var i = 0; i < dustCount; i++)
            {
                var angle = i * (360f / dustCount) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.4f, 0f);
                var dust = CreateSprite($"Dust{i}", root.transform, EffectSprites.Circle, RandomPastel(), 10000, basePosition);
                dust.transform.localScale = Vector3.one * 0.2f * f;
                DOTween.Sequence().SetLink(root)
                    .Append(dust.transform.DOMove(basePosition + dir * 0.4f * w, 0.3f).SetEase(Ease.OutCubic))
                    .Join(dust.DOFade(0f, 0.3f));
            }

            DestroyAfter(root, 0.4f);
        }

        /// 家具上空からキラキラが足元へ降り注ぐ
        static void SparkleShower(Transform furniture, Vector3 basePosition, Bounds bounds)
        {
            var f = Mathf.Max(bounds.size.x, bounds.size.y);
            var original = furniture.localScale;
            DOTween.Sequence().SetLink(furniture.gameObject).SetId(furniture)
                .Append(furniture.DOScale(original * 1.05f, 0.1f).SetEase(Ease.OutBack))
                .Append(furniture.DOScale(original, 0.15f))
                .OnKill(() => furniture.localScale = original);

            var root = new GameObject("SparkleShowerFx");
            const int count = 12;
            var bottomThirdMinY = bounds.min.y;
            var bottomThirdMaxY = bounds.min.y + bounds.size.y / 3f;
            var lastStart = 0f;

            for (var i = 0; i < count; i++)
            {
                var target = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bottomThirdMinY, bottomThirdMaxY),
                    0f);
                var start = new Vector3(target.x, bounds.max.y + 0.4f * f, 0f);
                var sprite = CreateSprite($"Sparkle{i}", root.transform, EffectSprites.Star4, RandomPastel(), 10000, start);
                sprite.transform.localScale = Vector3.zero;
                var targetScale = 0.25f * f;

                var delay = i * 0.04f;
                lastStart = delay;
                DOTween.Sequence().SetLink(root)
                    .AppendInterval(delay)
                    .Append(sprite.transform.DOScale(targetScale, 0.1f).SetEase(Ease.OutBack))
                    .Join(sprite.transform.DOMove(target, 0.4f).SetEase(Ease.InSine))
                    .Append(sprite.transform.DOScale(0f, 0.1f));
            }

            DestroyAfter(root, lastStart + 0.5f);
        }

        /// スケールでポンと弾む + 紙吹雪 14 枚が重力付きで飛び散る
        static void PopConfetti(Transform furniture, Vector3 basePosition, Bounds bounds)
        {
            var f = Mathf.Max(bounds.size.x, bounds.size.y);
            var original = furniture.localScale;
            DOTween.Sequence().SetLink(furniture.gameObject).SetId(furniture)
                .Append(furniture.DOScale(original * 1.15f, 0.15f).SetEase(Ease.OutBack))
                .Append(furniture.DOScale(original, 0.2f))
                .OnKill(() => furniture.localScale = original);

            var root = new GameObject("PopConfettiFx");
            const int count = 14;

            for (var i = 0; i < count; i++)
            {
                var confetti = CreateSprite($"Confetti{i}", root.transform, EffectSprites.Square, RandomPastel(), 10000, basePosition);
                confetti.transform.localScale = Vector3.one * 0.16f * f;

                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var speed = Random.Range(0.8f, 1.6f);
                var velocity = new Vector3(Mathf.Cos(angle), Mathf.Abs(Mathf.Sin(angle)) + 0.3f, 0f) * speed;
                var spin = Random.Range(-720f, 720f);

                DOTween.Sequence().SetLink(root)
                    .Join(DOVirtual.Float(0f, 1f, 0.8f, t =>
                        {
                            var gravity = Vector3.down * 1.8f * (t * t);
                            confetti.transform.position = basePosition + velocity * t + gravity;
                        })
                        .SetEase(Ease.Linear))
                    .Join(confetti.transform.DORotate(new Vector3(0f, 0f, spin), 0.8f, RotateMode.FastBeyond360))
                    .Insert(0.4f, confetti.DOFade(0f, 0.4f));
            }

            DestroyAfter(root, 0.8f);
        }

        /// 楕円の等角配置で肉球が順番にポンポン現れ、最後に平たいリングが一度だけ広がる
        static void PawRing(Transform furniture, Vector3 basePosition, Bounds bounds)
        {
            var w = bounds.size.x;
            var root = new GameObject("PawRingFx");
            const int count = 8;
            var radiusX = w * 0.7f;
            var radiusY = w * 0.35f;
            var lastStart = 0f;

            for (var i = 0; i < count; i++)
            {
                var angle = i * (360f / count) * Mathf.Deg2Rad;
                var position = basePosition + new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, 0f);
                var facing = Mathf.Atan2(Mathf.Sin(angle) * radiusY, Mathf.Cos(angle) * radiusX) * Mathf.Rad2Deg;
                var paw = CreateSprite($"Paw{i}", root.transform, EffectSprites.Paw, RandomPastel(), 10000, position);
                paw.transform.localScale = Vector3.zero;
                paw.transform.localRotation = Quaternion.Euler(0f, 0f, facing - 90f);

                var delay = i * 0.07f;
                lastStart = delay;
                DOTween.Sequence().SetLink(root)
                    .AppendInterval(delay)
                    .Append(paw.transform.DOScale(0.3f * w, 0.18f).SetEase(Ease.OutBack));
            }

            var ringDelay = lastStart + 0.3f;
            var ring = CreateSprite("Ring", root.transform, EffectSprites.Ring, RandomPastel(), 10000, basePosition);
            ring.transform.localScale = new Vector3(0.1f, 0.05f, 1f);
            DOTween.Sequence().SetLink(root)
                .AppendInterval(ringDelay)
                .Append(ring.transform.DOScale(new Vector3(w * 2.4f, w * 1.2f, 1f), 0.35f).SetEase(Ease.OutCubic))
                .Join(ring.DOFade(0f, 0.35f));

            var fadeStart = ringDelay + 0.35f;
            for (var i = 0; i < root.transform.childCount - 1; i++)
            {
                var renderer = root.transform.GetChild(i).GetComponent<SpriteRenderer>();
                DOTween.Sequence().SetLink(root).AppendInterval(fadeStart).Append(renderer.DOFade(0f, 0.3f));
            }

            DestroyAfter(root, fadeStart + 0.3f);
        }

        static Color RandomPastel() => PastelColors[Random.Range(0, PastelColors.Length)];

        static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color, int sortingOrder, Vector3 worldPosition)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        static void DestroyAfter(GameObject root, float delay) =>
            DOTween.Sequence().SetLink(root).AppendInterval(delay).OnComplete(() => Object.Destroy(root));
    }
}
