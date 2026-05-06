using UnityEngine;

namespace Cat.Character
{
    public enum OutfitType
    {
        Body = 1,
        Cloth = 2,
        Face = 3,
        HandAccessory = 4,
        HeadAccessory = 5,
        LegAccessory = 6,
        Tail = 7,
        FaceMakeup = 8,
        Effect = 9,
    }

    public abstract class Outfit : ScriptableObject
    {
        public abstract OutfitType OutfitType { get; }

        public Sprite Thumbnail;
    }
}
