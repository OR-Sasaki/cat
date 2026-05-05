using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Home.View
{
    /// 大タブ 1 個分の見た目とクリック発火を担う presenter
    public class ClosetMajorTabItemView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _backgroundImage;
        [SerializeField] Sprite _selectedBackgroundSprite;
        [SerializeField] Sprite _unselectedBackgroundSprite;
        [SerializeField] Image _iconImage;
        [SerializeField] Sprite _selectedIconSprite;
        [SerializeField] Sprite _unselectedIconSprite;

        /// 親コンテナから受け取ったクリックコールバックをボタンに結線する
        public void Bind(UnityAction onClick)
        {
            if (_button is null)
            {
                Debug.LogError("[ClosetMajorTabItemView] _button is not assigned");
                return;
            }

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
        }

        /// 選択中・非選択の背景／アイコンスプライトを切り替える
        public void SetSelected(bool selected)
        {
            if (_backgroundImage is not null)
            {
                _backgroundImage.sprite = selected ? _selectedBackgroundSprite : _unselectedBackgroundSprite;
            }

            if (_iconImage is not null)
            {
                _iconImage.sprite = selected ? _selectedIconSprite : _unselectedIconSprite;
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
