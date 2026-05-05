#nullable enable
using UnityEngine;

namespace History.Service
{
    /// 履歴カレンダーの段階表現に用いる設定 ScriptableObject。
    /// 段階は集中時間 (秒) に応じて 4 段階の Sprite 画像を切り替える形で表現する。
    /// 重要: コードで色 (Color) を生成・乗算せず、Sprite 画像差し替えのみで段階表現する。
    /// (要件 4.6 を満たすため、GetSpriteForSeconds は Color 値の生成・乗算を行わない)
    [CreateAssetMenu(menuName = "Cat/History/HistoryCalendarSettings")]
    public sealed class HistoryCalendarSettings : ScriptableObject
    {
        /// 4 段階分の Sprite (length 4 想定)。Inspector で 4 要素配列をデフォルト表示する。
        [SerializeField] Sprite[] _icons = new Sprite[4];

        /// 段階判定の閾値 (秒, length 3 想定)。デフォルト {0, 1500, 3600}。
        [SerializeField] int[] _thresholdsSeconds = { 0, 1500, 3600 };

        /// 月外日のセル描画用の Tint (Sprite の Image.color にのみ適用される想定)。
        [SerializeField] Color _outOfMonthTint = new Color(1f, 1f, 1f, 0.4f);

        /// 月外日セル描画用の Tint カラーを取得する。
        public Color OutOfMonthTint => _outOfMonthTint;

        /// 集中時間 (秒) に対応する Sprite を返す。
        /// 段階 1 (icons[0]): seconds == 0 または seconds < thresholdsSeconds[0]
        /// 段階 2 (icons[1]): thresholdsSeconds[0] <= seconds < thresholdsSeconds[1] (seconds > 0)
        /// 段階 3 (icons[2]): thresholdsSeconds[1] <= seconds < thresholdsSeconds[2]
        /// 段階 4 (icons[3]): thresholdsSeconds[2] <= seconds
        public Sprite? GetSpriteForSeconds(int seconds)
        {
            if (_icons == null || _icons.Length < 4)
            {
                Debug.LogWarning($"[HistoryCalendarSettings] _icons is null or length < 4. Returning null sprite.");
                return null;
            }

            if (_thresholdsSeconds == null || _thresholdsSeconds.Length < 3)
            {
                Debug.LogWarning($"[HistoryCalendarSettings] _thresholdsSeconds is null or length < 3. Returning icons[0].");
                return _icons[0];
            }

            int stage = FindStageIndex(seconds);
            return _icons[stage];
        }

        /// 段階インデックス (0..3) を計算する内部ヘルパ。
        /// 呼び出し側は事前に _thresholdsSeconds.Length >= 3 を保証していること。
        int FindStageIndex(int seconds)
        {
            if (seconds == 0 || seconds < _thresholdsSeconds[0])
            {
                return 0;
            }

            if (seconds < _thresholdsSeconds[1])
            {
                return 1;
            }

            if (seconds < _thresholdsSeconds[2])
            {
                return 2;
            }

            return 3;
        }
    }
}
