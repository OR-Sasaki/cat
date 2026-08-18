#nullable enable

namespace Root.Service
{
    /// オーディオ設定のイミュータブルなスナップショット
    public sealed class AudioSettingSnapshot
    {
        public float BgmVolume { get; }
        public float SeVolume { get; }
        public bool SoundEnabled { get; }

        public AudioSettingSnapshot(float bgmVolume, float seVolume, bool soundEnabled)
        {
            BgmVolume = bgmVolume;
            SeVolume = seVolume;
            SoundEnabled = soundEnabled;
        }
    }
}
