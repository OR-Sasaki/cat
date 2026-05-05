using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.Events;

namespace Home.View
{
    /// 大タブ群のコンテナ View。「体」「服」の 2 要素を静的に保持する
    public class ClosetMajorTabsView : MonoBehaviour
    {
        [SerializeField] ClosetMajorTabItemView _bodyTabItem;
        [SerializeField] ClosetMajorTabItemView _clothTabItem;

        ClosetTabState _closetTabState;
        ClosetTabService _closetTabService;
        UnityAction<MajorTab> _onMajorChanged;

        /// 親 (`ClosetUiView`) から呼ばれてバインドし、初期描画と購読を行う
        public void Bind(ClosetTabService closetTabService, ClosetTabState closetTabState)
        {
            _closetTabService = closetTabService;
            _closetTabState = closetTabState;

            if (_bodyTabItem is not null)
            {
                _bodyTabItem.Bind(() => _closetTabService.SelectMajorTab(MajorTab.Body));
            }
            else
            {
                Debug.LogError("[ClosetMajorTabsView] _bodyTabItem is not assigned");
            }
            if (_clothTabItem is not null)
            {
                _clothTabItem.Bind(() => _closetTabService.SelectMajorTab(MajorTab.Cloth));
            }
            else
            {
                Debug.LogError("[ClosetMajorTabsView] _clothTabItem is not assigned");
            }

            _onMajorChanged = OnMajorChanged;
            _closetTabState.MajorChanged.AddListener(_onMajorChanged);

            OnMajorChanged(_closetTabState.Major);
        }

        void OnMajorChanged(MajorTab major)
        {
            if (_bodyTabItem is not null) _bodyTabItem.SetSelected(major == MajorTab.Body);
            if (_clothTabItem is not null) _clothTabItem.SetSelected(major == MajorTab.Cloth);
        }

        void OnDestroy()
        {
            if (_closetTabState is not null && _onMajorChanged is not null)
            {
                _closetTabState.MajorChanged.RemoveListener(_onMajorChanged);
            }
        }
    }
}
