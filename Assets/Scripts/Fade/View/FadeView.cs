using System.Threading.Tasks;
using UnityEngine;

namespace Fade.View
{
    public class FadeView : MonoBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        void Awake()
        {
            _canvasGroup.alpha = 1f;
        }

        public async Task AnimateFade(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = _fadeCurve.Evaluate(t);
                _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, curveValue);
                await Task.Yield();
            }

            _canvasGroup.alpha = toAlpha;
        }
    }
}
