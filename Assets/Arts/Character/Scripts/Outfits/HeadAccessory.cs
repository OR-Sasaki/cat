using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "HeadAccessory", menuName = "Outfit/HeadAccessory", order = -10000)]
    public class HeadAccessory : Outfit
    {
        public override OutfitType OutfitType => OutfitType.HeadAccessory;

        public OutfitPart HeadAccessoryPart;
    }
}
