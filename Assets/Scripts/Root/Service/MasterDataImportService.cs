using System;
using System.Collections.Generic;
using System.Linq;
using Root.State;
using Shop.RewardAd;
using Shop.State;
using UnityEngine;

namespace Root.Service
{
    public class MasterDataImportService
    {
        /// outfits.csv / furnitures.csv の display_name 列 (0 始まり)
        const int DisplayNameColumnIndex = 3;

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
                var name = columns[2].Trim();
                return new Outfit
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = name,
                    DisplayName = ResolveDisplayName(columns, name)
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
                var name = columns[2].Trim();
                return new Furniture
                {
                    Id = uint.Parse(columns[0].Trim()),
                    Type = columns[1].Trim(),
                    Name = name,
                    DisplayName = ResolveDisplayName(columns, name)
                };
            }).ToArray();
        }

        /// outfits.csv / furnitures.csv の display_name 列を読む。
        /// 列ごと無い / 空欄の場合はアセット名 (name 列) で代替する
        static string ResolveDisplayName(string[] columns, string fallback)
        {
            if (columns.Length <= DisplayNameColumnIndex) return fallback;

            var displayName = columns[DisplayNameColumnIndex].Trim();
            return string.IsNullOrEmpty(displayName) ? fallback : displayName;
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
                    if (!ShopProductCsvParser.TryParseLine(line, out var row, out var error))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: {error}, skipping line: {line}");
                        continue;
                    }

                    if (!Enum.TryParse<ItemType>(row.ItemTypeRaw, ignoreCase: true, out var itemType)
                        || !Enum.IsDefined(typeof(ItemType), itemType))
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: invalid item_type, skipping line: {line}");
                        continue;
                    }

                    CurrencyType currencyType;
                    if (string.Equals(row.CurrencyTypeRaw, "yarn", StringComparison.OrdinalIgnoreCase))
                    {
                        currencyType = CurrencyType.Yarn;
                    }
                    else if (string.Equals(row.CurrencyTypeRaw, "reward_ad", StringComparison.OrdinalIgnoreCase))
                    {
                        currencyType = CurrencyType.RewardAd;
                    }
                    else
                    {
                        Debug.LogWarning($"[MasterDataImportService] shop_products: unsupported currency_type '{row.CurrencyTypeRaw}', skipping line: {line}");
                        continue;
                    }

                    products.Add(new ShopProduct(
                        row.Id, row.Name, itemType, row.ItemId, row.Price, currencyType, row.Amount, row.DailyCap));
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
