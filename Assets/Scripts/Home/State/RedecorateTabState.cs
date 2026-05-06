using Cat.Furniture;
using UnityEngine.Events;

namespace Home.State
{
    /// リデコレート画面のタブ選択状態 (`FurnitureType`) を保持する SSOT。差分時のみ `Changed` を発火する
    public class RedecorateTabState
    {
        public const FurnitureType Default = FurnitureType.Floor;

        public readonly UnityEvent<FurnitureType> Changed = new();

        FurnitureType _current = Default;

        public FurnitureType Current => _current;

        /// 選択中タブを書き込む。値が変化したときのみ通知を発火する
        public void WriteCurrent(FurnitureType value)
        {
            if (_current == value) return;
            _current = value;
            Changed.Invoke(value);
        }
    }
}
