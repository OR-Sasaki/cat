#nullable enable
using DG.Tweening;
using UnityEngine;

namespace Home.View
{
    /// 家具設置時の「上から落ちて潰れて弾む」演出。IsoDraggableView._viewPivot（グリッドスナップ座標を持つルートではない）のみをアニメーションする
    public class FurniturePlaceEffectView : MonoBehaviour
    {
        static readonly Vector2 SquashRatio = new(1.15f, 0.85f);

        /// 家具設置演出を再生する。呼ぶたびに ViewPivot を本来の位置・スケールから始めるため、連打しても位置がずれない
        public void Play(IsoDraggableView view)
        {
            var viewPivot = view.ViewPivot;
            var originalPos = view.ViewPivotRestPosition;
            var originalScale = view.ViewPivotRestScale;

            DOTween.Kill(viewPivot);
            viewPivot.localScale = originalScale;
            viewPivot.localPosition = originalPos;

            var bounds = ComputeBounds(view.gameObject);
            var f = Mathf.Max(bounds.size.x, bounds.size.y);
            viewPivot.localPosition = originalPos + Vector3.up * 0.25f * f;

            var anchorWorld = new Vector3(bounds.center.x, bounds.min.y, viewPivot.position.z);
            var anchorInParent = viewPivot.parent != null
                ? viewPivot.parent.InverseTransformPoint(anchorWorld)
                : anchorWorld;
            var anchorOffset = anchorInParent - originalPos;

            DOTween.Sequence().SetLink(view.gameObject).SetId(viewPivot)
                .Append(viewPivot.DOLocalMoveY(originalPos.y, 0.22f).SetEase(Ease.InQuad))
                .Append(DOVirtual.Float(0f, 1f, 0.08f, t => ApplySquash(viewPivot, originalPos, originalScale, anchorOffset, Vector2.LerpUnclamped(Vector2.one, SquashRatio, t))))
                .Append(DOVirtual.Float(0f, 1f, 0.22f, t => ApplySquash(viewPivot, originalPos, originalScale, anchorOffset, Vector2.LerpUnclamped(SquashRatio, Vector2.one, t))).SetEase(Ease.OutElastic, 1.1f));
        }

        static void ApplySquash(Transform viewPivot, Vector3 originalPos, Vector3 originalScale, Vector3 anchorOffset, Vector2 ratio)
        {
            viewPivot.localScale = new Vector3(originalScale.x * ratio.x, originalScale.y * ratio.y, originalScale.z);
            viewPivot.localPosition = originalPos + new Vector3(anchorOffset.x * (1f - ratio.x), anchorOffset.y * (1f - ratio.y), 0f);
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
