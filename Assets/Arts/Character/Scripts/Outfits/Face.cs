using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "Face", menuName = "Outfit/Face", order = -10000)]
    public class Face : Outfit
    {
        public override OutfitType OutfitType => OutfitType.Face;

        public OutfitPart FacePart;
    }
}
