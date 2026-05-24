#nullable enable

namespace Shop.RewardAd
{
    /// shop_products.csv 1 行分の構造的・数値的パースを担う純粋ロジック。
    /// item_type / currency_type の enum 解釈は呼び出し側 (MasterDataImportService) が行う。
    public static class ShopProductCsvParser
    {
        public const int RequiredColumnCount = 8;

        public static bool TryParseLine(string line, out ShopProductCsvRow row, out string error)
        {
            row = default;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                error = "empty line";
                return false;
            }

            var columns = line.Split(',');
            if (columns.Length < RequiredColumnCount)
            {
                error = $"invalid column count ({columns.Length})";
                return false;
            }

            if (!uint.TryParse(columns[0].Trim(), out var id))
            {
                error = "invalid id";
                return false;
            }

            var name = columns[1].Trim();
            var itemTypeRaw = columns[2].Trim();

            if (!uint.TryParse(columns[3].Trim(), out var itemId))
            {
                error = "invalid item_id";
                return false;
            }

            if (!int.TryParse(columns[4].Trim(), out var price))
            {
                error = "invalid price";
                return false;
            }

            var currencyTypeRaw = columns[5].Trim();

            var amountRaw = columns[6].Trim();
            int amount;
            if (string.IsNullOrEmpty(amountRaw))
            {
                amount = 1;
            }
            else if (!int.TryParse(amountRaw, out amount))
            {
                error = "invalid amount";
                return false;
            }

            var dailyCapRaw = columns[7].Trim();
            int? dailyCap;
            if (string.IsNullOrEmpty(dailyCapRaw))
            {
                dailyCap = null;
            }
            else if (int.TryParse(dailyCapRaw, out var parsedDailyCap))
            {
                dailyCap = parsedDailyCap;
            }
            else
            {
                error = "invalid daily_cap";
                return false;
            }

            row = new ShopProductCsvRow(id, name, itemTypeRaw, itemId, price, currencyTypeRaw, amount, dailyCap);
            return true;
        }
    }

    public readonly struct ShopProductCsvRow
    {
        public readonly uint Id;
        public readonly string Name;
        public readonly string ItemTypeRaw;
        public readonly uint ItemId;
        public readonly int Price;
        public readonly string CurrencyTypeRaw;
        public readonly int Amount;
        public readonly int? DailyCap;

        public ShopProductCsvRow(
            uint id,
            string name,
            string itemTypeRaw,
            uint itemId,
            int price,
            string currencyTypeRaw,
            int amount,
            int? dailyCap)
        {
            Id = id;
            Name = name;
            ItemTypeRaw = itemTypeRaw;
            ItemId = itemId;
            Price = price;
            CurrencyTypeRaw = currencyTypeRaw;
            Amount = amount;
            DailyCap = dailyCap;
        }
    }
}
