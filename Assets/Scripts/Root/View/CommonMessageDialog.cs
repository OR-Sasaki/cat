#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    public record CommonMessageDialogArgs(
        string Title,
        string Message,
        string OkButtonText = "OK"
    ) : IDialogArgs;

    public class CommonMessageDialog : BaseDialogView<CommonMessageDialogArgs>
    {
        [SerializeField] TMP_Text? _titleText;
        [SerializeField] TMP_Text? _messageText;
        [SerializeField] Button? _okButton;
        [SerializeField] TMP_Text? _okButtonText;

        protected override void Awake()
        {
            base.Awake();

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkButtonClicked);
            }
        }

        protected override void OnInitialize(CommonMessageDialogArgs args)
        {
            if (_titleText != null)
            {
                _titleText.text = args.Title;
            }

            if (_messageText != null)
            {
                _messageText.text = args.Message;
            }

            if (_okButtonText != null)
            {
                _okButtonText.text = args.OkButtonText;
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