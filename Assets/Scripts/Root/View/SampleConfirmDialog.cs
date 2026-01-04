#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    public class SampleConfirmDialog : BaseDialogView
    {
        [SerializeField] Button? _okButton;
        [SerializeField] Button? _cancelButton;

        protected override void Awake()
        {
            base.Awake();

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        void OnOkButtonClicked()
        {
            RequestClose(DialogResult.Ok);
        }

        void OnCancelButtonClicked()
        {
            RequestClose(DialogResult.Cancel);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnOkButtonClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }
    }
}
