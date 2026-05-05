using System;
using System.Collections.Generic;
using Cat.Character;
using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.Events;

namespace Home.View
{
    /// 小タブ群のコンテナ View。現大タブの集合に応じて要素を動的に再構築する
    public class ClosetMinorTabsView : MonoBehaviour
    {
        [Serializable]
        public class IconEntry
        {
            public OutfitType OutfitType;
            public Sprite Icon;
        }

        [SerializeField] Transform _itemRoot;
        [SerializeField] ClosetMinorTabItemView _itemPrefab;
        [SerializeField] List<IconEntry> _iconEntries = new();

        readonly Dictionary<OutfitType, Sprite> _iconMap = new();
        readonly Dictionary<OutfitType, ClosetMinorTabItemView> _items = new();

        ClosetTabState _closetTabState;
        ClosetTabService _closetTabService;
        UnityAction<MajorTab> _onMajorChanged;
        UnityAction<OutfitType> _onMinorChanged;

        /// 親 (`ClosetUiView`) から呼ばれてバインドし、初期描画と購読を行う
        public void Bind(ClosetTabService closetTabService, ClosetTabState closetTabState)
        {
            _closetTabService = closetTabService;
            _closetTabState = closetTabState;

            BuildIconMap();

            _onMajorChanged = OnMajorChanged;
            _onMinorChanged = OnMinorChanged;
            _closetTabState.MajorChanged.AddListener(_onMajorChanged);
            _closetTabState.MinorChanged.AddListener(_onMinorChanged);

            Rebuild(_closetTabState.Major);
            UpdateSelection(_closetTabState.Minor);
        }

        void BuildIconMap()
        {
            _iconMap.Clear();
            foreach (var entry in _iconEntries)
            {
                if (entry is null) continue;
                _iconMap[entry.OutfitType] = entry.Icon;
            }
        }

        void OnMajorChanged(MajorTab major)
        {
            Rebuild(major);
            UpdateSelection(_closetTabState.Minor);
        }

        void OnMinorChanged(OutfitType minor)
        {
            UpdateSelection(minor);
        }

        void Rebuild(MajorTab major)
        {
            // 既存子要素を破棄
            foreach (var pair in _items)
            {
                if (pair.Value is not null) Destroy(pair.Value.gameObject);
            }
            _items.Clear();

            if (_itemRoot is null || _itemPrefab is null)
            {
                Debug.LogError("[ClosetMinorTabsView] _itemRoot or _itemPrefab is not assigned");
                return;
            }

            var minorTypes = _closetTabState.GetMinorTypes(major);
            for (var i = 0; i < minorTypes.Count; i++)
            {
                var type = minorTypes[i];
                var item = Instantiate(_itemPrefab, _itemRoot);
                item.name = $"{_itemPrefab.name}_{type}";

                if (!_iconMap.TryGetValue(type, out var icon))
                {
                    Debug.LogError($"[ClosetMinorTabsView] Icon for OutfitType '{type}' is not assigned");
                    icon = null;
                }

                var capturedType = type;
                item.Bind(icon, () => _closetTabService.SelectMinorTab(capturedType));
                _items[type] = item;
            }
        }

        void UpdateSelection(OutfitType minor)
        {
            foreach (var pair in _items)
            {
                if (pair.Value is null) continue;
                pair.Value.SetSelected(pair.Key == minor);
            }
        }

        void OnDestroy()
        {
            if (_closetTabState is null) return;
            if (_onMajorChanged is not null) _closetTabState.MajorChanged.RemoveListener(_onMajorChanged);
            if (_onMinorChanged is not null) _closetTabState.MinorChanged.RemoveListener(_onMinorChanged);
        }
    }
}
