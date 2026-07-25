using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Timer.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

namespace Timer.View
{
    public class CompletePanelView : MonoBehaviour
    {
        [SerializeField] RectTransform _root;
        [SerializeField] TextMeshProUGUI _totalFocusTimeText;
        [SerializeField] Button _homeButton;
        [SerializeField, Min(0.01f)] float _slideDuration = 0.55f;
        [SerializeField, Min(0f)] float _slideOvershoot = 0.9f;

        const float FallbackScreenHeight = 1920f;

        PomodoroState _state;
        SceneLoader _sceneLoader;

        [Inject]
        public void Construct(PomodoroState state, SceneLoader sceneLoader)
        {
            _state = state;
            _sceneLoader = sceneLoader;
        }

        void Start()
        {
            _homeButton.onClick.AddListener(OnHomeButtonClicked);
            _state.OnPhaseChanged += OnPhaseChanged;

            if (_state.CurrentPhase == PomodoroPhase.Complete)
            {
                UpdateTotalFocusTimeText();
            }
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnPhaseChanged -= OnPhaseChanged;
        }

        /// 完了演出で降りてくるまで画面上部の外へ退避させる
        public void MoveOffScreenTop()
        {
            if (_root == null) return;
            _root.anchoredPosition = new Vector2(0f, ScreenHeight());
        }

        /// 画面上部からスライドインさせる
        public async UniTask SlideInAsync(CancellationToken cancellationToken)
        {
            if (_root == null) return;

            var from = new Vector2(0f, ScreenHeight());
            _root.anchoredPosition = from;

            var elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / _slideDuration);
                _root.anchoredPosition = Vector2.LerpUnclamped(
                    from, Vector2.zero, CompleteEase.OutBack(t, _slideOvershoot));
            }

            _root.anchoredPosition = Vector2.zero;
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            if (phase != PomodoroPhase.Complete) return;
            UpdateTotalFocusTimeText();
        }

        void UpdateTotalFocusTimeText()
        {
            var totalMinutes = Mathf.FloorToInt(_state.TotalFocusTime / 60f);
            var totalSeconds = Mathf.FloorToInt(_state.TotalFocusTime % 60f);
            _totalFocusTimeText.text = $"{totalMinutes:D2}:{totalSeconds:D2}";
        }

        // パネルは画面いっぱいに引き伸ばされているため、自身の高さがそのまま画面高になる
        float ScreenHeight()
        {
            var height = _root.rect.height;
            return height > 0f ? height : FallbackScreenHeight;
        }

        void OnHomeButtonClicked()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }
    }
}
