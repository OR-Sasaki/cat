#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    public enum DialogResult
    {
        Ok,
        Cancel,
        Close
    }

    public interface IDialogArgs { }

    public interface IDialogWithArgs<in TArgs> where TArgs : IDialogArgs
    {
        void Initialize(TArgs args);
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseDialogView : MonoBehaviour
    {
        [SerializeField] Animator? _animator;
        [SerializeField] Button? _closeButton;
        [SerializeField] CanvasGroup? _canvasGroup;

        static readonly int OpenState = Animator.StringToHash("Open");
        static readonly int CloseState = Animator.StringToHash("Close");

        public event Action<DialogResult>? OnCloseRequested;

        protected virtual void Reset()
        {
            _animator = GetComponent<Animator>();
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

        public async UniTask PlayOpenAnimationAsync(CancellationToken cancellationToken)
        {
            if (_animator == null)
            {
                return;
            }

            SetInteractable(false);
            _animator.Play(OpenState);
            await WaitForAnimationCompleteAsync(cancellationToken);
            SetInteractable(true);
        }

        public async UniTask PlayCloseAnimationAsync(CancellationToken cancellationToken)
        {
            if (_animator == null)
            {
                return;
            }

            SetInteractable(false);
            _animator.Play(CloseState);
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
