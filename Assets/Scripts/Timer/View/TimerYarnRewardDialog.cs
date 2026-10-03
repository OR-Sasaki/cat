#nullable enable

using Root.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Timer.View
{
    public record TimerYarnRewardDialogArgs(
        int GrantedYarn,
        int EarnedToday,
        int DailyCap,
        bool IsCapped
    ) : IDialogArgs;

    /// タイマー完了時に獲得した毛糸玉の数を知らせる小さなダイアログ
    public class TimerYarnRewardDialog : BaseDialogView<TimerYarnRewardDialogArgs>
    {
        const string GrantedTitle = "毛糸玉をゲット！";
        const string NoGrantTitle = "おつかれさま！";
        const string DailyCapNotice = "本日の上限に達しました";

        [SerializeField] TMP_Text? _titleText;
        [SerializeField] TMP_Text? _amountText;
        [SerializeField] TMP_Text? _dailyProgressText;
        /// 日次上限で獲得数が削られたときだけ出す注記
        [SerializeField] GameObject? _dailyCapNoticeRoot;
        [SerializeField] TMP_Text? _dailyCapNoticeText;
        /// 注記の表示有無に応じて高さが変わる VerticalLayoutGroup の root
        [SerializeField] RectTransform? _contentRoot;
        [SerializeField] Button? _okButton;

        protected override void Awake()
        {
            base.Awake();

            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkButtonClicked);
            }
        }

        protected override void OnInitialize(TimerYarnRewardDialogArgs args)
        {
            if (_titleText != null)
            {
                _titleText.text = args.GrantedYarn > 0 ? GrantedTitle : NoGrantTitle;
            }

            if (_amountText != null)
            {
                _amountText.text = $"×{args.GrantedYarn}";
            }

            if (_dailyProgressText != null)
            {
                _dailyProgressText.text = $"本日の獲得 {args.EarnedToday} / {args.DailyCap}";
            }

            if (_dailyCapNoticeText != null)
            {
                _dailyCapNoticeText.text = DailyCapNotice;
            }

            if (_dailyCapNoticeRoot != null)
            {
                _dailyCapNoticeRoot.SetActive(args.IsCapped);
            }

            // 注記の表示有無で本文の高さが変わる。フェードイン前に確定させて 1 フレームのズレを防ぐ
            if (_contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
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
