#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using Root.View;
using UnityEngine;

namespace Root.Service
{
    public class DialogService : IDialogService, IDisposable
    {
        readonly DialogState _dialogState;
        readonly DialogContainer _dialogContainer;

        DialogInstance? _activeDialogInstance;
        bool _isDisposed;

        public bool HasOpenDialog => _dialogState.HasDialog;

        public DialogService(DialogState dialogState, DialogContainer dialogContainer)
        {
            _dialogState = dialogState;
            _dialogContainer = dialogContainer;
            _dialogContainer.OnBackButtonPressed += HandleBackButtonPressed;
        }

        public async UniTask<DialogResult> OpenAsync<TDialog>(CancellationToken cancellationToken = default)
            where TDialog : BaseDialogView
        {
            var addressableKey = GetAddressableKey<TDialog>();
            return await OpenDialogInternalAsync<TDialog>(addressableKey, null, cancellationToken);
        }

        public async UniTask<DialogResult> OpenAsync<TDialog, TArgs>(TArgs args, CancellationToken cancellationToken = default)
            where TDialog : BaseDialogView, IDialogWithArgs<TArgs>
            where TArgs : IDialogArgs
        {
            var addressableKey = GetAddressableKey<TDialog>();
            return await OpenDialogInternalAsync<TDialog>(addressableKey, dialogView =>
            {
                if (dialogView is IDialogWithArgs<TArgs> dialogWithArgs)
                {
                    dialogWithArgs.Initialize(args);
                }
            }, cancellationToken);
        }

        async UniTask<DialogResult> OpenDialogInternalAsync<TDialog>(
            string addressableKey,
            Action<BaseDialogView>? initializeAction,
            CancellationToken cancellationToken)
            where TDialog : BaseDialogView
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DialogService));
            }

            var sortingOrder = _dialogState.GetNextSortingOrder();

            BaseDialogView dialogView;
            try
            {
                dialogView = await _dialogContainer.LoadAndInstantiateAsync(
                    addressableKey,
                    sortingOrder,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogService] Failed to open dialog '{addressableKey}': {e.Message}\n{e.StackTrace}");
                throw;
            }

            initializeAction?.Invoke(dialogView);

            var dialogInstance = new DialogInstance(addressableKey, dialogView, sortingOrder);
            _dialogState.Push(dialogInstance);
            _dialogContainer.UpdateBackdrop();

            dialogView.OnCloseRequested += result => HandleCloseRequested(dialogInstance, result);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

            linkedToken.Register(() =>
            {
                if (_dialogState.Current == dialogInstance)
                {
                    CloseDialogAsync(dialogInstance, DialogResult.Cancel).Forget();
                }
            });

            try
            {
                await dialogView.PlayOpenAnimationAsync(linkedToken);
            }
            catch (OperationCanceledException)
            {
                await CloseDialogImmediateAsync(dialogInstance);
                throw;
            }

            try
            {
                return await dialogInstance.CompletionSource.Task;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        void HandleCloseRequested(DialogInstance instance, DialogResult result)
        {
            CloseDialogAsync(instance, result).Forget();
        }

        void HandleBackButtonPressed()
        {
            if (_dialogState.Current is { } currentDialog)
            {
                CloseDialogAsync(currentDialog, DialogResult.Cancel).Forget();
            }
        }

        public void Close(DialogResult result, bool closeParent = false)
        {
            if (_dialogState.Current is not { } currentDialog)
            {
                Debug.LogWarning("[DialogService] Close called but no dialog is open.");
                return;
            }

            if (closeParent)
            {
                CloseWithParentAsync(currentDialog, result).Forget();
            }
            else
            {
                CloseDialogAsync(currentDialog, result).Forget();
            }
        }

        async UniTask CloseWithParentAsync(DialogInstance targetDialog, DialogResult result)
        {
            var dialogsToClose = _dialogState.PopUntil(targetDialog);

            foreach (var dialog in dialogsToClose)
            {
                var dialogView = dialog.View as BaseDialogView;
                if (dialogView != null)
                {
                    try
                    {
                        await dialogView.PlayCloseAnimationAsync();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[DialogService] Error during close animation: {e.Message}\n{e.StackTrace}");
                    }
                }

                _dialogContainer.DestroyDialog(dialog);
                dialog.CompletionSource.TrySetResult(result);
            }

            _dialogContainer.UpdateBackdrop();
        }

        async UniTask CloseDialogAsync(DialogInstance instance, DialogResult result)
        {
            if (_dialogState.Current != instance)
            {
                return;
            }

            _dialogState.Pop();

            var dialogView = instance.View as BaseDialogView;
            if (dialogView != null)
            {
                try
                {
                    await dialogView.PlayCloseAnimationAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DialogService] Error during close animation: {e.Message}\n{e.StackTrace}");
                }
            }

            _dialogContainer.DestroyDialog(instance);
            _dialogContainer.UpdateBackdrop();

            instance.CompletionSource.TrySetResult(result);
        }

        async UniTask CloseDialogImmediateAsync(DialogInstance instance)
        {
            _dialogState.Pop();
            _dialogContainer.DestroyDialog(instance);
            _dialogContainer.UpdateBackdrop();
            instance.CompletionSource.TrySetCanceled();
            await UniTask.CompletedTask;
        }

        static string GetAddressableKey<TDialog>() where TDialog : BaseDialogView
        {
            return typeof(TDialog).Name;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _dialogContainer.OnBackButtonPressed -= HandleBackButtonPressed;
        }
    }
}
