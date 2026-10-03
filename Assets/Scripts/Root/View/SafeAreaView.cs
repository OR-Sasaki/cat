#nullable enable

using UnityEngine;

namespace Root.View
{
    /// Screen.safeArea を自身の anchorMin / anchorMax に反映する受動 View
    /// Canvas 直下のフルストレッチな入れ物に付け、画面端に寄せたい UI をその子に置く
    /// 判断ロジックを持たないため Service / DI は介さず、各シーンのヒエラルキーに直接置く
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaView : MonoBehaviour
    {
        [SerializeField] bool _applyLeft = true;
        [SerializeField] bool _applyRight = true;
        [SerializeField] bool _applyTop = true;
        [SerializeField] bool _applyBottom = true;

        RectTransform _rect = null!;

        /// 端末回転・分割表示・Device Simulator の機種変更を拾うため毎フレーム比較する
        Rect _lastSafeArea;
        int _lastScreenWidth;
        int _lastScreenHeight;
        bool _hasApplied;

        void Awake()
        {
            _rect = (RectTransform)transform;
        }

        void OnEnable()
        {
            _hasApplied = false;
            Apply(Screen.safeArea, Screen.width, Screen.height);
        }

        void Update()
        {
            // Screen.safeArea はネイティブ呼び出しなので 1 フレーム 1 回だけ読む
            var safeArea = Screen.safeArea;
            var width = Screen.width;
            var height = Screen.height;

            if (_hasApplied && safeArea == _lastSafeArea && width == _lastScreenWidth && height == _lastScreenHeight)
            {
                return;
            }

            Apply(safeArea, width, height);
        }

        void Apply(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
            _hasApplied = true;

            // 適用しない辺は画面端まで戻す (Rect.xMin などの setter は反対側の辺を保ったまま広がる)
            if (_applyLeft == false)
            {
                safeArea.xMin = 0f;
            }

            if (_applyRight == false)
            {
                safeArea.xMax = screenWidth;
            }

            if (_applyBottom == false)
            {
                safeArea.yMin = 0f;
            }

            if (_applyTop == false)
            {
                safeArea.yMax = screenHeight;
            }

            CalculateAnchors(safeArea, screenWidth, screenHeight, out var anchorMin, out var anchorMax);

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            // anchor を変えた後に offset を潰す順序でないと sizeDelta が残る
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }

        /// safeArea (左下原点のピクセル矩形) を anchor の 0..1 へ正規化する
        /// 実機なしで検証できるよう副作用を持たない静的関数として公開する
        public static void CalculateAnchors(
            Rect safeArea,
            int screenWidth,
            int screenHeight,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenWidth),
                Mathf.Clamp01(safeArea.yMin / screenHeight));
            anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenWidth),
                Mathf.Clamp01(safeArea.yMax / screenHeight));

            // safeArea が未初期化 (ゼロ矩形) の端末・タイミングでは UI が消えるため全画面へ退避する
            if (anchorMin.x >= anchorMax.x || anchorMin.y >= anchorMax.y)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
            }
        }
    }
}
