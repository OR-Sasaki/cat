using System;
using System.Collections.Generic;

namespace Home.State
{
    public class OutfitAssetState
    {
        readonly Dictionary<string, Cat.Character.Outfit> _loadedOutfits = new();

        public bool IsLoaded { get; set; }
        public event Action OnLoaded;

        public void Add(string name, Cat.Character.Outfit outfit)
        {
            _loadedOutfits[name] = outfit;
        }

        public Cat.Character.Outfit Get(string name)
        {
            return _loadedOutfits.TryGetValue(name, out var outfit) ? outfit : null;
        }

        public IReadOnlyDictionary<string, Cat.Character.Outfit> GetAll()
        {
            return _loadedOutfits;
        }

        public void NotifyLoaded()
        {
            IsLoaded = true;
            OnLoaded?.Invoke();
        }
    }
}
