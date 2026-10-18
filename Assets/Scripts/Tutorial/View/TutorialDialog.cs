#nullable enable

using System.Collections.Generic;
using Root.View;
using TMPro;
using Tutorial.State;
using Tutorial.TutorialLogic;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.View
{
    public record TutorialDialogArgs(TutorialRegistry.Page[] Pages) : IDialogArgs;

    public class TutorialDialog : BaseDialogView<TutorialDialogArgs>
    {
        [SerializeField] Image _image = null!;
        [SerializeField] TMP_Text _text = null!;
        [SerializeField] GameObject _navigationRoot = null!;
        [SerializeField] Transform _dotsRoot = null!;
        [SerializeField] TutorialNavigationDotView _dotTemplate = null!;
        [SerializeField] Button _prevButton = null!;
        [SerializeField] Button _nextButton = null!;

        readonly List<TutorialNavigationDotView> _dots = new();
        TutorialPager? _pager;

        public override bool CanCloseByBackdrop => _pager?.HasReachedLast ?? true;

        protected override void OnInitialize(TutorialDialogArgs args)
        {
            var pager = new TutorialPager(args.Pages.Length);
            _pager = pager;
            _navigationRoot.SetActive(pager.ShowsNavigation);

            for (var i = 0; i < args.Pages.Length; i++)
            {
                var index = i;
                var dot = Instantiate(_dotTemplate, _dotsRoot);
                dot.gameObject.SetActive(true);
                dot.Initialize(() => Move(pager.JumpTo(index)));
                _dots.Add(dot);
            }

            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);

            Refresh();
        }

        void OnPrevClicked() => Move(_pager!.Previous());

        void OnNextClicked() => Move(_pager!.Next());

        void Move(bool changed)
        {
            if (changed)
            {
                Refresh();
            }
        }

        void Refresh()
        {
            var pager = _pager!;
            var page = Args.Pages[pager.Index];
            _image.sprite = page.Image;
            _text.text = page.Text;

            for (var i = 0; i < _dots.Count; i++)
            {
                _dots[i].SetCurrent(i == pager.Index);
            }

            if (CloseButton != null)
            {
                CloseButton.gameObject.SetActive(pager.HasReachedLast);
            }
        }

        protected override void OnDestroy()
        {
            _prevButton.onClick.RemoveListener(OnPrevClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            base.OnDestroy();
        }
    }
}
