using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
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
    /// 5. 獲得した毛糸玉を知らせるダイアログ
    public class CompleteSequenceManager : MonoBehaviour
    {
        [SerializeField] CompleteTransitionView _transitionView;
        [SerializeField] CompleteCharacterPopView _characterPopView;
        [SerializeField] CompletePanelView _completePanelView;
        [SerializeField] ConfettiBurstView _confettiView;
        [SerializeField, Min(0f)] float _popDelay = 0.1f;
        [SerializeField, Min(0f)] float _completePanelDelay = 0.15f;
        [SerializeField, Min(0f)] float _confettiDelay = 0.1f;
        [SerializeField, Min(0f)] float _rewardDialogDelay = 0.6f;

        PomodoroState _state;
        IDialogService _dialogService;
        CancellationToken _cancellationToken;
        CancellationTokenSource _sequenceCts;
        bool _isPlayed;

        [Inject]
        public void Construct(
            PomodoroState state, IDialogService dialogService, CancellationToken cancellationToken)
        {
            _state = state;
            _dialogService = dialogService;
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

                // 紙吹雪が開いてから報酬ダイアログを重ねる
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_rewardDialogDelay), cancellationToken: cancellationToken);
                await ShowYarnRewardDialogAsync(cancellationToken);
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

        /// 今回獲得した毛糸玉の数を知らせるダイアログを開く。
        /// 付与は PomodoroService が Complete 遷移の直前に済ませており、ここでは表示のみを担う
        async UniTask ShowYarnRewardDialogAsync(CancellationToken cancellationToken)
        {
            if (_state.YarnReward is not { } reward) return;

            await _dialogService.OpenAsync<TimerYarnRewardDialog, TimerYarnRewardDialogArgs>(
                new TimerYarnRewardDialogArgs(
                    reward.GrantedYarn, reward.EarnedToday, reward.DailyCap, reward.IsCapped),
                cancellationToken);
        }
    }
}
