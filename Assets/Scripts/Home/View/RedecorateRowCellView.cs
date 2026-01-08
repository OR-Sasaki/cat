using Home.State;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Home.View
{
    /// <summary>
    /// Redecorateスクローラーの行内の個々のセルビュー
    /// </summary>
    public class RedecorateRowCellView : MonoBehaviour
    {
        [SerializeField] GameObject _container;
        [SerializeField] Image _furnitureImage;
        [SerializeField] GameObject _selectedView;

        public int DataIndex { get; private set; }

        UnityEvent<RedecorateRowCellView> _selected;
        RedecorateFurnitureData _data;

        void OnDestroy()
        {
            if (_data != null)
            {
                _data.SelectedChanged.RemoveListener(OnSelectedChanged);
            }
        }

        public void SetData(int dataIndex, RedecorateFurnitureData data, UnityEvent<RedecorateRowCellView> selected)
        {
            _selected = selected;

            if (_data != null)
            {
                _data.SelectedChanged.RemoveListener(OnSelectedChanged);
            }

            DataIndex = dataIndex;
            _data = data;

            if (data is null)
            {
                _container.SetActive(false);
                return;
            }

            _container.SetActive(true);
            _furnitureImage.sprite = data.Furniture.Thumbnail;

            _data.SelectedChanged.AddListener(OnSelectedChanged);
            OnSelectedChanged(_data.Selected);
        }

        void OnSelectedChanged(bool selected)
        {
            _selectedView.SetActive(selected);
        }

        /// <summary>
        /// UIボタンのクリックイベントから呼び出される
        /// </summary>
        public void OnSelected()
        {
            _selected?.Invoke(this);
        }
    }
}