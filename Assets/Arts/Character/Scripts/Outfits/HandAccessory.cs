using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "HandAccessory", menuName = "Outfit/HandAccessory", order = -10000)]
    public class HandAccessory : Outfit
    {
        public override OutfitType OutfitType => OutfitType.HandAccessory;

        public OutfitPart HandAccessoryPart;
    }
}
