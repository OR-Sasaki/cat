#nullable enable

using Root.Service;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Root.View
{
    /// ButtonSeAttacher が付与する、既定クリック SE を自前で再生するコンポーネント
    /// onClick に相乗りしないため、View 側の onClick.RemoveAllListeners() の影響を受けない
    [RequireComponent(typeof(Button))]
    public sealed class ButtonDefaultSe : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        Button? _button;
        bool _wasInteractableOnPointerDown;

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _wasInteractableOnPointerDown = _button!.IsInteractable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // onClick が先に interactable を変更しうるため、押下時点の値で判定する
            if (_wasInteractableOnPointerDown == false)
            {
                return;
            }

            AudioServiceHandle.Current?.PlaySe(SeId.Click);
        }
    }
}
