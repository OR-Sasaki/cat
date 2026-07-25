#nullable enable

using System;
using System.Threading;
using Cat.Character;
using Cysharp.Threading.Tasks;
using Root.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Root.Starter
{
    /// シーン内の CharacterView に装備中の見た目を適用する起動フック
    /// CharacterView を持つ SceneScope から登録する
    public sealed class CharacterOutfitStarter : IStartable, IDisposable
    {
        readonly CharacterView _characterView;
        readonly CharacterOutfitService _characterOutfitService;
        readonly CancellationTokenSource _cts = new();

        [Inject]
        public CharacterOutfitStarter(
            CharacterView characterView,
            CharacterOutfitService characterOutfitService)
        {
            _characterView = characterView;
            _characterOutfitService = characterOutfitService;
        }

        public void Start()
        {
            ApplyAsync(_cts.Token).Forget();
        }

        async UniTaskVoid ApplyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _characterOutfitService.ApplyEquippedAsync(_characterView, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // シーン破棄によるキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[CharacterOutfitStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
