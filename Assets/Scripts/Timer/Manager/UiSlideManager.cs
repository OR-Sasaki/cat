using System.Threading;
using Cysharp.Threading.Tasks;
using Timer.State;
using UnityEngine;
using VContainer;

namespace Timer.Manager
{
    public class UiSlideManager : MonoBehaviour
    {
        [SerializeField] RectTransform _focusPanel;
        [SerializeField] RectTransform _breakPanel;
        [SerializeField] RectTransform _completePanel;
        [SerializeField] AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] float _slideDuration = 0.4f;

        PomodoroState _state;
        float _screenWidth;
        RectTransform _currentPanel;
        CancellationToken _cancellationToken;

        [Inject]
        public void Construct(PomodoroState state, CancellationToken cancellationToken)
        {
            _state = state;
            _cancellationToken = cancellationToken;
        }

        void Start()
        {
            _screenWidth = ((RectTransform)_focusPanel.parent).rect.width;
            if (_screenWidth <= 0f) _screenWidth = 1080f;

            // 初期配置: 集中パネルが画面内、他は画面外（右側）
            _focusPanel.anchoredPosition = Vector2.zero;
            _breakPanel.anchoredPosition = new Vector2(_screenWidth, 0f);
            _completePanel.anchoredPosition = new Vector2(_screenWidth, 0f);
            _currentPanel = _focusPanel;

            _state.OnPhaseChanged += OnPhaseChanged;
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            var nextPanel = phase switch
            {
                PomodoroPhase.Focus => _focusPanel,
                PomodoroPhase.Break => _breakPanel,
                PomodoroPhase.Complete => _completePanel,
                _ => _focusPanel
            };

            if (nextPanel == _currentPanel) return;

            SlideAsync(_currentPanel, nextPanel, _cancellationToken).Forget();
            _currentPanel = nextPanel;
        }

        async UniTaskVoid SlideAsync(RectTransform outPanel, RectTransform inPanel, CancellationToken ct)
        {
            var outFrom = outPanel.anchoredPosition;
            var outTo = new Vector2(-_screenWidth, 0f);
            var inFrom = new Vector2(_screenWidth, 0f);
            var inTo = Vector2.zero;

            inPanel.anchoredPosition = inFrom;

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _slideDuration);
                var curveValue = _slideCurve.Evaluate(t);

                outPanel.anchoredPosition = Vector2.Lerp(outFrom, outTo, curveValue);
                inPanel.anchoredPosition = Vector2.Lerp(inFrom, inTo, curveValue);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            outPanel.anchoredPosition = outTo;
            inPanel.anchoredPosition = inTo;
        }
    }
}
