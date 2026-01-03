#nullable enable

using System;
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

        [SerializeField] Canvas? _canvas;
        [SerializeField] CanvasGroup? _canvasGroup;

        public Canvas? Canvas => _canvas;

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

        public void SetAlphaByStackIndex(int stackIndex)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var alpha = BaseAlpha + (stackIndex * AlphaIncrement);
            _canvasGroup.alpha = Mathf.Min(alpha, MaxAlpha);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}
