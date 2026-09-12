#nullable enable
using DG.Tweening;
using UnityEngine;

namespace Home.View
{
    /// 家具設置時の「上から落ちて潰れて弾む」演出。IsoDraggableView._viewPivot（グリッドスナップ座標を持つルートではない）のみをアニメーションする
    public class FurniturePlaceEffectView : MonoBehaviour
    {
        /// 家具設置演出を再生する。呼ぶたびに ViewPivot を本来の位置・スケールから始めるため、連打しても位置がずれない
        public void Play(IsoDraggableView view)
        {
            var viewPivot = view.ViewPivot;
            var originalPos = view.ViewPivotRestPosition;
            var originalScale = view.ViewPivotRestScale;

            DOTween.Kill(viewPivot);
            viewPivot.localScale = originalScale;

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
