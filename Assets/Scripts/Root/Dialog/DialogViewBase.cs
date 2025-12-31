#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Root.Dialog
{
    public abstract class DialogViewBase : MonoBehaviour
    {
        [SerializeField] Animator? _animator;
        [SerializeField] Button? _closeButton;

        static readonly int OpenTrigger = Animator.StringToHash("Open");
        static readonly int CloseTrigger = Animator.StringToHash("Close");

        IDialogCloser? _dialogCloser;
        int _closeCount = 1;

        public void Initialize(IDialogCloser dialogCloser)
        {
            if (dialogCloser == null)
            {
                throw new System.ArgumentNullException(nameof(dialogCloser));
            }

            _dialogCloser = dialogCloser;

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        void OnCloseButtonClicked()
        {
            Close(DialogResult.Close);
        }

        protected void SetCloseCount(int count)
        {
            _closeCount = Mathf.Max(1, count);
        }

        protected void Close(DialogResult result)
        {
            _dialogCloser?.Close(this, result, _closeCount);
        }

        public virtual UniTask PlayOpenAnimation()
        {
            if (_animator == null)
            {
                return UniTask.CompletedTask;
            }

            _animator.SetTrigger(OpenTrigger);
            return WaitForAnimationComplete();
        }

        public virtual UniTask PlayCloseAnimation()
        {
            if (_animator == null)
            {
                return UniTask.CompletedTask;
            }

            _animator.SetTrigger(CloseTrigger);
            return WaitForAnimationComplete();
        }

        async UniTask WaitForAnimationComplete()
        {
            if (_animator == null)
            {
                return;
            }

            // アニメーション開始を1フレーム待つ
            await UniTask.Yield();

            // アニメーション完了を待つ
            while (_animator != null)
            {
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.normalizedTime >= 1.0f)
                {
                    break;
                }
                await UniTask.Yield();
            }
        }

        void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }
    }

    public abstract class DialogViewBase<TArgs> : DialogViewBase, IDialogWithArgs<TArgs>
        where TArgs : IDialogArgs
    {
        protected TArgs Args { get; private set; } = default!;

        public void Initialize(TArgs args)
        {
            Args = args;
            OnArgsSet(args);
        }

        protected virtual void OnArgsSet(TArgs args) { }
    }
}
