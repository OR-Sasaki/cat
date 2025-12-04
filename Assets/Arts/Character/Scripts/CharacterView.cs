using System;
using System.Reflection;
using UnityEngine;

namespace Cat.Character
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _backFoot;
        [SerializeField] SpriteRenderer _backHand;
        [SerializeField] SpriteRenderer _body;
        [SerializeField] SpriteRenderer _clothBack;
        [SerializeField] SpriteRenderer _clothBody;
        [SerializeField] SpriteRenderer _clothCollar;
        [SerializeField] SpriteRenderer _clothFront;
        [SerializeField] SpriteRenderer _face;
        [SerializeField] SpriteRenderer _frontFoot;
        [SerializeField] SpriteRenderer _frontFootLine;
        [SerializeField] SpriteRenderer _frontHand;
        [SerializeField] SpriteRenderer _handAccessory;
        [SerializeField] SpriteRenderer _headAccessory;
        [SerializeField] SpriteRenderer _legAccessoryBack;
        [SerializeField] SpriteRenderer _legAccessoryFront;
        [SerializeField] SpriteRenderer _tail;

        [SerializeField] OutfitPartOrderSetting _outfitPartOrderSetting;

        public void SetOutfit(Outfit outfit)
        {
            if (outfit == null)
            {
                return;
            }

            var outfitType = outfit.GetType();
            var fields = outfitType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(OutfitPart))
                {
                    var outfitPart = field.GetValue(outfit) as OutfitPart;
                    if (outfitPart != null)
                    {
                        var spriteRenderer = GetSpriteRenderer(outfitPart.PartType);
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = outfitPart.Sprite;

                            var order = _outfitPartOrderSetting.GetOrder(outfitPart.PartType);
                            if (order != int.MaxValue)
                            {
                                spriteRenderer.sortingOrder = order * 100;
                            }
                        }
                    }
                }
            }
        }

        SpriteRenderer GetSpriteRenderer(PartType partType)
        {
            return partType switch
            {
                PartType.BackFoot => _backFoot,
                PartType.BackHand => _backHand,
                PartType.Body => _body,
                PartType.ClothBack => _clothBack,
                PartType.ClothBody => _clothBody,
                PartType.ClothCollar => _clothCollar,
                PartType.ClothFront => _clothFront,
                PartType.Face => _face,
                PartType.FrontFoot => _frontFoot,
                PartType.FrontFootLine => _frontFootLine,
                PartType.FrontHand => _frontHand,
                PartType.HandAccessory => _handAccessory,
                PartType.HeadAccessory => _headAccessory,
                PartType.LegAccessoryBack => _legAccessoryBack,
                PartType.LegAccessoryFront => _legAccessoryFront,
                PartType.Tail => _tail,
                _ => null,
            };
        }
    }
}
