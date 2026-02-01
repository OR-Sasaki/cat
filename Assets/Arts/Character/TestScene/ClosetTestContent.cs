using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cat.Character
{
    public class ClosetTestContent : MonoBehaviour
    {
        [SerializeField] TMP_Text _outfitName;
        [SerializeField] Transform _imageRoot;
        [SerializeField] Image _imagePrefab;
        public Button Button;

        public void Initialize(Outfit outfit)
        {
            if (outfit == null)
            {
                return;
            }

            _outfitName.text = outfit.name;

            var outfitType = outfit.GetType();
            var fields = outfitType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(OutfitPart))
                {
                    var outfitPart = field.GetValue(outfit) as OutfitPart;
                    var image = Instantiate(_imagePrefab, _imageRoot);
                    image.sprite = outfitPart.Sprite;
                    image.gameObject.SetActive(true);
                }
            }
        }
    }
}
