using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "LegAccessory", menuName = "Outfit/LegAccessory", order = -10000)]
    public class LegAccessory : Outfit
    {
        public override OutfitType OutfitType => OutfitType.LegAccessory;

        public OutfitPart LegAccessoryBackPart;
        public OutfitPart LegAccessoryFrontPart;
    }
}
