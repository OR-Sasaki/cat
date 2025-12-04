using JetBrains.Annotations;
using UnityEngine;

namespace Cat.Character
{
    public class ClosetTest : MonoBehaviour
    {
        [SerializeField] OutfitSetting _outfitSetting;
        [SerializeField] Transform _contentRoot;
        [SerializeField] ClosetTestContent _closetTestContentPrefab;
        [SerializeField] CharacterView _characterView;

        public void Start()
        {
            if (_outfitSetting == null || _outfitSetting.Outfits == null)
            {
                return;
            }

            foreach (var outfit in _outfitSetting.Outfits)
            {
                var content = Instantiate(_closetTestContentPrefab, _contentRoot);
                content.Initialize(outfit);
                content.gameObject.SetActive(true);

                content.Button.onClick.AddListener(() =>
                {
                    _characterView.SetOutfit(outfit);
                });
            }
        }
    }
}
