#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Root.View
{
    /// CommonConfirmDialog の引数
    public record CommonConfirmDialogArgs(
        string Title,
        string Message,
        string OkButtonText = "はい",
        string CancelButtonText = "いいえ"
    ) : IDialogArgs;

    /// 汎用確認ダイアログ
    public class CommonConfirmDialog : BaseDialogView<CommonConfirmDialogArgs>
    {
        [SerializeField] TMP_Text? _titleText;
        [SerializeField] TMP_Text? _messageText;
        [SerializeField] Button? _okButton;
        [SerializeField] Button? _cancelButton;
        [SerializeField] TMP_Text? _okButtonText;
        [SerializeField] TMP_Text? _cancelButtonText;

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

        protected override void OnInitialize(CommonConfirmDialogArgs args)
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

            if (_cancelButtonText != null)
            {
                _cancelButtonText.text = args.CancelButtonText;
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
