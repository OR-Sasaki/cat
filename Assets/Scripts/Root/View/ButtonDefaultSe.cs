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
    public sealed class ButtonDefaultSe : MonoBehaviour, IPointerClickHandler
    {
        Button? _button;

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (_button!.IsInteractable() == false)
            {
                return;
            }

            AudioServiceHandle.Current?.PlaySe(SeId.Click);
        }
    }
}
