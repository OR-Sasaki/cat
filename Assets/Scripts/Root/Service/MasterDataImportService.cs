using System;
using System.Collections.Generic;
using System.Linq;
using Root.State;
using Shop.State;
using UnityEngine;

namespace Root.Service
{
    public class MasterDataImportService
    {
        readonly MasterDataState _masterDataState;

        /// Import 完了時に 1 度だけ発火する (Import は冪等で再発火しない)
        public event Action Imported;

        public MasterDataImportService(MasterDataState masterDataState)
        {
            _masterDataState = masterDataState;
        }

        public void Import()
        {
            if (_masterDataState.IsImported) return;

            ImportOutfits();
            ImportFurnitures();
            ImportShopProducts();

            _masterDataState.IsImported = true;
            Imported?.Invoke();
        }

        void ImportOutfits()
        {
            var csv = Resources.Load<TextAsset>("outfits");
            if (csv is null)
            {
                Debug.LogError("[MasterDataImportService] outfit.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _masterDataState.Outfits = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new Outfit
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = columns[2].Trim()
                };
            }).ToArray();
        }

        void ImportFurnitures()
        {
            var csv = Resources.Load<TextAsset>("furnitures");
            if (csv is null)
            {
                Debug.LogError("[MasterDataImportService] furnitures.csv not found");
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            _masterDataState.Furnitures = lines.Select(line =>
            {
                var columns = line.Split(',');
                return new Furniture
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = columns[2].Trim()
                };
            }).ToArray();
        }

        void ImportShopProducts()
        {
            var csv = Resources.Load<TextAsset>("shop_products");
            if (csv == null)
            {
                Debug.LogError("[MasterDataImportService] shop_products.csv not found");
                _masterDataState.ShopProducts = Array.Empty<ShopProduct>();
                return;
            }

            try
            {
                var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
                var products = new List<ShopProduct>();
                foreach (var line in lines)
                {
                    var columns = line.Split(',');
                    if (columns.Length < 6)
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid column count, skipping line: {line}");
                        continue;
                    }

                    if (!uint.TryParse(columns[0].Trim(), out var id))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid id, skipping line: {line}");
                        continue;
                    }

                    var name = columns[1].Trim();

                    if (!Enum.TryParse<ItemType>(columns[2].Trim(), ignoreCase: true, out var itemType))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid item_type, skipping line: {line}");
                        continue;
                    }

                    if (!uint.TryParse(columns[3].Trim(), out var itemId))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid item_id, skipping line: {line}");
                        continue;
                    }

                    if (!int.TryParse(columns[4].Trim(), out var price))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid price, skipping line: {line}");
                        continue;
                    }

                    var currencyTypeRaw = columns[5].Trim();
                    CurrencyType currencyType;
                    if (string.Equals(currencyTypeRaw, "yarn", StringComparison.OrdinalIgnoreCase))
                    {
                        currencyType = CurrencyType.Yarn;
                    }
                    else if (string.Equals(currencyTypeRaw, "reward_ad", StringComparison.OrdinalIgnoreCase))
                    {
                        currencyType = CurrencyType.RewardAd;
                    }
                    else
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: unsupported currency_type '{currencyTypeRaw}', skipping line: {line}");
                        continue;
                    }

                    products.Add(new ShopProduct(id, name, itemType, itemId, price, currencyType));
                }

                _masterDataState.ShopProducts = products.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MasterDataImportService] shop_products parse failed: {ex.Message}");
                _masterDataState.ShopProducts = Array.Empty<ShopProduct>();
            }
        }
    }
}
