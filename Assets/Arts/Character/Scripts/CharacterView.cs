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

            // TODO: リフレクションを使用しないように修正する
            //       SpriteRenderを取りまとめるクラスを作り、それをOutfitクラスへ渡すことで
            //       Spriteをセットするようにする
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

        public void RemoveOutfit(OutfitType outfitType)
        {
            var partTypes = GetPartTypes(outfitType);
            foreach (var partType in partTypes)
            {
                var spriteRenderer = GetSpriteRenderer(partType);
                if (spriteRenderer is not null)
                {
                    spriteRenderer.sprite = null;
                }
            }
        }

        PartType[] GetPartTypes(OutfitType outfitType)
        {
            return outfitType switch
            {
                OutfitType.Body => new[] { PartType.BackFoot, PartType.BackHand, PartType.Body, PartType.FrontFoot, PartType.FrontFootLine, PartType.FrontHand },
                OutfitType.Cloth => new[] { PartType.ClothBack, PartType.ClothBody, PartType.ClothCollar, PartType.ClothFront },
                OutfitType.Face => new[] { PartType.Face },
                OutfitType.HandAccessory => new[] { PartType.HandAccessory },
                OutfitType.HeadAccessory => new[] { PartType.HeadAccessory },
                OutfitType.LegAccessory => new[] { PartType.LegAccessoryBack, PartType.LegAccessoryFront },
                OutfitType.Tail => new[] { PartType.Tail },
                _ => System.Array.Empty<PartType>(),
            };
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
