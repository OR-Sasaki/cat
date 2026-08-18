#nullable enable

namespace Root.Service
{
    public interface IAudioService
    {
        /// SE をワンショット再生する。未登録識別子はログのみで無視
        void PlaySe(SeId id, float pitch = 1f);
        /// BGM をループ再生する。同一 BGM は何もしない。切替はクロスフェード
        void PlayBgm(BgmId id);
        /// BGM をフェードアウトして停止する。未再生時は何もしない
        void StopBgm();
        /// 0-1 に丸めて適用し、再生中 BGM へ即時反映 + 永続化
        void SetBgmVolume(float volume);
        /// 0-1 に丸めて適用し、以降の SE へ適用 + 永続化
        void SetSeVolume(float volume);
        /// サウンド全体の有効/無効。無効時は BGM を無音化し SE を再生しない + 永続化
        void SetSoundEnabled(bool enabled);
        /// 現在のオーディオ設定のイミュータブルスナップショット
        AudioSettingSnapshot GetSettingSnapshot();
    }
}
