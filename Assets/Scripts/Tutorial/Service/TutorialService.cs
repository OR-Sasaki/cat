#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Tutorial.State;
using Tutorial.View;
using UnityEngine;
using VContainer;

namespace Tutorial.Service
{
    public sealed class TutorialService : ITutorialService
    {
        readonly TutorialRegistry _registry;
        readonly PlayerPrefsService _playerPrefsService;
        readonly IDialogService _dialogService;
        readonly TutorialSaveData _data;

        [Inject]
        public TutorialService(
            TutorialRegistry registry,
            PlayerPrefsService playerPrefsService,
            IDialogService dialogService)
        {
            _registry = registry;
            _playerPrefsService = playerPrefsService;
            _dialogService = dialogService;
            _data = _playerPrefsService.Load<TutorialSaveData>(PlayerPrefsKey.Tutorial) ?? new TutorialSaveData();
        }

        public async UniTask ShowIfFirstTimeAsync(TutorialId id, CancellationToken cancellationToken)
        {
            var key = id.ToString();
            if (_data.shownIds.Contains(key))
            {
                return;
            }

            var pages = _registry.FindPages(id);
            if (pages == null)
            {
                Debug.LogWarning($"[TutorialService] ページ定義がありません: {id}");
                return;
            }

            // シーンの IStartable は MonoBehaviour.Start より先に走り、DialogCanvasView は Start でキャンバスを登録する。
            // Update まで待つと Start の後かつ入力処理の前に着地する
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            await _dialogService.OpenAsync<TutorialDialog, TutorialDialogArgs>(new TutorialDialogArgs(pages), cancellationToken);

            _data.shownIds.Add(key);
            _playerPrefsService.Save(PlayerPrefsKey.Tutorial, _data);
        }
    }
}
