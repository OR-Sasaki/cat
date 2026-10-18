#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tutorial.Service;
using Tutorial.State;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tutorial.Starter
{
    /// シーン開始時に、そのシーンの初回チュートリアルを表示する起動フック
    public sealed class TutorialStarter : IStartable, IDisposable
    {
        readonly ITutorialService _tutorialService;
        readonly TutorialId _tutorialId;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public TutorialStarter(ITutorialService tutorialService, TutorialId tutorialId)
        {
            _tutorialService = tutorialService;
            _tutorialId = tutorialId;
        }

        public void Start()
        {
            ShowAsync(_cts.Token).Forget();
        }

        async UniTaskVoid ShowAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _tutorialService.ShowIfFirstTimeAsync(_tutorialId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[TutorialStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
