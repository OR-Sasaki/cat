using EnhancedUI.EnhancedScroller;
using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Home.View
{
    public class ClosetUiView : UiView
    {
        [SerializeField] Button _backButton;
        [SerializeField] EnhancedScroller _scroller;
        [SerializeField] EnhancedScrollerCellView _cellViewPrefab;
        [SerializeField] int _numberOfCellsPerRow = 4;
        [SerializeField] float _cellViewSize = 100f;
        [SerializeField] float _bottomPadding = 50f;
        [SerializeField] ClosetMajorTabsView _majorTabsView;
        [SerializeField] ClosetMinorTabsView _minorTabsView;

        public EnhancedScroller Scroller => _scroller;
        public EnhancedScrollerCellView CellViewPrefab => _cellViewPrefab;
        public int NumberOfCellsPerRow => _numberOfCellsPerRow;
        public float CellViewSize => _cellViewSize;
        public float BottomPadding => _bottomPadding;

        [Inject]
        public void Init(
            HomeStateSetService homeStateSetService,
            ClosetTabService closetTabService,
            ClosetTabState closetTabState)
        {
            _backButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Home));

            if (_majorTabsView is not null)
            {
                _majorTabsView.Bind(closetTabService, closetTabState);
            }
            else
            {
                Debug.LogError("[ClosetUiView] _majorTabsView is not assigned");
            }

            if (_minorTabsView is not null)
            {
                _minorTabsView.Bind(closetTabService, closetTabState);
            }
            else
            {
                Debug.LogError("[ClosetUiView] _minorTabsView is not assigned");
            }
        }
    }
}
