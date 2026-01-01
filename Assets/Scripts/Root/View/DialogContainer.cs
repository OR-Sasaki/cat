#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Root.View
{
    public class DialogContainer : ITickable, IDisposable
    {
        readonly DialogState _dialogState;
        readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabCache = new();

        Canvas? _dialogCanvas;
        BackdropView? _backdropView;
        bool _isDisposed;

        public event Action? OnBackButtonPressed;

        public DialogContainer(DialogState dialogState)
        {
            _dialogState = dialogState;
        }

        public void SetCanvas(Canvas canvas)
        {
            _dialogCanvas = canvas;
        }

        public void SetBackdrop(BackdropView backdropView)
        {
            _backdropView = backdropView;
            _backdropView.OnClicked += OnBackdropClicked;
        }

        void OnBackdropClicked()
        {
            OnBackButtonPressed?.Invoke();
        }

        public async UniTask<BaseDialogView> LoadAndInstantiateAsync(
            string addressableKey,
            int sortingOrder,
            CancellationToken cancellationToken)
        {
            if (_dialogCanvas == null)
            {
                throw new InvalidOperationException("[DialogContainer] Canvas is not set.");
            }

            var prefab = await LoadPrefabAsync(addressableKey, cancellationToken);
            var instance = UnityEngine.Object.Instantiate(prefab, _dialogCanvas.transform);

            var dialogView = instance.GetComponent<BaseDialogView>();
            if (dialogView == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"[DialogContainer] Prefab '{addressableKey}' does not have a BaseDialogView component.");
            }

            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = instance.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            var raycaster = instance.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                instance.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            instance.SetActive(true);

            return dialogView;
        }

        async UniTask<GameObject> LoadPrefabAsync(string addressableKey, CancellationToken cancellationToken)
        {
            if (_prefabCache.TryGetValue(addressableKey, out var cachedHandle))
            {
                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return cachedHandle.Result;
                }
                _prefabCache.Remove(addressableKey);
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);

            try
            {
                await handle.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                var error = exception?.Message ?? "Unknown error";
                var stackTrace = exception?.StackTrace ?? "";
                Debug.LogError($"[DialogContainer] Failed to load prefab '{addressableKey}': {error}\n{stackTrace}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw new InvalidOperationException($"Failed to load dialog prefab: {addressableKey}");
            }

            _prefabCache[addressableKey] = handle;
            return handle.Result;
        }

        public void UpdateBackdrop()
        {
            if (_backdropView == null)
            {
                return;
            }

            var hasDialog = _dialogState.HasDialog;
            _backdropView.gameObject.SetActive(hasDialog);

            if (hasDialog)
            {
                _backdropView.SetAlphaByStackIndex(_dialogState.Count - 1);

                var backdropCanvas = _backdropView.GetComponent<Canvas>();
                if (backdropCanvas != null && _dialogState.Current != null)
                {
                    backdropCanvas.sortingOrder = _dialogState.Current.SortingOrder - 1;
                }
            }
        }

        public void DestroyDialog(DialogInstance instance)
        {
            if (instance.View != null)
            {
                UnityEngine.Object.Destroy(instance.View.gameObject);
            }
        }

        public void Tick()
        {
            if (_isDisposed)
            {
                return;
            }

            if (!_dialogState.HasDialog)
            {
                return;
            }

            // AndroidではEscapeキーがバックボタンにマッピングされている
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackButtonPressed?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_backdropView != null)
            {
                _backdropView.OnClicked -= OnBackdropClicked;
            }

            foreach (var handle in _prefabCache.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _prefabCache.Clear();
        }
    }
}
