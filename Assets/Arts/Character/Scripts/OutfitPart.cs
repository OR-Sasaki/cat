using UnityEngine;

namespace Cat.Character
{
    public enum PartType // アルファベット順に並べること
    {
        BackFoot,
        BackHand,
        Body,
        ClothBack,
        ClothBody,
        ClothCollar,
        ClothFront,
        Face,
        FrontFoot,
        FrontFootLine,
        FrontHand,
        HandAccessory,
        HeadAccessory,
        LegAccessoryBack,
        LegAccessoryFront,
        Tail,
    }

    [CreateAssetMenu(fileName = "OutfitPart", menuName = "OutfitPart", order = -10000)]
    public class OutfitPart : ScriptableObject
    {
        public PartType PartType;
        public Sprite Sprite;
    }
}
