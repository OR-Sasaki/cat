#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using Root.View;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    public class DialogService : IDialogService, IDisposable
    {
        readonly DialogState _dialogState;
        readonly DialogContainer _dialogContainer;
        readonly CancellationTokenSource _cancellationTokenSource = new();

        bool _isDisposed;

        public bool HasOpenDialog => _dialogState.HasDialog;

        [Inject]
        public DialogService(DialogState dialogState, DialogContainer dialogContainer)
        {
            _dialogState = dialogState;
            _dialogContainer = dialogContainer;
            _dialogContainer.OnBackButtonPressed += HandleBackButtonPressed;
        }

        public async UniTask<DialogResult> OpenAsync<TDialog>(CancellationToken cancellationToken)
            where TDialog : BaseDialogView
        {
            var addressableKey = GetAddressableKey<TDialog>();
            return await OpenDialogInternalAsync<TDialog>(addressableKey, null, cancellationToken);
        }

        public async UniTask<DialogResult> OpenAsync<TDialog, TArgs>(TArgs args, CancellationToken cancellationToken)
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

            await using var registration = cancellationToken.Register(() => CloseDialogImmediate(dialogInstance));

            try
            {
                await dialogView.PlayOpenAnimationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CloseDialogImmediate(dialogInstance);
                throw;
            }

            return await dialogInstance.CompletionSource.Task;
        }

        void HandleCloseRequested(DialogInstance instance, DialogResult result)
        {
            CloseDialogFireAndForget(instance, result).Forget();
        }

        void HandleBackButtonPressed()
        {
            if (_dialogState.Current is { } currentDialog)
            {
                CloseDialogFireAndForget(currentDialog, DialogResult.Cancel).Forget();
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
                CloseWithParentFireAndForget(currentDialog, result).Forget();
            }
            else
            {
                CloseDialogFireAndForget(currentDialog, result).Forget();
            }
        }

        async UniTaskVoid CloseWithParentFireAndForget(DialogInstance targetDialog, DialogResult result)
        {
            try
            {
                var dialogsToClose = _dialogState.PopUntil(targetDialog);
                _dialogContainer.SetBackdropInteractable(false);

                foreach (var dialog in dialogsToClose)
                {
                    try
                    {
                        await dialog.View.PlayCloseAnimationAsync(_cancellationTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[DialogService] Error during close animation: {e.Message}\n{e.StackTrace}");
                    }

                    _dialogContainer.DestroyDialog(dialog);
                    dialog.CompletionSource.TrySetResult(result);
                }

                _dialogContainer.UpdateBackdrop();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogService] Error during CloseWithParent: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _dialogContainer.SetBackdropInteractable(true);
            }
        }

        async UniTaskVoid CloseDialogFireAndForget(DialogInstance instance, DialogResult result)
        {
            try
            {
                if (_dialogState.Current != instance)
                {
                    return;
                }

                _dialogState.Pop();
                _dialogContainer.SetBackdropInteractable(false);

                try
                {
                    await instance.View.PlayCloseAnimationAsync(_cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DialogService] Error during close animation: {e.Message}\n{e.StackTrace}");
                }

                _dialogContainer.DestroyDialog(instance);
                _dialogContainer.UpdateBackdrop();

                instance.CompletionSource.TrySetResult(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogService] Error during CloseDialog: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _dialogContainer.SetBackdropInteractable(true);
            }
        }

        void CloseDialogImmediate(DialogInstance instance)
        {
            if (_dialogState.Current != instance)
            {
                return;
            }

            _dialogState.Pop();
            _dialogContainer.DestroyDialog(instance);
            _dialogContainer.UpdateBackdrop();
            instance.CompletionSource.TrySetCanceled();
        }

        static string GetAddressableKey<TDialog>() where TDialog : BaseDialogView
        {
            return $"Dialogs/{typeof(TDialog).Name}.prefab";
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _dialogContainer.OnBackButtonPressed -= HandleBackButtonPressed;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
