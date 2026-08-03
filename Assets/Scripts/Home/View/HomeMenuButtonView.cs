#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Menu.View;
using Root.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Home.View
{
    /// ホーム右上のメニューボタン。押下でメニューダイアログを開く
    public class HomeMenuButtonView : MonoBehaviour
    {
        [SerializeField] Button _menuButton = null!;

        IDialogService _dialogService = null!;

        /// HasOpenDialog は Addressables のロード完了後に true になるため、
        /// ロード中の連打で二重に開くのを自前のフラグで防ぐ
        bool _isMenuOpen;

        [Inject]
        public void Init(IDialogService dialogService)
        {
            _dialogService = dialogService;
            _menuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        void OnMenuButtonClicked()
        {
            OpenMenuDialogAsync(destroyCancellationToken).Forget();
        }

        async UniTaskVoid OpenMenuDialogAsync(CancellationToken cancellationToken)
        {
            if (_isMenuOpen || _dialogService.HasOpenDialog)
            {
                return;
            }

            _isMenuOpen = true;
            try
            {
                await _dialogService.OpenAsync<MenuDialog>(cancellationToken);
            }
            finally
            {
                _isMenuOpen = false;
            }
        }

        void OnDestroy()
        {
            _menuButton.onClick.RemoveListener(OnMenuButtonClicked);
        }
    }
}
