using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Home.View
{
    /// 小タブ 1 個分の見た目とクリック発火を担う presenter
    public class ClosetMinorTabItemView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _backgroundImage;
        [SerializeField] Image _iconImage;

        /// アイコンとクリックコールバックを結線する
        public void Bind(Sprite icon, UnityAction onClick)
        {
            if (_iconImage is not null)
            {
                _iconImage.sprite = icon;
            }

            if (_button is null)
            {
                Debug.LogError("[ClosetMinorTabItemView] _button is not assigned");
                return;
            }

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
        }

        /// 選択中のみ背景画像を表示する
        public void SetSelected(bool selected)
        {
            if (_backgroundImage is not null)
            {
                _backgroundImage.color = selected ? Color.white : Color.clear;
            }
        }

        void OnDestroy()
        {
            if (_button is not null)
            {
                _button.onClick.RemoveAllListeners();
            }
        }
    }
}
