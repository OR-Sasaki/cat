using System;

namespace Menu.State
{
    /// メニュー (設定) ダイアログのトグル設定。PlayerPrefs に JSON で永続化する
    [Serializable]
    public class MenuSettingData
    {
        public bool notificationEnabled = true;
    }
}
