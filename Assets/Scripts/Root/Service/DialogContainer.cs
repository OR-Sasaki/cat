#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using Root.View;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    public class DialogContainer : ITickable, IDisposable
    {
        readonly DialogState _dialogState;
        readonly IObjectResolver _resolver;
        readonly ButtonSeAttacher _buttonSeAttacher;
        readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabCache = new();

        Canvas? _dialogCanvas;
        BackdropView? _backdropView;
        bool _isDisposed;

        public event Action? OnBackButtonPressed;

        [Inject]
        public DialogContainer(DialogState dialogState, IObjectResolver resolver, ButtonSeAttacher buttonSeAttacher)
        {
            _dialogState = dialogState;
            _resolver = resolver;
            _buttonSeAttacher = buttonSeAttacher;
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

            // Instantiate した時点でプレハブの見た目のまま描画されうるので、
            // 描画前 (同じフレーム内) に非表示へ落としてから初期化を進める
            dialogView.PrepareForOpen();

            // Inject dependencies into dynamically instantiated dialog
            _resolver.InjectGameObject(instance);
            _buttonSeAttacher.AttachToHierarchy(instance);

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

        /// 初回オープン時に Addressables のロードでフレームが伸び、開くアニメーションが飛ぶのを
        /// 避けるため、プレハブだけ先に読み込んでキャッシュしておく
        public async UniTask PreloadAsync(string addressableKey, CancellationToken cancellationToken)
        {
            await LoadPrefabAsync(addressableKey, cancellationToken);
        }

        async UniTask<GameObject> LoadPrefabAsync(string addressableKey, CancellationToken cancellationToken)
        {
            if (_prefabCache.TryGetValue(addressableKey, out var handle))
            {
                if (!handle.IsValid())
                {
                    _prefabCache.Remove(addressableKey);
                }
                else if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
            }

            if (!_prefabCache.TryGetValue(addressableKey, out handle))
            {
                handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
                // 先読みとオープンが同じキーで重なってもハンドルを二重に作らないよう、
                // await する前にキャッシュへ登録して進行中のロードを共有する
                _prefabCache[addressableKey] = handle;
            }

            // キャンセルされてもハンドルは解放しない。同じロードを待っている呼び出しが残りうるため、
            // 解放は Dispose にまとめる
            await handle.WithCancellation(cancellationToken);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                var error = exception?.Message ?? "Unknown error";
                var stackTrace = exception?.StackTrace ?? "";
                Debug.LogError($"[DialogContainer] Failed to load prefab '{addressableKey}': {error}\n{stackTrace}");
                _prefabCache.Remove(addressableKey);
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw new InvalidOperationException($"Failed to load dialog prefab: {addressableKey}");
            }

            return handle.Result;
        }

        public void UpdateBackdrop()
        {
            if (_backdropView == null)
            {
                return;
            }

            if (!_dialogState.HasDialog)
            {
                _backdropView.Hide();
                return;
            }

            if (_backdropView.Canvas != null && _dialogState.Current is { } currentDialog)
            {
                _backdropView.Canvas.sortingOrder = currentDialog.SortingOrder - 1;
            }

            _backdropView.Show(_dialogState.Count - 1);
        }

        public void SetBackdropInteractable(bool interactable)
        {
            if (_backdropView != null)
            {
                _backdropView.SetInteractable(interactable);
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

            if (_backdropView != null && !_backdropView.IsInteractable)
            {
                return;
            }

            // AndroidではEscapeキーがバックボタンにマッピングされている
            if (UnityEngine.InputSystem.Keyboard.current is { } keyboard &&
                keyboard.escapeKey.wasReleasedThisFrame)
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
