#nullable enable

using UnityEngine;

namespace Root.View
{
    /// Canvas 配下の RectTransform を Screen.safeArea にぴったり合わせるリサイズコンポーネント
    /// 親 RectTransform は Canvas 全体を埋める RectTransform であることを前提とする
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class CanvasSafeAreaResizer : MonoBehaviour
    {
        const string LogPrefix = "[CanvasSafeAreaResizer]";
        const string NoCanvasMessage = LogPrefix + " No Canvas found in parents. Resizer disabled.";
        const string WorldSpaceMessage = LogPrefix + " World Space Canvas is not supported. Resizer disabled.";

        /// 上辺 (画面上端) のセーフエリア適用フラグ。false の場合は Canvas 上端 (anchorMax.y = 1) を採用する
        [SerializeField] bool _applyTop = true;

        /// 下辺 (画面下端) のセーフエリア適用フラグ。false の場合は Canvas 下端 (anchorMin.y = 0) を採用する
        [SerializeField] bool _applyBottom = true;

        /// 左辺 (画面左端) のセーフエリア適用フラグ。false の場合は Canvas 左端 (anchorMin.x = 0) を採用する
        [SerializeField] bool _applyLeft = true;

        /// 右辺 (画面右端) のセーフエリア適用フラグ。false の場合は Canvas 右端 (anchorMax.x = 1) を採用する
        [SerializeField] bool _applyRight = true;

        RectTransform? _rectTransform;
        Canvas? _parentCanvas;

        Rect _lastAppliedSafeArea;
        Vector2Int _lastAppliedScreenSize;
        ScreenOrientation _lastAppliedOrientation;
        Vector2 _lastAppliedAnchorMin;
        Vector2 _lastAppliedAnchorMax;
        bool _hasAppliedOnce;
        bool _isDirty = true;

        bool _noCanvasWarned;
        bool _worldSpaceWarned;

        void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            EnsureRectTransform();
            _parentCanvas = GetComponentInParent<Canvas>(true);
            _isDirty = true;
            _noCanvasWarned = false;
            _worldSpaceWarned = false;
        }

        void OnTransformParentChanged()
        {
            _parentCanvas = GetComponentInParent<Canvas>(true);
            _noCanvasWarned = false;
            _worldSpaceWarned = false;
            _isDirty = true;
        }

        void OnValidate()
        {
            _isDirty = true;
        }

        void Update()
        {
            if (!NeedsApply())
            {
                return;
            }
            Debug.Log("CanvasSafeAreaResizer: Applying safe area resize");

            Apply();
        }

        /// 強制的に再計算とアンカー書き換えを行う。テストおよび外部からの明示的トリガ用
        public void Apply()
        {
            EnsureRectTransform();
            EnsureParentCanvas();

            if (_parentCanvas == null)
            {
                LogWarningOnce(ref _noCanvasWarned, NoCanvasMessage);
                return;
            }

            if (_parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                LogWarningOnce(ref _worldSpaceWarned, WorldSpaceMessage);
                return;
            }

            var safeArea = Screen.safeArea;
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            var (anchorMin, anchorMax) = ComputeAnchors(
                safeArea,
                new Vector2(screenWidth, screenHeight),
                _applyTop, _applyBottom, _applyLeft, _applyRight);

            ApplyToRectTransform(_rectTransform!, anchorMin, anchorMax);

            _lastAppliedSafeArea = safeArea;
            _lastAppliedScreenSize = new Vector2Int(screenWidth, screenHeight);
            _lastAppliedOrientation = Screen.orientation;
            _lastAppliedAnchorMin = anchorMin;
            _lastAppliedAnchorMax = anchorMax;
            _hasAppliedOnce = true;
            _isDirty = false;
        }

        bool NeedsApply()
        {
            if (_isDirty || !_hasAppliedOnce)
            {
                return true;
            }
            if (ScreenStateChanged())
            {
                return true;
            }
            if (RectTransformAnchorChanged())
            {
                return true;
            }
            return false;
        }

        bool ScreenStateChanged()
        {
            return _lastAppliedSafeArea != Screen.safeArea
                || _lastAppliedScreenSize.x != Screen.width
                || _lastAppliedScreenSize.y != Screen.height
                || _lastAppliedOrientation != Screen.orientation;
        }

        bool RectTransformAnchorChanged()
        {
            if (_rectTransform == null)
            {
                return false;
            }
            return _rectTransform.anchorMin != _lastAppliedAnchorMin
                || _rectTransform.anchorMax != _lastAppliedAnchorMax;
        }

        void EnsureRectTransform()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        void EnsureParentCanvas()
        {
            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>(true);
            }
        }

        internal static (Vector2 anchorMin, Vector2 anchorMax) ComputeAnchors(
            Rect safeArea, Vector2 screenSize,
            bool applyTop, bool applyBottom, bool applyLeft, bool applyRight)
        {
            var anchorMin = new Vector2(
                applyLeft ? safeArea.x / screenSize.x : 0f,
                applyBottom ? safeArea.y / screenSize.y : 0f);
            var anchorMax = new Vector2(
                applyRight ? (safeArea.x + safeArea.width) / screenSize.x : 1f,
                applyTop ? (safeArea.y + safeArea.height) / screenSize.y : 1f);
            return (anchorMin, anchorMax);
        }

        static void ApplyToRectTransform(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        static void LogWarningOnce(ref bool emittedFlag, string message)
        {
            if (emittedFlag)
            {
                return;
            }

            emittedFlag = true;
            Debug.LogWarning(message);
        }
    }
}
