using UnityEngine;

namespace Cat.Character.Outfits
{
    [CreateAssetMenu(fileName = "Body", menuName = "Outfit/Body", order = -10000)]
    public class Body : Outfit
    {
        public override OutfitType OutfitType => OutfitType.Body;

        public OutfitPart BackFootPart; // 奥足
        public OutfitPart BackHandPart; // 奥手
        public OutfitPart BodyPart; // 胴体
        public OutfitPart FrontFoot; // 手前足
        public OutfitPart FrontFootLine; // 手前足の輪郭線
        public OutfitPart FrontHand; // 手前手
    }
}
