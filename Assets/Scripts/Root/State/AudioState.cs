#nullable enable

using Root.Service;

namespace Root.State
{
    public class AudioState
    {
        public float BgmVolume { get; set; } = 1f;
        public float SeVolume { get; set; } = 1f;
        public bool SoundEnabled { get; set; } = true;
        public BgmId? CurrentBgm { get; set; }
        /// 再生中クリップの基準音量。音量変更の即時反映の再計算に使う
        public float CurrentBgmBaseVolume { get; set; } = 1f;
    }
}
