#nullable enable

using Root.Service;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Root.View
{
    /// ボタンのクリック SE を既定から上書きする、または動的生成ボタンへ事前付与するための自己配線コンポーネント
    /// onClick に相乗りしないため、View 側の onClick.RemoveAllListeners() の影響を受けない
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSe : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] SeId _seId = SeId.Click;

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
            if (_seId == SeId.None)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // onClick が先に interactable を変更しうるため、押下時点の値で判定する
            if (_wasInteractableOnPointerDown == false)
            {
                return;
            }

            AudioServiceHandle.Current?.PlaySe(_seId);
        }
    }
}
