using System;
using System.Collections.Generic;

namespace Home.State
{
    public class FurnitureAssetState
    {
        readonly Dictionary<string, Cat.Furniture.Furniture> _loadedFurnitures = new();

        public bool IsLoaded { get; set; }
        public event Action OnLoaded;

        public void Add(string name, Cat.Furniture.Furniture furniture)
        {
            _loadedFurnitures[name] = furniture;
        }

        public Cat.Furniture.Furniture Get(string name)
        {
            return _loadedFurnitures.TryGetValue(name, out var furniture) ? furniture : null;
        }

        public IReadOnlyDictionary<string, Cat.Furniture.Furniture> GetAll()
        {
            return _loadedFurnitures;
        }

        public void NotifyLoaded()
        {
            IsLoaded = true;
            OnLoaded?.Invoke();
        }
    }
}