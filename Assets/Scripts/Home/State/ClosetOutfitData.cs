using Cat.Character;

namespace Home.State
{
    public delegate void SelectedChangedDelegate(bool val);

    /// <summary>
    /// Closetで使用するOutfitのデータ
    /// </summary>
    public class ClosetOutfitData
    {
        public Outfit Outfit { get; }
        public SelectedChangedDelegate SelectedChanged;

        bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                SelectedChanged?.Invoke(_selected);
            }
        }

        public ClosetOutfitData(Outfit outfit)
        {
            Outfit = outfit;
        }
    }
}

