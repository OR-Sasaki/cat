using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Home.View
{
    /// リデコレートタブ 1 個分の見た目とクリック発火を担う presenter
    public class RedecorateTabItemView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _backgroundImage;
        [SerializeField] Image _iconImage;

        /// クリックコールバックを結線する。アイコンは Prefab 側の Inspector 設定を維持する
        public void Bind(UnityAction onClick)
        {
            if (_button is null)
            {
                Debug.LogError("[RedecorateTabItemView] _button is not assigned");
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
