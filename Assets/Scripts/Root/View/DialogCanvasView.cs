#nullable enable

using Root.Service;
using UnityEngine;
using VContainer;

namespace Root.View
{
    [RequireComponent(typeof(Canvas))]
    public class DialogCanvasView : MonoBehaviour
    {
        [SerializeField] Canvas? _dialogCanvas;
        [SerializeField] BackdropView? _backdropView;

        /// シーンの IStartable は MonoBehaviour.Start より先に走るので、起動直後にダイアログを開けるよう注入時点で登録する
        [Inject]
        public void Init(DialogContainer dialogContainer)
        {
            if (_dialogCanvas == null)
            {
                Debug.LogError("[DialogCanvasView] DialogCanvas is not assigned.");
                return;
            }

            if (_backdropView == null)
            {
                Debug.LogError("[DialogCanvasView] BackdropView is not assigned.");
                return;
            }

            dialogContainer.SetCanvas(_dialogCanvas);
            dialogContainer.SetBackdrop(_backdropView);

            _backdropView.gameObject.SetActive(false);
        }

        void Reset()
        {
            _dialogCanvas = GetComponent<Canvas>();
            _backdropView = GetComponentInChildren<BackdropView>();
        }
    }
}
