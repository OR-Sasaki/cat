using System.Linq;
using UnityEngine;

namespace Cat.Character
{
    [CreateAssetMenu(fileName = "OutfitSetting", menuName = "Cat/OutfitSetting", order = -10000)]
    public class OutfitSetting : ScriptableObject
    {
       public Outfit[] Outfits;
    }
}
