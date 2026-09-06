#nullable enable
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EffectDemo
{
    public enum OutfitEffectKind
    {
        SparklePop,
        PoofCloud,
        HeartShower,
        PawStamp,
        PoofCombo,
    }

    /// 着せ替え演出 4 種。生成物は一時ルートにまとめ、演出終了で破棄する
    public static class OutfitChangeEffect
    {
        static readonly Color[] PastelColors =
        {
            new(1f, 0.75f, 0.85f), // pink
            new(0.68f, 0.92f, 0.78f), // mint
            new(1f, 0.90f, 0.55f), // yellow
            new(0.62f, 0.85f, 0.98f), // sky
            new(0.82f, 0.72f, 0.96f), // lavender
            Color.white,
        };

        /// 肉球スタンプ用の、はっきり見える濃いめのトーン
        static readonly Color[] VividColors =
        {
            new(1f, 0.4f, 0.55f, 1f),
            new(0.25f, 0.85f, 0.45f, 1f),
            new(1f, 0.8f, 0.15f, 1f),
            new(0.25f, 0.65f, 0.95f, 1f),
            new(0.65f, 0.45f, 0.9f, 1f),
        };

        public static void Play(Transform character, Bounds bounds, OutfitEffectKind kind, Action swapOutfit)
        {
            DOTween.Kill(character);
            character.localScale = Vector3.one;

            switch (kind)
            {
                case OutfitEffectKind.SparklePop:
                    SparklePop(character, bounds, swapOutfit);
                    break;
                case OutfitEffectKind.PoofCloud:
                    PoofCloud(character, bounds, swapOutfit);
                    break;
                case OutfitEffectKind.HeartShower:
                    HeartShower(character, bounds, swapOutfit);
                    break;
                case OutfitEffectKind.PawStamp:
                    PawStamp(character, bounds, swapOutfit);
                    break;
                case OutfitEffectKind.PoofCombo:
                    PoofCombo(character, bounds, swapOutfit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// 潰れて伸びるスカッシュ&ストレッチ + キラキラ 9 個
        static void SparklePop(Transform character, Bounds bounds, Action swapOutfit)
        {
            swapOutfit();

            var original = character.localScale;
            DOTween.Sequence().SetLink(character.gameObject).SetId(character)
                .Append(character.DOScale(new Vector3(original.x * 1.15f, original.y * 0.85f, original.z), 0.12f))
                .Append(character.DOScale(new Vector3(original.x * 0.92f, original.y * 1.1f, original.z), 0.12f))
                .Append(character.DOScale(original, 0.11f))
                .OnKill(() => character.localScale = original);

            var root = new GameObject("SparklePopFx");
            var h = bounds.size.y;
            const int count = 9;
            var lastStart = 0f;

            for (var i = 0; i < count; i++)
            {
                var position = RandomPointInBounds(bounds);
                var renderer = CreateSprite($"Star{i}", root.transform, EffectSprites.Star4, RandomPastel(), 10000, position);
                renderer.transform.localScale = Vector3.zero;
                var targetScale = Random.Range(0.15f, 0.22f) * h;

                var delay = i * 0.045f;
                lastStart = delay;
                DOTween.Sequence().SetLink(root)
                    .AppendInterval(delay)
                    .Append(renderer.transform.DOScale(targetScale, 0.18f).SetEase(Ease.OutBack))
                    .Join(renderer.transform.DORotate(new Vector3(0f, 0f, Random.Range(-40f, 40f)), 0.3f))
                    .Append(renderer.transform.DOScale(0f, 0.15f).SetEase(Ease.InBack));
            }

            DestroyAfter(root, lastStart + 0.35f);
        }

        /// もくもく雲がキャラ全体を覆い尽くし、そのピークで着替えさせてから晴れる
        static void PoofCloud(Transform character, Bounds bounds, Action swapOutfit)
        {
            var root = new GameObject("PoofCloudFx");
            var h = bounds.size.y;
            var center = bounds.center;
            const int count = 9;
            const int cols = 3;

            for (var i = 0; i < count; i++)
            {
                var col = i % cols;
                var row = i / cols;
                var offset = new Vector3(
                    Mathf.Lerp(-0.7f, 0.7f, col / (float)(cols - 1)) * bounds.extents.x + Random.Range(-0.15f, 0.15f) * bounds.extents.x,
                    Mathf.Lerp(-0.7f, 0.7f, row / (float)(cols - 1)) * bounds.extents.y + Random.Range(-0.15f, 0.15f) * bounds.extents.y,
                    0f);
                var position = center + offset;
                var renderer = CreateSprite($"Cloud{i}", root.transform, EffectSprites.Cloud, RandomPastel(), 10000, position);
                renderer.transform.localScale = Vector3.zero;
                var targetScale = Random.Range(0.4f, 0.5f) * h;
                var driftDir = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.up;

                DOTween.Sequence().SetLink(root)
                    .Append(renderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack))
                    .AppendInterval(0.4f)
                    .Append(renderer.transform.DOMove(position + driftDir * h * 0.3f, 0.4f))
                    .Join(renderer.GetComponent<SpriteRenderer>().DOFade(0f, 0.4f));
            }

            // ピーク (雲が最大になった瞬間) で着せ替える
            DOVirtual.DelayedCall(0.2f, () => swapOutfit()).SetLink(root);
            DestroyAfter(root, 0.2f + 0.4f + 0.4f);
        }

        /// スケールパンチ + 足元からハートが 9 個ふわふわ上昇
        static void HeartShower(Transform character, Bounds bounds, Action swapOutfit)
        {
            swapOutfit();

            var original = character.localScale;
            DOTween.Sequence().SetLink(character.gameObject).SetId(character)
                .Append(character.DOScale(original * 1.12f, 0.1f).SetEase(Ease.OutBack))
                .Append(character.DOScale(original, 0.15f))
                .OnKill(() => character.localScale = original);

            var root = new GameObject("HeartShowerFx");
            var h = bounds.size.y;
            const int count = 9;
            var lastStart = 0f;

            for (var i = 0; i < count; i++)
            {
                var startX = Random.Range(bounds.min.x, bounds.max.x);
                var start = new Vector3(startX, bounds.min.y + 0.05f, 0f);
                var renderer = CreateSprite($"Heart{i}", root.transform, EffectSprites.Heart, RandomPastel(), 10000, start);
                renderer.transform.localScale = Vector3.zero;
                var targetScale = Random.Range(0.14f, 0.18f) * h;

                var delay = i * 0.06f;
                lastStart = delay;
                var wobble = Random.Range(0.1f, 0.25f);
                var duration = 0.9f - delay;

                var sequence = DOTween.Sequence().SetLink(root)
                    .AppendInterval(delay)
                    .Append(renderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack));
                sequence.Join(renderer.transform.DOMoveY(start.y + 0.5f * h, duration).SetEase(Ease.OutSine));
                sequence.Join(DOVirtual.Float(0f, 1f, duration, t => renderer.transform.position = new Vector3(
                        start.x + Mathf.Sin(t * Mathf.PI * 2f) * wobble,
                        renderer.transform.position.y,
                        0f))
                    .SetEase(Ease.Linear));
                sequence.Insert(delay + duration * 0.5f, renderer.GetComponent<SpriteRenderer>().DOFade(0f, duration * 0.5f));
            }

            DestroyAfter(root, lastStart + 0.9f);
        }

        /// 小さくホップ + 肉球スタンプが円弧状に 6 個ポンポン現れる
        static void PawStamp(Transform character, Bounds bounds, Action swapOutfit)
        {
            swapOutfit();

            var h = bounds.size.y;
            var original = character.localPosition;
            DOTween.Sequence().SetLink(character.gameObject).SetId(character)
                .Append(character.DOLocalMoveY(original.y + 0.05f * h, 0.12f).SetEase(Ease.OutQuad))
                .Append(character.DOLocalMoveY(original.y, 0.13f).SetEase(Ease.InQuad))
                .OnKill(() => character.localPosition = original);

            var root = new GameObject("PawStampFx");
            const int count = 6;
            var radiusX = bounds.extents.x * 1.1f;
            var radiusY = bounds.extents.y * 0.9f;
            var lastStart = 0f;

            for (var i = 0; i < count; i++)
            {
                var angle = i * (360f / count) * Mathf.Deg2Rad;
                var position = bounds.center + new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, 0f);
                var renderer = CreateSprite($"Paw{i}", root.transform, EffectSprites.Paw, VividColors[Random.Range(0, VividColors.Length)], 10000, position);
                renderer.transform.localScale = Vector3.zero;
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
                var targetScale = 0.22f * h;

                var delay = i * 0.06f;
                lastStart = delay;
                DOTween.Sequence().SetLink(root)
                    .AppendInterval(delay)
                    .Append(renderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack))
                    .AppendInterval(0.35f)
                    .Append(renderer.GetComponent<SpriteRenderer>().DOFade(0f, 0.3f));
            }

            DestroyAfter(root, lastStart + 0.2f + 0.35f + 0.3f);
        }

        /// Cloud スプライトの内接する不透明円の半径 (localScale=1 のとき)。控えめな見積もりでカバレッジを保証する
        const float CloudInnerRadiusRatio = 0.36f;

        /// 円形にまとまった雲でキャラを覆い隠し、そのまま晴れる一続きの動き (合計 ≈0.5秒)。
        /// 晴れると同時に「キラーン」ときらめくキラキラ 5 個と肉球 3 個が右上から左下へ流れる
        static void PoofCombo(Transform character, Bounds bounds, Action swapOutfit)
        {
            var root = new GameObject("PoofComboFx");
            var h = bounds.size.y;
            var center = bounds.center;
            var extents = bounds.extents;

            const float coverDuration = 0.14f;
            const float clearStart = 0.16f;
            const float clearDuration = 0.28f; // clearStart + clearDuration = 0.44s
            const float driftRatio = 0.12f;

            foreach (var cloud in BuildCloudCoverage(bounds, h))
            {
                var driftDir = (cloud.position - center).sqrMagnitude > 0.0001f ? (cloud.position - center).normalized : Vector3.up;
                var targetScale = cloud.innerRadius / CloudInnerRadiusRatio;

                var renderer = CreateSprite("Puff", root.transform, EffectSprites.Cloud, RandomPastel(), 10000, cloud.position);
                renderer.transform.localScale = Vector3.zero;

                DOTween.Sequence().SetLink(root)
                    .Insert(0f, renderer.transform.DOScale(targetScale, coverDuration).SetEase(Ease.OutQuad))
                    .Insert(clearStart, renderer.transform.DOMove(cloud.position + driftDir * driftRatio * h, clearDuration).SetEase(Ease.InQuad))
                    .Insert(clearStart, renderer.GetComponent<SpriteRenderer>().DOFade(0f, clearDuration));
            }

            // 完全に隠れているタイミングで着せ替える
            DOVirtual.DelayedCall(coverDuration, () => swapOutfit()).SetLink(root);

            // 晴れると同時にキャラがきゅっと縮んで戻るお披露目リアクション
            var original = character.localScale;
            DOTween.Sequence().SetLink(character.gameObject).SetId(character)
                .AppendInterval(clearStart)
                .AppendCallback(() => character.localScale = new Vector3(original.x * 1.06f, original.y * 0.94f, original.z))
                .Append(character.DOScale(original, 0.15f))
                .OnKill(() => character.localScale = original);

            // 右上から左下へ流れる方向。キラキラと肉球はこの向き・速度を共有する
            var diagDir = new Vector3(-1f, -1f, 0f).normalized;
            var perpDir = new Vector3(1f, -1f, 0f).normalized;
            var upperRight = center + new Vector3(extents.x * 1.08f, extents.y * 1.08f, 0f);
            var lowerLeft = center - new Vector3(extents.x * 1.08f, extents.y * 1.08f, 0f);
            const float flowMoveDuration = 0.28f;
            var flowMoveDistance = 0.35f * h;
            const float jitterRange = 0.15f;

            const int glintCount = 5;
            const float glintStagger = 0.025f;

            for (var i = 0; i < glintCount; i++)
            {
                var delay = clearStart + i * glintStagger;
                var t = 0.05f + 0.5f * i / (glintCount - 1);
                var spawn = FlowStart(upperRight, lowerLeft, t, perpDir, jitterRange * h);
                var target = spawn + diagDir * flowMoveDistance;
                var visualScale = 0.18f * h;

                var glintRoot = new GameObject($"Glint{i}");
                glintRoot.transform.SetParent(root.transform, false);
                glintRoot.transform.position = spawn;

                var core = new GameObject("Core");
                core.transform.SetParent(glintRoot.transform, false);
                core.transform.position = spawn;

                var tinted = CreateSprite("Tinted", core.transform, EffectSprites.Star4, RandomPastel(), 10001, spawn);
                tinted.transform.localScale = Vector3.one * visualScale;
                var white = CreateSprite("White", core.transform, EffectSprites.Star4, Color.white, 10002, spawn);
                white.transform.localScale = Vector3.one * visualScale * 0.55f;
                core.transform.localScale = Vector3.zero;

                DOTween.Sequence().SetLink(root)
                    .Insert(delay, glintRoot.transform.DOMove(target, flowMoveDuration).SetEase(Ease.OutCubic))
                    .Insert(delay, core.transform.DOScale(Vector3.one * 1.25f, 0.05f).SetEase(Ease.OutQuad))
                    .Insert(delay + 0.05f, core.transform.DOScale(Vector3.one, 0.04f))
                    .Insert(delay, core.transform.DORotate(new Vector3(0f, 0f, 30f), flowMoveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear))
                    .Insert(delay + flowMoveDuration - 0.1f, core.transform.DOScale(Vector3.zero, 0.1f));
            }

            const int pawCount = 3;
            const float pawStart = 0.2f;
            const float pawStagger = 0.05f;
            var pawFactors = new[] { 0.2f, 0.4f, 0.6f };

            for (var i = 0; i < pawCount; i++)
            {
                var delay = pawStart + i * pawStagger;
                var spawn = FlowStart(upperRight, lowerLeft, pawFactors[i], perpDir, jitterRange * h);
                var target = spawn + diagDir * flowMoveDistance;
                var targetScale = 0.08f * h;

                var renderer = CreateSprite($"ComboPaw{i}", root.transform, EffectSprites.Paw, VividColors[Random.Range(0, VividColors.Length)], 10001, spawn);
                renderer.transform.localScale = Vector3.zero;

                var fadeStart = delay + 0.08f;
                var fadeDuration = Mathf.Max(0.05f, 0.5f - fadeStart);

                DOTween.Sequence().SetLink(root)
                    .Insert(delay, renderer.transform.DOScale(targetScale, 0.08f).SetEase(Ease.OutBack))
                    .Insert(delay, renderer.transform.DOMove(target, flowMoveDuration).SetEase(Ease.OutCubic))
                    .Insert(fadeStart, renderer.GetComponent<SpriteRenderer>().DOFade(0f, fadeDuration));
            }

            DestroyAfter(root, 0.6f);
        }

        /// upperRight から lowerLeft への進行度 t の位置に、進行方向と垂直な向きへランダムなジッターを加えた開始位置を返す
        static Vector3 FlowStart(Vector3 upperRight, Vector3 lowerLeft, float t, Vector3 perpDir, float jitterRange) =>
            Vector3.Lerp(upperRight, lowerLeft, t) + perpDir * Random.Range(-jitterRange, jitterRange);

        /// bounds 内をグリッドサンプリングし、シャッフル順に未カバーの点から雲を配置してキャラ全体を覆い尽くす配置を求める。
        /// サンプル数は小さい (spacing≈0.07h) ため O(n^2) の網羅チェックで十分
        static List<(Vector3 position, float innerRadius)> BuildCloudCoverage(Bounds bounds, float h)
        {
            const float spacing = 0.07f;
            var step = spacing * h;

            var samples = new List<Vector3>();
            for (var y = bounds.min.y; y <= bounds.max.y; y += step)
            {
                for (var x = bounds.min.x; x <= bounds.max.x; x += step)
                {
                    samples.Add(new Vector3(x, y, 0f));
                }
            }

            // Fisher-Yates シャッフルで走査順をランダム化し、機械的な並びを避ける
            for (var i = samples.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (samples[i], samples[j]) = (samples[j], samples[i]);
            }

            var covered = new bool[samples.Count];
            var clouds = new List<(Vector3 position, float innerRadius)>();

            for (var i = 0; i < samples.Count; i++)
            {
                if (covered[i])
                {
                    continue;
                }

                var offset = new Vector3(Random.Range(-0.08f, 0.08f) * h, Random.Range(-0.08f, 0.08f) * h, 0f);
                var spawnPosition = samples[i] + offset;
                var innerRadius = Random.Range(0.18f, 0.34f) * h;
                clouds.Add((spawnPosition, innerRadius));

                for (var j = 0; j < samples.Count; j++)
                {
                    if (!covered[j] && (samples[j] - spawnPosition).sqrMagnitude <= innerRadius * innerRadius)
                    {
                        covered[j] = true;
                    }
                }
            }

            if (clouds.Count > 40)
            {
                Debug.LogWarning($"PoofCombo: cloud count {clouds.Count} exceeds expected range (12-25); check spacing/radius constants.");
            }

            return clouds;
        }

        static Vector3 RandomPointInBounds(Bounds bounds) =>
            new(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y), 0f);

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
