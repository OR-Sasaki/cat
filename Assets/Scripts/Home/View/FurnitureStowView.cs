using UnityEngine;
using UnityEngine.UI;

namespace Home.View
{
    /// 家具を「しまう」ための画面下部ドロップゾーンUI
    /// ドラッグ中は常にうっすら表示し、ポインターがこのバーの矩形内に入ったら強調表示する。
    /// そこで離すと家具がしまわれる（しまう実処理は FurnitureStowService が担当）
    public class FurnitureStowView : MonoBehaviour
    {
        [Tooltip("当たり判定に使う矩形（通常はこのバー本体のRectTransform）")]
        [SerializeField] RectTransform _zoneRect;
        [Tooltip("表示/非表示を切り替えるCanvasGroup")]
        [SerializeField] CanvasGroup _canvasGroup;
        [Tooltip("ゾーン内かどうかで色を変える背景グラフィック")]
        [SerializeField] Graphic _background;

        [Header("Colors")]
        [SerializeField] Color _normalColor = new(0f, 0f, 0f, 0.55f);
        [SerializeField] Color _highlightColor = new(0.85f, 0.25f, 0.25f, 0.85f);

        [Header("Alpha")]
        [Tooltip("ドラッグ中・ゾーン外での“うっすら”表示の不透明度")]
        [SerializeField, Range(0f, 1f)] float _idleAlpha = 0.4f;
        [Tooltip("ゾーン内での強調表示の不透明度")]
        [SerializeField, Range(0f, 1f)] float _activeAlpha = 1f;

        Canvas _canvas;

        void Awake()
        {
            Hide();
        }

        /// バーを非表示にする（GameObjectは常時アクティブのままにして矩形判定を有効に保つ）
        public void Hide()
        {
            SetDisplay(false, false);
        }

        /// バーの表示状態を更新する
        /// visible=false: 非表示 / inZone=false: うっすら表示 / inZone=true: 強調表示
        public void SetDisplay(bool visible, bool inZone)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = !visible ? 0f : (inZone ? _activeAlpha : _idleAlpha);
                _canvasGroup.blocksRaycasts = false; // ドラッグ入力を妨げない
            }

            if (_background != null)
            {
                _background.color = inZone ? _highlightColor : _normalColor;
            }
        }

        /// スクリーン座標がバーの矩形内にあるか判定する
        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_zoneRect == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(_zoneRect, screenPoint, ResolveEventCamera());
        }

        /// 矩形判定に使うカメラを解決する（Screen Space - Overlay の場合は null）
        Camera ResolveEventCamera()
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) return null;
            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }
    }
}
