#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        /// 開閉フェードの尺。旧 DialogOpen / DialogClose クリップと同じ 60fps 換算 5 フレーム
        const float FadeDuration = 0.0833f;

        [SerializeField] Button? _closeButton;
        [SerializeField] CanvasGroup? _canvasGroup;

        public event Action<DialogResult>? OnCloseRequested;

        Tween? _fadeTween;

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

        /// プレハブはアクティブ / alpha 1 で保存されているため、Instantiate した時点で
        /// 完成形が描画されうる。生成と同じフレームのうちに非表示へ落として 1 フレームの
        /// ちらつきを防ぐ
        public void PrepareForOpen()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();

            KillFadeTween();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
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
            SetInteractable(false);
            try
            {
                await PlayFadeAsync(1f, cancellationToken);
            }
            finally
            {
                SetInteractable(true);
            }
        }

        public async UniTask PlayCloseAnimationAsync(CancellationToken cancellationToken)
        {
            SetInteractable(false);
            await PlayFadeAsync(0f, cancellationToken);
        }

        async UniTask PlayFadeAsync(float endAlpha, CancellationToken cancellationToken)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            KillFadeTween();

            // OnKill は自然完了 (AutoKill) でも Kill でも必ず呼ばれるので待機の解除点にする
            var completionSource = new UniTaskCompletionSource();
            _fadeTween = _canvasGroup
                .DOFade(endAlpha, FadeDuration)
                .SetEase(Ease.Linear)
                // ダイアログの開閉は Time.timeScale の影響を受けないようにする
                .SetUpdate(true)
                .OnKill(() => completionSource.TrySetResult());

            using (cancellationToken.Register(KillFadeTween))
            {
                await completionSource.Task;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        void KillFadeTween()
        {
            if (_fadeTween == null)
            {
                return;
            }

            var tween = _fadeTween;
            _fadeTween = null;

            if (tween.IsActive())
            {
                tween.Kill();
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
            KillFadeTween();

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
