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
        TutorialPager _pager = null!;

        protected override void OnInitialize(TutorialDialogArgs args)
        {
            _pager = new TutorialPager(args.Pages.Length);
            _navigationRoot.SetActive(_pager.ShowsNavigation);

            for (var i = 0; i < args.Pages.Length; i++)
            {
                var index = i;
                var dot = Instantiate(_dotTemplate, _dotsRoot);
                dot.gameObject.SetActive(true);
                dot.Initialize(() =>
                {
                    if (_pager.JumpTo(index))
                    {
                        Refresh();
                    }
                });
                _dots.Add(dot);
            }

            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);

            Refresh();
        }

        void OnPrevClicked()
        {
            if (_pager.Previous())
            {
                Refresh();
            }
        }

        void OnNextClicked()
        {
            if (_pager.Next())
            {
                Refresh();
            }
        }

        void Refresh()
        {
            var page = Args.Pages[_pager.Index];
            _image.sprite = page.Image;
            _text.text = page.Text;

            for (var i = 0; i < _dots.Count; i++)
            {
                _dots[i].SetCurrent(i == _pager.Index);
            }

            SetCloseEnabled(_pager.HasReachedLast);
        }

        protected override void OnDestroy()
        {
            _prevButton.onClick.RemoveListener(OnPrevClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            base.OnDestroy();
        }
    }
}
