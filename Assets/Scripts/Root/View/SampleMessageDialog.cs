#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    public record SampleMessageDialogArgs(string Message) : IDialogArgs;

    public class SampleMessageDialog : BaseDialogView<SampleMessageDialogArgs>
    {
        [SerializeField] TextMeshProUGUI? _messageText;
        [SerializeField] Button? _okButton;

        protected override void Awake()
        {
            base.Awake();

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkButtonClicked);
            }
        }

        protected override void OnInitialize(SampleMessageDialogArgs args)
        {
            if (_messageText != null)
            {
                _messageText.text = args.Message;
            }
        }

        void OnOkButtonClicked()
        {
            RequestClose(DialogResult.Ok);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnOkButtonClicked);
            }
        }
    }
}
