#nullable enable
using System;
using System.Collections.Generic;

namespace Root.State
{
    /// Addressables からロードした Outfit アセットをシーン横断で保持するキャッシュ
    /// Home で着替えた見た目を Timer など他シーンでも再現するため Root スコープに常駐させる
    /// このネームスペースには マスタデータの Root.State.Outfit があるため、アセット側は常に完全修飾する
    public class OutfitAssetState
    {
        readonly Dictionary<string, Cat.Character.Outfit> _loadedOutfits = new();

        /// マスタ全件のロードが完了しているか
        /// 全件を必要とするのはクローゼットの一覧表示のみで、装備分だけのロードでは true にならない
        public bool IsAllLoaded { get; private set; }

        /// 全件ロード完了通知
        /// Root スコープに常駐するため、購読側はシーン破棄時に必ず解除する
        public event Action? OnAllLoaded;

        public void Add(string name, Cat.Character.Outfit outfit)
        {
            _loadedOutfits[name] = outfit;
        }

        public bool Contains(string name)
        {
            return _loadedOutfits.ContainsKey(name);
        }

        public Cat.Character.Outfit? Get(string name)
        {
            return _loadedOutfits.TryGetValue(name, out var outfit) ? outfit : null;
        }

        public IReadOnlyDictionary<string, Cat.Character.Outfit> GetAll()
        {
            return _loadedOutfits;
        }

        public void NotifyAllLoaded()
        {
            IsAllLoaded = true;
            OnAllLoaded?.Invoke();
        }
    }
}
