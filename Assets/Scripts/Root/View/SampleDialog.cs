#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Root.View
{
    public class SampleDialog : BaseDialogView
    {
        [SerializeField] Button? _openConfirmButton;
        [SerializeField] Button? _openMessageButton;

        IDialogService? _dialogService;

        [Inject]
        public void Construct(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        protected override void Awake()
        {
            base.Awake();

            if (_openConfirmButton != null)
            {
                _openConfirmButton.OnClickAsAsyncEnumerable(destroyCancellationToken).ForEachAwaitAsync(_ => OnOpenConfirmButtonClickedAsync(), destroyCancellationToken);
            }

            if (_openMessageButton != null)
            {
                _openMessageButton.OnClickAsAsyncEnumerable(destroyCancellationToken).ForEachAwaitAsync(_ => OnOpenMessageButtonClickedAsync(), destroyCancellationToken);
            }
        }

        async UniTask OnOpenConfirmButtonClickedAsync()
        {
            if (_dialogService == null) return;

            var result = await _dialogService.OpenAsync<SampleConfirmDialog>(destroyCancellationToken);
            Debug.Log($"[SampleDialog] SampleConfirmDialog result: {result}");
        }

        async UniTask OnOpenMessageButtonClickedAsync()
        {
            if (_dialogService == null) return;

            var args = new SampleMessageDialogArgs("This is a message from SampleDialog");
            var result = await _dialogService.OpenAsync<SampleMessageDialog, SampleMessageDialogArgs>(args, destroyCancellationToken);
            Debug.Log($"[SampleDialog] SampleMessageDialog result: {result}");
        }
    }
}
