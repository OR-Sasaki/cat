#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Root.Scope;
using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Root.View
{
    public class DialogSampleButtonView : MonoBehaviour
    {
        [SerializeField] Button? _openDialogButton;

        IDialogService? _dialogService;

        void Start()
        {
            // Get DialogService from RootScope (no scene-specific scope needed)
            var rootScope = FindAnyObjectByType<RootScope>();
            if (rootScope == null)
            {
                Debug.LogError("[DialogSampleButtonView] RootScope not found in scene.");
                return;
            }

            _dialogService = rootScope.Container.Resolve<IDialogService>();

            if (_openDialogButton != null)
            {
                _openDialogButton.OnClickAsAsyncEnumerable(destroyCancellationToken).ForEachAwaitAsync(_ => OpenDialogAsync(), destroyCancellationToken);
            }
        }

        async UniTask OpenDialogAsync()
        {
            if (_dialogService == null) return;

            var result = await _dialogService.OpenAsync<SampleDialog>(destroyCancellationToken);
            Debug.Log($"[DialogSampleButtonView] SampleDialog result: {result}");
        }
    }
}
