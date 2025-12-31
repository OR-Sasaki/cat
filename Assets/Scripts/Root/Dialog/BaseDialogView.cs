#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Root.Dialog
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseDialogView : MonoBehaviour
    {
        [SerializeField] Animator? _animator;
        [SerializeField] Button? _closeButton;
        [SerializeField] CanvasGroup? _canvasGroup;

        static readonly int OpenTrigger = Animator.StringToHash("Open");
        static readonly int CloseTrigger = Animator.StringToHash("Close");

        UniTaskCompletionSource? _animationCompletionSource;

        public event Action<DialogResult>? OnCloseRequested;

        protected virtual void Reset()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        protected virtual void Awake()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        void OnCloseButtonClicked()
        {
            RequestClose(DialogResult.Close);
        }

        protected void RequestClose(DialogResult result)
        {
            OnCloseRequested?.Invoke(result);
        }

        public async UniTask PlayOpenAnimationAsync(CancellationToken cancellationToken = default)
        {
            if (_animator == null)
            {
                return;
            }

            SetInteractable(false);
            _animator.SetTrigger(OpenTrigger);
            await WaitForAnimationCompleteAsync(cancellationToken);
            SetInteractable(true);
        }

        public async UniTask PlayCloseAnimationAsync(CancellationToken cancellationToken = default)
        {
            if (_animator == null)
            {
                return;
            }

            SetInteractable(false);
            _animator.SetTrigger(CloseTrigger);
            await WaitForAnimationCompleteAsync(cancellationToken);
        }

        // AnimationEventから呼び出す
        public void OnAnimationComplete()
        {
            _animationCompletionSource?.TrySetResult();
        }

        async UniTask WaitForAnimationCompleteAsync(CancellationToken cancellationToken)
        {
            _animationCompletionSource = new UniTaskCompletionSource();

            await UniTask.Yield(cancellationToken);

            var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);
            var completionTask = _animationCompletionSource.Task;

            await UniTask.WhenAny(completionTask, timeoutTask);

            _animationCompletionSource = null;
        }

        void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }
    }

    public abstract class BaseDialogView<TArgs> : BaseDialogView, IDialogWithArgs<TArgs>
        where TArgs : IDialogArgs
    {
        protected TArgs Args { get; private set; } = default!;

        public void Initialize(TArgs args)
        {
            Args = args;
            OnInitialize(args);
        }

        protected virtual void OnInitialize(TArgs args) { }
    }
}
