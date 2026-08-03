#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Menu.View;
using Root.View;
using TimerSetting.View;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Root.Service
{
    /// 起動時にダイアログのプレハブを Addressables から先読みしておく起動フック。
    /// 初回オープン時にロードでフレームが伸び、開くフェードが飛ぶのを防ぐ
    public sealed class DialogPreloader : IStartable, IDisposable
    {
        readonly IDialogService _dialogService;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public DialogPreloader(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public void Start()
        {
            PreloadAsync(_cts.Token).Forget();
        }

        async UniTaskVoid PreloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.WhenAll(
                    _dialogService.PreloadAsync<MenuDialog>(cancellationToken),
                    _dialogService.PreloadAsync<TimerSettingDialog>(cancellationToken),
                    _dialogService.PreloadAsync<CommonMessageDialog>(cancellationToken),
                    _dialogService.PreloadAsync<CommonConfirmDialog>(cancellationToken));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                // 先読みに失敗してもオープン時に再ロードされるため、警告に留める
                Debug.LogWarning($"[DialogPreloader] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
