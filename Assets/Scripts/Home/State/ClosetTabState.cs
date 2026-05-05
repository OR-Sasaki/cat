using System;
using System.Collections.Generic;
using Cat.Character;
using UnityEngine;
using UnityEngine.Events;

namespace Home.State
{
    /// クローゼット 2 段階タブ (大/小) の選択状態とマッピングを保持する SSOT
    public class ClosetTabState
    {
        static readonly OutfitType[] _bodyMinors =
        {
            OutfitType.Body,
            OutfitType.Face,
            OutfitType.Tail,
            OutfitType.FaceMakeup,
        };

        static readonly OutfitType[] _clothMinors =
        {
            OutfitType.Cloth,
            OutfitType.HandAccessory,
            OutfitType.HeadAccessory,
            OutfitType.LegAccessory,
            OutfitType.Effect,
        };

        static readonly Dictionary<MajorTab, OutfitType[]> _majorTabMinors = new()
        {
            { MajorTab.Body, _bodyMinors },
            { MajorTab.Cloth, _clothMinors },
        };

        static readonly Dictionary<MajorTab, OutfitType> _majorTabDefaults = new()
        {
            { MajorTab.Body, OutfitType.Face },
            { MajorTab.Cloth, OutfitType.Cloth },
        };

        public const MajorTab DefaultMajor = MajorTab.Body;
        public const OutfitType DefaultMinor = OutfitType.Face;

        public readonly UnityEvent<MajorTab> MajorChanged = new();
        public readonly UnityEvent<OutfitType> MinorChanged = new();

        MajorTab _major = DefaultMajor;
        OutfitType _minor = DefaultMinor;

        public MajorTab Major => _major;
        public OutfitType Minor => _minor;

        public ClosetTabState()
        {
            ValidateMappings();
        }

        /// 大タブを書き込む。値が変化したときのみ通知を発火する
        public void WriteMajor(MajorTab value)
        {
            if (_major == value) return;
            _major = value;
            MajorChanged.Invoke(value);
        }

        /// 小タブを書き込む。値が変化したときのみ通知を発火する
        public void WriteMinor(OutfitType value)
        {
            if (_minor == value) return;
            _minor = value;
            MinorChanged.Invoke(value);
        }

        /// 指定大タブに対応する小タブ集合を返す
        public IReadOnlyList<OutfitType> GetMinorTypes(MajorTab major)
        {
            return _majorTabMinors.TryGetValue(major, out var values)
                ? values
                : Array.Empty<OutfitType>();
        }

        /// 指定大タブの既定小タブを返す
        public OutfitType GetDefaultMinor(MajorTab major)
        {
            return _majorTabDefaults.TryGetValue(major, out var value) ? value : DefaultMinor;
        }

        /// 指定小タブが属する大タブを返す。未割当時は既定大タブを返す
        public MajorTab GetMajorOf(OutfitType minor)
        {
            foreach (var pair in _majorTabMinors)
            {
                if (Array.IndexOf(pair.Value, minor) >= 0) return pair.Key;
            }
            return DefaultMajor;
        }

        /// 全 OutfitType がいずれか 1 つの MajorTab にだけ属していることを検証する
        void ValidateMappings()
        {
            var allOutfitTypes = (OutfitType[])Enum.GetValues(typeof(OutfitType));

            foreach (var type in allOutfitTypes)
            {
                var count = 0;
                foreach (var pair in _majorTabMinors)
                {
                    if (Array.IndexOf(pair.Value, type) >= 0) count++;
                }

                if (count == 0)
                {
                    Debug.LogError($"[ClosetTabState] OutfitType '{type}' is not assigned to any major tab");
                }
                else if (count > 1)
                {
                    Debug.LogError($"[ClosetTabState] OutfitType '{type}' is assigned to multiple major tabs");
                }
            }
        }
    }
}
