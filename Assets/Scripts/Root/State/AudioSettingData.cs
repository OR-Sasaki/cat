#nullable enable

using System;

namespace Root.State
{
    /// オーディオ設定の永続化用 DTO
    [Serializable]
    public class AudioSettingData
    {
        public float bgmVolume = 1f;
        public float seVolume = 1f;
        public bool soundEnabled = true;
    }
}
