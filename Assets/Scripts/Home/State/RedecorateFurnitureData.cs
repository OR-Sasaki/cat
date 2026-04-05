using Cat.Furniture;
using UnityEngine.Events;

namespace Home.State
{
    /// <summary>
    /// Redecoratefで使用するFurnitureのデータ
    /// </summary>
    public class RedecorateFurnitureData
    {
        public int UserFurnitureId { get; }
        public Furniture Furniture { get; }
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

        public RedecorateFurnitureData(int userFurnitureId, Furniture furniture)
        {
            UserFurnitureId = userFurnitureId;
            Furniture = furniture;
        }
    }
}