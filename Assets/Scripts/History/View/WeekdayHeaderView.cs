#nullable enable
using TMPro;
using UnityEngine;

namespace History.View
{
    /// 曜日ヘッダー (日・月・火・水・木・金・土) の固定表示 View。
    /// Inspector で 7 ラベルを配置し、未設定時は Awake で既定の曜日テキストを補完する。
    /// 動的に曜日順を変更する処理は持たない。
    public class WeekdayHeaderView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[]? _weekdayLabels;

        private void Awake()
        {
            if (_weekdayLabels == null || _weekdayLabels.Length != 7)
            {
                Debug.LogWarning("[WeekdayHeaderView] expects 7 labels (Sun..Sat).", this);
                return;
            }

            string[] expected = { "日", "月", "火", "水", "木", "金", "土" };
            for (int i = 0; i < expected.Length; i++)
            {
                var label = _weekdayLabels[i];
                if (label == null)
                {
                    Debug.LogWarning("[WeekdayHeaderView] expects 7 labels (Sun..Sat).", this);
                    continue;
                }

                if (string.IsNullOrEmpty(label.text))
                {
                    /// Inspector 未設定時は既定の曜日テキストで補完する静的初期化フォールバック。
                    label.text = expected[i];
                }
                else if (label.text != expected[i])
                {
                    Debug.LogWarning($"[WeekdayHeaderView] label[{i}] expected '{expected[i]}' but was '{label.text}'.", this);
                }
            }
        }
    }
}
