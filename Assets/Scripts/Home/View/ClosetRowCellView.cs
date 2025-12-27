using Home.State;
using UnityEngine;
using UnityEngine.UI;

namespace Home.View
{
    public delegate void ClosetCellSelectedDelegate(ClosetRowCellView rowCellView);

    /// <summary>
    /// Closetスクローラーの行内の個々のセルビュー
    /// </summary>
    public class ClosetRowCellView : MonoBehaviour
    {
        [SerializeField] GameObject _container;
        [SerializeField] Image _outfitImage;
        [SerializeField] GameObject _selectedView;

        public int DataIndex { get; private set; }

        ClosetCellSelectedDelegate _selected;
        ClosetOutfitData _data;

        void OnDestroy()
        {
            if (_data != null)
            {
                _data.SelectedChanged -= OnSelectedChanged;
            }
        }

        public void SetData(int dataIndex, ClosetOutfitData data, ClosetCellSelectedDelegate selected)
        {
            _selected = selected;

            if (_data != null)
            {
                _data.SelectedChanged -= OnSelectedChanged;
            }

            DataIndex = dataIndex;
            _data = data;

            if (data is null)
            {
                _container.SetActive(false);
                return;
            }

            _container.SetActive(true);
            _outfitImage.sprite = data.Outfit.Thumbnail;

            _data.SelectedChanged += OnSelectedChanged;
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
