using UnityEngine;

namespace Root.Service
{
    public enum PlayerPrefsKey
    {
        Outfit,
        UserEquippedOutfit,
        IsoGrid,
        TimerSetting,
        UserItemInventory,
        UserPoint,
        TimerRecord,
        TimerYarnReward,
        RewardAdDailyCount,
        MenuSetting,
        AudioSetting,
    }

    public class PlayerPrefsService
    {
        public void Save<T>(PlayerPrefsKey key, T value)
        {
            var json = JsonUtility.ToJson(value);
            Debug.Log($"PlayerPrefs Saved: {key} {json}");
            PlayerPrefs.SetString(key.ToString(), json);
        }

        /// SetString はメモリ上のキャッシュを書き換えるだけで、ディスクへの書き出しは
        /// この Flush か正常終了時 (OnApplicationQuit) の自動保存で起きる。
        /// 同期ディスク書き込みなので毎フレームやホットパスからは呼ばず、
        /// バックグラウンド遷移時 (AppLifecycleManager) と、
        /// 複数キーをまたぐ取引の完了時 (購入・ガチャ) にまとめて呼ぶ
        public void Flush()
        {
            PlayerPrefs.Save();
        }

        public T Load<T>(PlayerPrefsKey key)
        {
            var json = PlayerPrefs.GetString(key.ToString());
            Debug.Log($"PlayerPrefs Loaded: {key} {json}");
            return JsonUtility.FromJson<T>(json);
        }
    }
}
