#nullable enable

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.View
{
    public class TutorialNavigationDotView : MonoBehaviour
    {
        [SerializeField] Button _button = null!;
        [SerializeField] GameObject _activeDot = null!;
        [SerializeField] GameObject _inactiveDot = null!;

        Action? _onClicked;

        public void Initialize(Action onClicked)
        {
            _onClicked = onClicked;
            _button.onClick.AddListener(OnButtonClicked);
        }

        public void SetCurrent(bool isCurrent)
        {
            _activeDot.SetActive(isCurrent);
            _inactiveDot.SetActive(!isCurrent);
        }

        void OnButtonClicked()
        {
            _onClicked?.Invoke();
        }

        void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
