using Timer.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Timer.View
{
    /// 休憩中に背景を夕方の色合いへ遷移させるビュー。
    /// 空は夕焼け画像のオーバーレイをフェードインし、雲は暖色に、
    /// 電柱・道路など手前のオブジェクトは暗めの色にティントする。
    public class TimerEveningView : MonoBehaviour
    {
        [SerializeField] Image _sunsetSkyOverlay;
        [SerializeField] Image[] _warmTintTargets;
        [SerializeField] Image[] _dimTargets;
        [SerializeField] Color _warmTintColor = new Color(1f, 0.82f, 0.74f);
        [SerializeField] Color _dimColor = new Color(0.62f, 0.57f, 0.7f);
        [SerializeField, Min(0f)] float _transitionSeconds = 3f;
        [SerializeField] AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        PomodoroState _state;
        Color[] _warmTintOriginals;
        Color[] _dimOriginals;
        float _evening01;
        float _target01;

        [Inject]
        public void Construct(PomodoroState state)
        {
            _state = state;
        }

        void Start()
        {
            _warmTintOriginals = CaptureOriginals(_warmTintTargets);
            _dimOriginals = CaptureOriginals(_dimTargets);

            _state.OnPhaseChanged += OnPhaseChanged;
            Apply(_transitionCurve.Evaluate(_evening01));
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
        }

        void Update()
        {
            if (_state.IsPaused) return;
            if (Mathf.Approximately(_evening01, _target01)) return;

            var step = _transitionSeconds > 0f ? Time.deltaTime / _transitionSeconds : 1f;
            _evening01 = Mathf.MoveTowards(_evening01, _target01, step);
            Apply(_transitionCurve.Evaluate(_evening01));
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            _target01 = phase == PomodoroPhase.Break ? 1f : 0f;
        }

        void Apply(float t)
        {
            if (_sunsetSkyOverlay != null)
            {
                var color = _sunsetSkyOverlay.color;
                color.a = t;
                _sunsetSkyOverlay.color = color;
            }

            ApplyTint(_warmTintTargets, _warmTintOriginals, _warmTintColor, t);
            ApplyTint(_dimTargets, _dimOriginals, _dimColor, t);
        }

        // 元の色に対する乗算ティントを t で補間して適用する（元のアルファは維持）
        static void ApplyTint(Image[] targets, Color[] originals, Color tint, float t)
        {
            if (targets == null || originals == null) return;

            for (int i = 0; i < targets.Length; i++)
            {
                var image = targets[i];
                if (image == null) continue;

                var original = originals[i];
                image.color = Color.Lerp(original, original * tint, t);
            }
        }

        static Color[] CaptureOriginals(Image[] targets)
        {
            if (targets == null) return new Color[0];

            var originals = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                originals[i] = targets[i] != null ? targets[i].color : Color.white;
            }
            return originals;
        }
    }
}
