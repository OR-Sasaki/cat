#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPanel.View
{
    /// 画面左上に置く透明なデバッグボタン
    /// 見た目を持たないため Canvas ごとコードから生成する (DebugPanelStarter から呼ばれる)
    public class DebugOpenButtonView : MonoBehaviour
    {
        /// DialogCanvas (1000) とダイアログのバックドロップ (最前面ダイアログ - 1) より必ず下に置き、
        /// ダイアログが開いている間はデバッグボタンを押せないようにする
        const int CanvasSortingOrder = 998;

        static readonly Vector2 ReferenceResolution = new(1080f, 1920f);

        /// 透明でも raycast を奪うため、各シーンの左上 UI を塞がないサイズに抑える
        /// Shop の戻るボタン (上端から 100px 以降) と History の戻るボタン (同 90px 以降) は完全に回避できる
        /// Home の Closet / Redecorate 中の戻るボタン (x 33..172, 上端から 34..174) だけは左上角が重なるが、
        /// 残りの領域で操作できる
        static readonly Vector2 ButtonSize = new(90f, 90f);

        IDialogService _dialogService = null!;

        /// HasOpenDialog は Addressables のロード完了後に true になるため、
        /// ロード中の連打で二重に開くのを自前のフラグで防ぐ
        bool _isPanelOpen;

        public void Initialize(IDialogService dialogService)
        {
            _dialogService = dialogService;
            Build();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            gameObject.AddComponent<GraphicRaycaster>();

            // alpha 0 の Image でも raycastTarget が true なら入力は拾える
            var image = DebugUiFactory.CreateImage("OpenButton", transform, new Color(1f, 1f, 1f, 0f));
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = ButtonSize;
            rect.anchoredPosition = Vector2.zero;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClicked);
        }

        void OnClicked()
        {
            OpenDebugPanelAsync(destroyCancellationToken).Forget();
        }

        async UniTaskVoid OpenDebugPanelAsync(CancellationToken cancellationToken)
        {
            if (_isPanelOpen || _dialogService.HasOpenDialog)
            {
                return;
            }

            _isPanelOpen = true;
            try
            {
                await _dialogService.OpenAsync<DebugPanelDialog>(cancellationToken);
            }
            finally
            {
                _isPanelOpen = false;
            }
        }
    }
}
