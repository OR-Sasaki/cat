using System.Linq;
using UnityEngine;

namespace Cat.Character
{
    [CreateAssetMenu(fileName = "OutfitPartOrderSetting", menuName = "Cat/OutfitPartOrderSetting", order = -10000)]
    public class OutfitPartOrderSetting : ScriptableObject
    {
        [SerializeField]
        PartType[] _partOrder = new PartType[]
        {
            PartType.BackFoot,
            PartType.BackHand,
            PartType.Body,
            PartType.ClothBack,
            PartType.ClothBody,
            PartType.ClothCollar,
            PartType.ClothFront,
            PartType.Face,
            PartType.FrontFoot,
            PartType.FrontFootLine,
            PartType.FrontHand,
            PartType.HandAccessory,
            PartType.HeadAccessory,
            PartType.LegAccessoryBack,
            PartType.LegAccessoryFront,
            PartType.Tail,
        };

        public PartType[] PartOrder => _partOrder;

        public int GetOrder(PartType partType)
        {
            var index = System.Array.IndexOf(_partOrder, partType);
            return index >= 0 ? index : int.MaxValue;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            ValidatePartOrder();
        }

        void ValidatePartOrder()
        {
            if (_partOrder == null)
            {
                Debug.LogError($"[OutfitPartOrderSetting] PartOrder配列がnullです。");
                return;
            }

            var allPartTypes = System.Enum.GetValues(typeof(PartType)).Cast<PartType>().ToArray();
            var missingParts = allPartTypes.Where(partType => !_partOrder.Contains(partType)).ToArray();
            var duplicateParts = _partOrder.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();

            if (missingParts.Length > 0)
            {
                Debug.LogError($"[OutfitPartOrderSetting] 以下のPartTypeが配列に含まれていません: {string.Join(", ", missingParts)}");
            }

            if (duplicateParts.Length > 0)
            {
                Debug.LogError($"[OutfitPartOrderSetting] 以下のPartTypeが重複しています: {string.Join(", ", duplicateParts)}");
            }

            if (missingParts.Length == 0 && duplicateParts.Length == 0)
            {
                Debug.Log($"[OutfitPartOrderSetting] 全てのPartTypeが正しく設定されています。");
            }
        }
#endif
    }
}
