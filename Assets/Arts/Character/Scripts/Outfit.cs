using UnityEngine;

namespace Cat.Character
{
    public enum OutfitType // アルファベット順に並べること
    {
        Body,
        Cloth,
        Face,
        HandAccessory,
        HeadAccessory,
        LegAccessory,
        Tail,
    }

    public abstract class Outfit : ScriptableObject
    {
        public abstract OutfitType OutfitType { get; }

        public Sprite Thumbnail;
    }
}
