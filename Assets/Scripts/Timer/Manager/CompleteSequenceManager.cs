using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Timer.State;
using Timer.View;
using UnityEngine;
using VContainer;

namespace Timer.Manager
{
    /// タイマー完了時の演出を順番に進める進行役。
    /// 1. 画面を覆うトランジションパネル（同時にキャラクターが画面下へ退場）
    /// 2. キャラクターが大きく画面下から飛び出す
    /// 3. 完了 UI が画面上部からスライドイン
    /// 4. クラッカーの紙吹雪
    public class CompleteSequenceManager : MonoBehaviour
    {
        [SerializeField] CompleteTransitionView _transitionView;
        [SerializeField] CompleteCharacterPopView _characterPopView;
        [SerializeField] CompletePanelView _completePanelView;
        [SerializeField] ConfettiBurstView _confettiView;
        [SerializeField, Min(0f)] float _popDelay = 0.1f;
        [SerializeField, Min(0f)] float _completePanelDelay = 0.15f;
        [SerializeField, Min(0f)] float _confettiDelay = 0.1f;

        PomodoroState _state;
        CancellationToken _cancellationToken;
        CancellationTokenSource _sequenceCts;
        bool _isPlayed;

        [Inject]
        public void Construct(PomodoroState state, CancellationToken cancellationToken)
        {
            _state = state;
            _cancellationToken = cancellationToken;
        }

        void Start()
        {
            // 完了 UI は演出の 3 番目で降りてくるため、開始時点では画面上部の外へ逃がしておく
            if (_completePanelView != null)
            {
                _completePanelView.MoveOffScreenTop();
            }

            _state.OnPhaseChanged += OnPhaseChanged;

            if (_state.CurrentPhase == PomodoroPhase.Complete)
            {
                StartSequence();
            }
        }

        void OnDestroy()
        {
            if (_state != null)
            {
                _state.OnPhaseChanged -= OnPhaseChanged;
            }
            _sequenceCts?.Cancel();
            _sequenceCts?.Dispose();
        }

        void OnPhaseChanged(PomodoroPhase phase)
        {
            if (phase != PomodoroPhase.Complete) return;
            StartSequence();
        }

        void StartSequence()
        {
            if (_isPlayed) return;
            _isPlayed = true;

            _sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
            PlayAsync(_sequenceCts.Token).Forget();
        }

        async UniTaskVoid PlayAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 画面が覆われるまでの間にキャラクターも下へ抜けるので、この 2 つは同時に走らせる
                await UniTask.WhenAll(
                    _transitionView.PlayAsync(cancellationToken),
                    _characterPopView.DuckOutAsync(cancellationToken));

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_popDelay), cancellationToken: cancellationToken);
                await _characterPopView.PopAsync(cancellationToken);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_completePanelDelay), cancellationToken: cancellationToken);
                await _completePanelView.SlideInAsync(cancellationToken);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_confettiDelay), cancellationToken: cancellationToken);
                _confettiView.Burst();
            }
            catch (OperationCanceledException)
            {
                // シーン破棄によるキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[CompleteSequenceManager] {e.Message}\n{e.StackTrace}", this);
            }
        }
    }
}
