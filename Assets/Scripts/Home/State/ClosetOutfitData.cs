using Cat.Character;
using UnityEngine.Events;

namespace Home.State
{
    /// <summary>
    /// Closetで使用するOutfitのデータ
    /// </summary>
    public class ClosetOutfitData
    {
        public Outfit Outfit { get; }
        public readonly UnityEvent<bool> SelectedChanged = new();

        bool _selected;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                SelectedChanged.Invoke(_selected);
            }
        }

        public ClosetOutfitData(Outfit outfit)
        {
            Outfit = outfit;
        }
    }
}


