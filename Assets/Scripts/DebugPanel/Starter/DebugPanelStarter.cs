#nullable enable

using DebugPanel.View;
using Root.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DebugPanel.Starter
{
    /// 全シーンで使えるデバッグボタンを RootScope 起動時に生成する
    /// RootScope での登録自体を UNITY_EDITOR / DEVELOPMENT_BUILD で切り替えるため、
    /// リリースビルドではこの Starter は動かずボタンも存在しない
    public class DebugPanelStarter : IStartable
    {
        const string CanvasObjectName = "DebugPanelCanvas";

        readonly IDialogService _dialogService;

        [Inject]
        public DebugPanelStarter(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public void Start()
        {
            var canvasObject = new GameObject(CanvasObjectName);
            Object.DontDestroyOnLoad(canvasObject);

            var view = canvasObject.AddComponent<DebugOpenButtonView>();
            view.Initialize(_dialogService);
        }
    }
}
