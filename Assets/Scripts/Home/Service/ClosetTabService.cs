using Cat.Character;
using Home.State;
using UnityEngine;
using VContainer;

namespace Home.Service
{
    /// クローゼット 2 段階タブの遷移ロジックを集約する Service
    public sealed class ClosetTabService
    {
        readonly ClosetTabState _closetTabState;

        [Inject]
        public ClosetTabService(ClosetTabState closetTabState)
        {
            _closetTabState = closetTabState;
        }

        /// 既定状態 (Body + Face) にリセットする
        public void ResetToDefault()
        {
            _closetTabState.WriteMajor(ClosetTabState.DefaultMajor);
            _closetTabState.WriteMinor(ClosetTabState.DefaultMinor);
        }

        /// 大タブを選択する。同値時は no-op、異値時は小タブも既定値に同期する
        public void SelectMajorTab(MajorTab major)
        {
            if (_closetTabState.Major == major) return;

            _closetTabState.WriteMajor(major);
            _closetTabState.WriteMinor(_closetTabState.GetDefaultMinor(major));
        }

        /// 小タブを選択する。同値時または現 Major のマッピングに含まれない値は no-op
        public void SelectMinorTab(OutfitType minor)
        {
            if (_closetTabState.Minor == minor) return;

            var currentMajor = _closetTabState.Major;
            var minorTypes = _closetTabState.GetMinorTypes(currentMajor);
            var contains = false;
            for (var i = 0; i < minorTypes.Count; i++)
            {
                if (minorTypes[i] == minor)
                {
                    contains = true;
                    break;
                }
            }

            if (!contains)
            {
                Debug.LogError($"[ClosetTabService] OutfitType '{minor}' is not assigned to major tab '{currentMajor}'");
                return;
            }

            _closetTabState.WriteMinor(minor);
        }
    }
}
