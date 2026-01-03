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

        DialogContainer? _dialogContainer;

        [Inject]
        public void Init(DialogContainer dialogContainer)
        {
            _dialogContainer = dialogContainer;
        }

        void Reset()
        {
            _dialogCanvas = GetComponent<Canvas>();
        }

        void Start()
        {
            if (_dialogContainer == null)
            {
                Debug.LogError("[DialogCanvasView] DialogContainer is not injected.");
                return;
            }

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

            _dialogContainer.SetCanvas(_dialogCanvas);
            _dialogContainer.SetBackdrop(_backdropView);

            _backdropView.gameObject.SetActive(false);
        }
    }
}
