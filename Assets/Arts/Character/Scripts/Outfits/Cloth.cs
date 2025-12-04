using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "Cloth", menuName = "Outfit/Cloth", order = -10000)]
    public class Cloth : Outfit
    {
        public override OutfitType OutfitType => OutfitType.Cloth;

        public OutfitPart ClothBackPart; // 奥袖
        public OutfitPart ClothBodyPart; // 胴体
        public OutfitPart ClothCollarPart; // 襟
        public OutfitPart ClothFrontPart; // 手前袖
    }
}
