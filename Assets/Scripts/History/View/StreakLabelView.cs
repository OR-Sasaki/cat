using TMPro;
using UnityEngine;
using VContainer;
using History.Service;

namespace History.View
{
    /// 連続使用日数 (ストリーク) を画面上部に「N」で表示する View。
    /// シーン入場時に 1 度算出して反映する (要件 5.1)。
    public class StreakLabelView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _streakText;

        StreakCalculator _streakCalculator;

        [Inject]
        public void Construct(StreakCalculator streakCalculator)
        {
            _streakCalculator = streakCalculator;
        }

        void Start()
        {
            var streak = _streakCalculator.Calculate();
            _streakText.text = $"{streak}";
        }
    }
}
