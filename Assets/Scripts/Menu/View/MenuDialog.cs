#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Menu.State;
using Root.Service;
using Root.View;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Menu.View
{
    /// ホーム右上のメニューボタンから開く設定ダイアログ
    /// サウンド・通知のトグルは PlayerPrefs に永続化し、所持金確認は毛糸残高を表示する
    public class MenuDialog : BaseDialogView
    {
        [SerializeField] MenuSwitchView _soundSwitch = null!;
        [SerializeField] MenuSwitchView _notificationSwitch = null!;
        [SerializeField] Button _yarnBalanceButton = null!;
        [SerializeField] Button _noticeButton = null!;
        [SerializeField] Button _termsButton = null!;
        [SerializeField] Button _privacyPolicyButton = null!;
        [SerializeField] Button _contactButton = null!;

        PlayerPrefsService _playerPrefsService = null!;
        IDialogService _dialogService = null!;
        IUserPointService _userPointService = null!;

        MenuSettingData _settingData = new();

        [Inject]
        public void Construct(
            PlayerPrefsService playerPrefsService,
            IDialogService dialogService,
            IUserPointService userPointService)
        {
            _playerPrefsService = playerPrefsService;
            _dialogService = dialogService;
            _userPointService = userPointService;

            LoadSettings();

            _soundSwitch.SetValueWithoutNotify(_settingData.soundEnabled);
            _notificationSwitch.SetValueWithoutNotify(_settingData.notificationEnabled);

            _soundSwitch.ValueChanged += OnSoundSwitchChanged;
            _notificationSwitch.ValueChanged += OnNotificationSwitchChanged;

            _yarnBalanceButton.onClick.AddListener(OnYarnBalanceButtonClicked);
            _noticeButton.onClick.AddListener(OnNoticeButtonClicked);
            _termsButton.onClick.AddListener(OnTermsButtonClicked);
            _privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyButtonClicked);
            _contactButton.onClick.AddListener(OnContactButtonClicked);
        }

        void LoadSettings()
        {
            _settingData = _playerPrefsService.Load<MenuSettingData>(PlayerPrefsKey.MenuSetting) ?? new MenuSettingData();
        }

        void SaveSettings()
        {
            _playerPrefsService.Save(PlayerPrefsKey.MenuSetting, _settingData);
        }

        void OnSoundSwitchChanged(bool isOn)
        {
            _settingData.soundEnabled = isOn;
            SaveSettings();
        }

        void OnNotificationSwitchChanged(bool isOn)
        {
            _settingData.notificationEnabled = isOn;
            SaveSettings();
        }

        void OnYarnBalanceButtonClicked()
        {
            var balance = _userPointService.GetYarnBalance();
            OpenMessageAsync("所持金", $"毛糸 {balance:N0} 個", destroyCancellationToken).Forget();
        }

        void OnNoticeButtonClicked()
        {
            OpenNotImplementedAsync("お知らせ", destroyCancellationToken).Forget();
        }

        void OnTermsButtonClicked()
        {
            OpenNotImplementedAsync("利用規約", destroyCancellationToken).Forget();
        }

        void OnPrivacyPolicyButtonClicked()
        {
            OpenNotImplementedAsync("プライバシーポリシー", destroyCancellationToken).Forget();
        }

        void OnContactButtonClicked()
        {
            OpenNotImplementedAsync("お問い合わせ", destroyCancellationToken).Forget();
        }

        /// 遷移先が未実装の項目はメッセージダイアログで告知する
        UniTask OpenNotImplementedAsync(string title, CancellationToken cancellationToken)
        {
            return OpenMessageAsync(title, "準備中です", cancellationToken);
        }

        async UniTask OpenMessageAsync(string title, string message, CancellationToken cancellationToken)
        {
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(title, message),
                cancellationToken);
        }

        protected override void OnDestroy()
        {
            _soundSwitch.ValueChanged -= OnSoundSwitchChanged;
            _notificationSwitch.ValueChanged -= OnNotificationSwitchChanged;

            _yarnBalanceButton.onClick.RemoveListener(OnYarnBalanceButtonClicked);
            _noticeButton.onClick.RemoveListener(OnNoticeButtonClicked);
            _termsButton.onClick.RemoveListener(OnTermsButtonClicked);
            _privacyPolicyButton.onClick.RemoveListener(OnPrivacyPolicyButtonClicked);
            _contactButton.onClick.RemoveListener(OnContactButtonClicked);

            base.OnDestroy();
        }
    }
}
