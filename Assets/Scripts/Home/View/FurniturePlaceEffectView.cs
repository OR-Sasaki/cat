#nullable enable
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Home.View
{
    /// 家具設置時の「上から落ちて潰れて弾む」演出。IsoDraggableView._viewPivot（グリッドスナップ座標を持つルートではない）のみをアニメーションする
    public class FurniturePlaceEffectView : MonoBehaviour
    {
        // ViewPivotごとの本来のlocalPosition/localScale。初回に一度だけ記録し、以後は連打してもここから復元する
        readonly Dictionary<Transform, (Vector3 Pos, Vector3 Scale)> _originals = new();

        /// 家具設置演出を再生する。呼ぶたびにViewPivotを元の位置・スケールへリセットしてから開始するため、連打しても位置がずれない
        public void Play(IsoDraggableView view)
        {
            var viewPivot = view.ViewPivot;

            if (!_originals.TryGetValue(viewPivot, out var original))
            {
                original = (viewPivot.localPosition, viewPivot.localScale);
                _originals[viewPivot] = original;
            }

            DOTween.Kill(viewPivot);
            viewPivot.localPosition = original.Pos;
            viewPivot.localScale = original.Scale;

            var originalPos = original.Pos;
            var originalScale = original.Scale;

            var bounds = ComputeBounds(view.gameObject);
            var f = Mathf.Max(bounds.size.x, bounds.size.y);
            viewPivot.localPosition = originalPos + Vector3.up * 0.25f * f;

            DOTween.Sequence().SetLink(view.gameObject).SetId(viewPivot)
                .Append(viewPivot.DOLocalMoveY(originalPos.y, 0.22f).SetEase(Ease.InQuad))
                .Append(viewPivot.DOScale(new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z), 0.08f))
                .Append(viewPivot.DOScale(originalScale, 0.22f).SetEase(Ease.OutElastic, 1.1f));
        }

        static Bounds ComputeBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}
