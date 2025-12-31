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

        async UniTask WaitForAnimationCompleteAsync(CancellationToken cancellationToken)
        {
            await UniTask.Yield(cancellationToken);

            while (_animator != null)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.normalizedTime >= 1.0f)
                {
                    break;
                }
                await UniTask.Yield(cancellationToken);
            }
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
