#nullable enable

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugPanel.View
{
    /// デバッグ UI をコードから組み立てるためのヘルパー
    /// デバッグ専用の画面はプレハブを持たないため、生成とアンカー設定をここに集約する
    static class DebugUiFactory
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            return label;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color background,
            Action onClick,
            float fontSize = 26f)
        {
            var image = CreateImage(name, parent, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var text = CreateText("Label", image.transform, label, fontSize, TextAlignmentOptions.Midline);
            Stretch(text.rectTransform);
            return button;
        }

        /// 親いっぱいに広げる
        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// 親の上端から top だけ下げた位置に、横幅いっぱい (左右 horizontalPadding) の帯を置く
        public static RectTransform AnchorTop(RectTransform rect, float top, float height, float horizontalPadding)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-horizontalPadding * 2f, height);
            rect.anchoredPosition = new Vector2(0f, -top);
            return rect;
        }

        /// 親の下端から bottom だけ上げた位置に、横幅いっぱい (左右 horizontalPadding) の帯を置く
        public static RectTransform AnchorBottom(RectTransform rect, float bottom, float height, float horizontalPadding)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-horizontalPadding * 2f, height);
            rect.anchoredPosition = new Vector2(0f, bottom);
            return rect;
        }

        /// 親の右上に固定サイズで置く
        public static RectTransform AnchorTopRight(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(-right, -top);
            return rect;
        }

        /// 上下を余白指定で埋める
        public static RectTransform AnchorFill(RectTransform rect, float top, float bottom, float horizontalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(horizontalPadding, bottom);
            rect.offsetMax = new Vector2(-horizontalPadding, -top);
            return rect;
        }

        /// forceExpandWidth を true にすると全要素が均等幅になる
        /// LayoutElement の preferredWidth を効かせたい行では false を指定する
        /// (childForceExpandWidth は子の flexibleWidth を強制的に 1 以上へ引き上げるため)
        public static HorizontalLayoutGroup AddHorizontalGroup(
            RectTransform rect,
            float spacing,
            RectOffset? padding = null,
            bool forceExpandWidth = true)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = forceExpandWidth;
            group.childForceExpandHeight = true;
            return group;
        }

        public static VerticalLayoutGroup AddVerticalGroup(RectTransform rect, float spacing, RectOffset? padding = null)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        public static void SetPreferredHeight(RectTransform rect, float height)
        {
            var element = GetOrAddLayoutElement(rect);
            element.minHeight = height;
            element.preferredHeight = height;
        }

        public static void SetPreferredWidth(RectTransform rect, float width)
        {
            var element = GetOrAddLayoutElement(rect);
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        public static void SetFlexibleWidth(RectTransform rect, float weight)
        {
            GetOrAddLayoutElement(rect).flexibleWidth = weight;
        }

        static LayoutElement GetOrAddLayoutElement(RectTransform rect)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = rect.gameObject.AddComponent<LayoutElement>();
            }
            return element;
        }
    }
}
