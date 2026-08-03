#nullable enable

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Root.View
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class BackdropView : MonoBehaviour, IPointerClickHandler
    {
        const float BaseAlpha = 0.5f;
        const float AlphaIncrement = 0.1f;
        const float MaxAlpha = 0.9f;

        /// ダイアログ側のフェードと尺を揃える
        const float FadeDuration = 0.0833f;

        [SerializeField] Canvas? _canvas;
        [SerializeField] CanvasGroup? _canvasGroup;

        Tween? _fadeTween;

        public Canvas? Canvas => _canvas;

        public bool IsInteractable => _canvasGroup != null && _canvasGroup.blocksRaycasts;

        public event Action? OnClicked;

        void Reset()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            _canvas ??= GetComponent<Canvas>();
            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
            }
        }

        /// スタックの深さに応じた濃さへフェードインする。
        /// 非表示から表示へ変わるときは、1 フレーム目に完成形が出ないよう透明から始める
        public void Show(int stackIndex)
        {
            KillFadeTween();

            var wasHidden = !gameObject.activeSelf;
            if (wasHidden)
            {
                gameObject.SetActive(true);
            }

            if (_canvasGroup == null)
            {
                return;
            }

            if (wasHidden)
            {
                _canvasGroup.alpha = 0f;
            }

            _fadeTween = _canvasGroup
                .DOFade(GetAlpha(stackIndex), FadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        /// フェードアウトしきってから非アクティブに戻す
        public void Hide()
        {
            KillFadeTween();

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _fadeTween = _canvasGroup
                .DOFade(0f, FadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.blocksRaycasts = interactable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }

        static float GetAlpha(int stackIndex)
        {
            return Mathf.Min(BaseAlpha + (stackIndex * AlphaIncrement), MaxAlpha);
        }

        void KillFadeTween()
        {
            if (_fadeTween == null)
            {
                return;
            }

            var tween = _fadeTween;
            _fadeTween = null;

            if (tween.IsActive())
            {
                tween.Kill();
            }
        }

        void OnDestroy()
        {
            KillFadeTween();
        }
    }
}
