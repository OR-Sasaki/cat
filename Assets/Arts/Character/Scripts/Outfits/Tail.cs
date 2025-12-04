using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "Tail", menuName = "Outfit/Tail", order = -10000)]
    public class Tail : Outfit
    {
        public override OutfitType OutfitType => OutfitType.Tail;

        public OutfitPart TailPart;
    }
}
