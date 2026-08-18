#nullable enable

namespace Root.AudioLogic
{
    /// 音量計算に関する純粋計算ロジック。UnityEngine 非依存
    public static class AudioVolumeLogic
    {
        /// 音量を [0, 1] に丸める。NaN は 0 に丸める
        public static float Clamp(float volume)
        {
            if (float.IsNaN(volume))
                return 0f;
            if (volume < 0f)
                return 0f;
            if (volume > 1f)
                return 1f;
            return volume;
        }

        /// BGM 音量を計算する。無効時は 0、それ以外はクリップ基準音量 × BGM 音量
        public static float CalcBgmVolume(bool soundEnabled, float clipBaseVolume, float bgmVolume)
        {
            if (!soundEnabled)
                return 0f;
            return Clamp(clipBaseVolume) * Clamp(bgmVolume);
        }

        /// SE 音量を計算する。無効時は 0、それ以外は SE 音量
        public static float CalcSeVolume(bool soundEnabled, float seVolume)
        {
            if (!soundEnabled)
                return 0f;
            return Clamp(seVolume);
        }
    }
}
