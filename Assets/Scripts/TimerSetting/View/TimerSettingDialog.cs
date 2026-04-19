#nullable enable

using Root.Service;
using Root.View;
using TimerSetting.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TimerSetting.View
{
    /// タイマー設定ダイアログ
    /// 集中時間・休憩時間・セット回数を設定し、PlayerPrefsに永続化する
    public class TimerSettingDialog : BaseDialogView
    {
        [SerializeField] TMP_Text _focusTimeText = null!;
        [SerializeField] TMP_Text _breakTimeText = null!;
        [SerializeField] TMP_Text _setsText = null!;
        [SerializeField] Button _startButton = null!;
        [SerializeField] Button _focusTimeUpButton = null!;
        [SerializeField] Button _focusTimeDownButton = null!;
        [SerializeField] Button _breakTimeUpButton = null!;
        [SerializeField] Button _breakTimeDownButton = null!;
        [SerializeField] Button _setsUpButton = null!;
        [SerializeField] Button _setsDownButton = null!;

        PlayerPrefsService _playerPrefsService = null!;

        int _focusTime;
        int _breakTime;
        int _sets;

        [Inject]
        public void Construct(PlayerPrefsService playerPrefsService)
        {
            _playerPrefsService = playerPrefsService;

            LoadSettings();
            UpdateTexts();

            _focusTimeUpButton.onClick.AddListener(() => SetFocusTime(_focusTime + 1));
            _focusTimeDownButton.onClick.AddListener(() => SetFocusTime(_focusTime - 1));
            _breakTimeUpButton.onClick.AddListener(() => SetBreakTime(_breakTime + 1));
            _breakTimeDownButton.onClick.AddListener(() => SetBreakTime(_breakTime - 1));
            _setsUpButton.onClick.AddListener(() => SetSets(_sets + 1));
            _setsDownButton.onClick.AddListener(() => SetSets(_sets - 1));
            _startButton.onClick.AddListener(OnStartButtonClicked);
        }

        void LoadSettings()
        {
            var data = _playerPrefsService.Load<TimerSettingData>(PlayerPrefsKey.TimerSetting);
            data ??= new TimerSettingData();
            _focusTime = data.focusTime;
            _breakTime = data.breakTime;
            _sets = data.sets;
        }

        void SaveSettings()
        {
            var data = new TimerSettingData
            {
                focusTime = _focusTime,
                breakTime = _breakTime,
                sets = _sets
            };
            _playerPrefsService.Save(PlayerPrefsKey.TimerSetting, data);
        }

        void SetFocusTime(int value)
        {
            _focusTime = Mathf.Clamp(value, 1, 60);
            UpdateTexts();
        }

        void SetBreakTime(int value)
        {
            _breakTime = Mathf.Clamp(value, 1, 60);
            UpdateTexts();
        }

        void SetSets(int value)
        {
            _sets = Mathf.Clamp(value, 1, 10);
            UpdateTexts();
        }

        void UpdateTexts()
        {
            _focusTimeText.text = $"{_focusTime}";
            _breakTimeText.text = $"{_breakTime}";
            _setsText.text = $"{_sets}";
        }

        void OnStartButtonClicked()
        {
            SaveSettings();
            RequestClose(DialogResult.Ok);
        }

        protected override void OnDestroy()
        {
            SaveSettings();

            _focusTimeUpButton.onClick.RemoveAllListeners();
            _focusTimeDownButton.onClick.RemoveAllListeners();
            _breakTimeUpButton.onClick.RemoveAllListeners();
            _breakTimeDownButton.onClick.RemoveAllListeners();
            _setsUpButton.onClick.RemoveAllListeners();
            _setsDownButton.onClick.RemoveAllListeners();
            _startButton.onClick.RemoveAllListeners();

            base.OnDestroy();
        }
    }
}
