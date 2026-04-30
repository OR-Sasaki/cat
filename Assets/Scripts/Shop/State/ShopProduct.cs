#nullable enable

namespace Shop.State
{
    public enum ItemType
    {
        Furniture,
        Outfit,
        Point
    }

    public sealed record ShopProduct(
        uint Id,
        string Name,
        ItemType ItemType,
        uint ItemId,
        int Price,
        CurrencyType CurrencyType
    );
}
