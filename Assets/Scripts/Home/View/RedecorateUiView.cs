using EnhancedUI.EnhancedScroller;
using Home.Service;
using Home.State;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Home.View
{
    public class RedecorateUiView : UiView
    {
        [SerializeField] Button _backButton;
        [SerializeField] EnhancedScroller _scroller;
        [SerializeField] EnhancedScrollerCellView _cellViewPrefab;
        [SerializeField] int _numberOfCellsPerRow = 4;
        [SerializeField] float _cellViewSize = 100f;
        [SerializeField] float _bottomPadding = 50f;

        [Header("Tiny")]
        [SerializeField] Animator _tinyAnimator;
        [SerializeField] Button _tinyButton;

        static readonly int TinyParam = Animator.StringToHash("Tiny");

        bool _isTiny;

        public bool IsTiny => _isTiny;

        public EnhancedScroller Scroller => _scroller;
        public EnhancedScrollerCellView CellViewPrefab => _cellViewPrefab;
        public int NumberOfCellsPerRow => _numberOfCellsPerRow;
        public float CellViewSize => _cellViewSize;
        public float BottomPadding => _bottomPadding;

        public Button TinyButton => _tinyButton;

        public void SetTiny(bool isTiny)
        {
            _isTiny = isTiny;
            _tinyAnimator.SetBool(TinyParam, _isTiny);
        }

        public void ResetTiny()
        {
            _isTiny = false;
            _tinyAnimator.SetBool(TinyParam, false);
        }

        [Inject]
        public void Init(HomeStateSetService homeStateSetService)
        {
            _backButton.onClick.AddListener(() => homeStateSetService.SetState(HomeState.State.Home));
        }
    }
}