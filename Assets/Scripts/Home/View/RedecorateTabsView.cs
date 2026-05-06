using Cat.Furniture;
using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.Events;

namespace Home.View
{
    /// リデコレート 4 タブの静的バインドコンテナ View。`FurnitureType` 宣言順 (Base → Floor → Small → Wall) を強制する
    public class RedecorateTabsView : MonoBehaviour
    {
        [SerializeField] RedecorateTabItemView _baseTabItem;
        [SerializeField] RedecorateTabItemView _floorTabItem;
        [SerializeField] RedecorateTabItemView _smallTabItem;
        [SerializeField] RedecorateTabItemView _wallTabItem;

        RedecorateTabState _redecorateTabState;
        RedecorateTabService _redecorateTabService;
        UnityAction<FurnitureType> _onChanged;

        /// 親 (`RedecorateUiView`) から呼ばれてバインドし、初期描画と購読を行う
        public void Bind(RedecorateTabService redecorateTabService, RedecorateTabState redecorateTabState)
        {
            _redecorateTabService = redecorateTabService;
            _redecorateTabState = redecorateTabState;

            EnforceSiblingOrder();
            BindItems();

            _onChanged = OnChanged;
            _redecorateTabState.Changed.AddListener(_onChanged);

            OnChanged(_redecorateTabState.Current);
        }

        /// Inspector 配線ぶれの保険として、コード側で sibling 順序を `Base` → `Floor` → `Small` → `Wall` に強制する
        void EnforceSiblingOrder()
        {
            if (_baseTabItem is not null) _baseTabItem.transform.SetSiblingIndex(0);
            if (_floorTabItem is not null) _floorTabItem.transform.SetSiblingIndex(1);
            if (_smallTabItem is not null) _smallTabItem.transform.SetSiblingIndex(2);
            if (_wallTabItem is not null) _wallTabItem.transform.SetSiblingIndex(3);
        }

        void BindItems()
        {
            if (_baseTabItem is not null)
            {
                _baseTabItem.Bind(() => _redecorateTabService.Select(FurnitureType.Base));
            }
            else
            {
                Debug.LogError("[RedecorateTabsView] _baseTabItem is not assigned");
            }

            if (_floorTabItem is not null)
            {
                _floorTabItem.Bind(() => _redecorateTabService.Select(FurnitureType.Floor));
            }
            else
            {
                Debug.LogError("[RedecorateTabsView] _floorTabItem is not assigned");
            }

            if (_smallTabItem is not null)
            {
                _smallTabItem.Bind(() => _redecorateTabService.Select(FurnitureType.Small));
            }
            else
            {
                Debug.LogError("[RedecorateTabsView] _smallTabItem is not assigned");
            }

            if (_wallTabItem is not null)
            {
                _wallTabItem.Bind(() => _redecorateTabService.Select(FurnitureType.Wall));
            }
            else
            {
                Debug.LogError("[RedecorateTabsView] _wallTabItem is not assigned");
            }
        }

        void OnChanged(FurnitureType current)
        {
            if (_baseTabItem is not null) _baseTabItem.SetSelected(current == FurnitureType.Base);
            if (_floorTabItem is not null) _floorTabItem.SetSelected(current == FurnitureType.Floor);
            if (_smallTabItem is not null) _smallTabItem.SetSelected(current == FurnitureType.Small);
            if (_wallTabItem is not null) _wallTabItem.SetSelected(current == FurnitureType.Wall);
        }

        void OnDestroy()
        {
            if (_redecorateTabState is not null && _onChanged is not null)
            {
                _redecorateTabState.Changed.RemoveListener(_onChanged);
            }
        }
    }
}
