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

        public void SetOutfit(Outfit outfit)
        {
            
        }
    }
}
