#nullable enable
using System.Collections.Generic;
using Cat.Character;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
#endif

namespace EffectDemo
{
    /// 着せ替え / 家具設置の演出 8 種をボタンでプレビューするデモ画面
    public class EffectDemoView : MonoBehaviour
    {
        [SerializeField] CharacterView _character = null!;
        [SerializeField] Outfit[] _cloths = null!;
        [SerializeField] Transform _furniture = null!;

        static readonly string[] OutfitLabels = { "キラキラ・ポップ", "もくもく変身", "ハート・シャワー", "肉球スタンプ", "もくもく＋キラキラ＋肉球" };
        static readonly string[] FurnitureLabels = { "ぽよん着地", "キラキラ・シャワー", "ぽんっ紙吹雪", "肉球リング" };

        int _outfitIndex;

        public void PlayOutfit(int kind)
        {
            var bounds = ComputeBounds(_character.transform);
            var nextIndex = (_outfitIndex + 1) % Mathf.Max(_cloths.Length, 1);
            var next = _cloths[nextIndex];
            _outfitIndex = nextIndex;

            OutfitChangeEffect.Play(_character.transform, bounds, (OutfitEffectKind)kind, () => _character.SetOutfit(next));
        }

        public void PlayFurniture(int kind)
        {
            FurniturePlaceEffect.Play(_furniture, _furniture.position, (FurnitureEffectKind)kind);
        }

#if UNITY_EDITOR
        /// 検証用: 演出を再生してから delay 秒後 (実時間) に Play モードを一時停止する。
        /// 停止後は外部から capture_game_view 等で撮影できる
        public void PauseAfter(int kind, bool outfit, float delay)
        {
            StartCoroutine(PauseAfterRoutine(kind, outfit, delay));
        }

        IEnumerator PauseAfterRoutine(int kind, bool outfit, float delay)
        {
            // 毎フレームの deltaTime を固定し、エディタのフォーカス状態による実時間のばらつきを無視できるようにする
            Time.captureDeltaTime = 1f / 60f;
            if (outfit) PlayOutfit(kind); else PlayFurniture(kind);
            var elapsed = 0f;
            while (elapsed < delay)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            UnityEditor.EditorApplication.isPaused = true;
            Time.captureDeltaTime = 0f;
        }
#endif

        void OnGUI()
        {
            var buttonWidth = Screen.width * 0.4f;
            const float buttonHeight = 70f;
            const float margin = 16f;

            var style = new GUIStyle(GUI.skin.button) { fontSize = 28 };

            for (var i = 0; i < OutfitLabels.Length; i++)
            {
                var rect = new Rect(margin, margin + i * (buttonHeight + margin), buttonWidth, buttonHeight);
                if (GUI.Button(rect, OutfitLabels[i], style))
                {
                    PlayOutfit(i);
                }
            }

            for (var i = 0; i < FurnitureLabels.Length; i++)
            {
                var rect = new Rect(Screen.width - margin - buttonWidth, margin + i * (buttonHeight + margin), buttonWidth, buttonHeight);
                if (GUI.Button(rect, FurnitureLabels[i], style))
                {
                    PlayFurniture(i);
                }
            }
        }

        // キャラの各パーツはリグ位置合わせのため、実際の絵より大きな透明キャンバスを共通サイズで使っている。
        // そのため素の SpriteRenderer.bounds は使わず、非透過ピクセルのみを包む範囲を求める
        static readonly Dictionary<Sprite, Rect> AlphaRectCache = new();

        static Bounds ComputeBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>();
            var bounds = new Bounds(root.position, Vector3.zero);
            var hasBounds = false;

            foreach (var renderer in renderers)
            {
                if (renderer.sprite == null)
                {
                    continue;
                }

                var tight = TightWorldBounds(renderer);
                if (!hasBounds)
                {
                    bounds = tight;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(tight);
            }

            return bounds;
        }

        static Bounds TightWorldBounds(SpriteRenderer renderer)
        {
            var sprite = renderer.sprite;
            if (!AlphaRectCache.TryGetValue(sprite, out var pixelRect))
            {
                pixelRect = ComputeAlphaPixelRect(sprite);
                AlphaRectCache[sprite] = pixelRect;
            }

            var ppu = sprite.pixelsPerUnit;
            var pivot = sprite.pivot;
            var min = new Vector3((pixelRect.xMin - pivot.x) / ppu, (pixelRect.yMin - pivot.y) / ppu, 0f);
            var max = new Vector3((pixelRect.xMax - pivot.x) / ppu, (pixelRect.yMax - pivot.y) / ppu, 0f);

            var t = renderer.transform;
            var bounds = new Bounds(t.TransformPoint(min), Vector3.zero);
            bounds.Encapsulate(t.TransformPoint(max));
            bounds.Encapsulate(t.TransformPoint(new Vector3(min.x, max.y, 0f)));
            bounds.Encapsulate(t.TransformPoint(new Vector3(max.x, min.y, 0f)));
            return bounds;
        }

        /// テクスチャを RenderTexture 経由で読み取り、アルファが乗っているピクセルの矩形を求める
        /// (Read/Write が無効なテクスチャでも読める)。スプライトの矩形がテクスチャ全体を占める前提
        static Rect ComputeAlphaPixelRect(Sprite sprite)
        {
            var texture = sprite.texture;
            var width = texture.width;
            var height = texture.height;

            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;

            var readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readable.Apply(false);

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = readable.GetPixels32();
            Object.DestroyImmediate(readable);

            const byte alphaThreshold = 8;
            int minX = width, minY = height, maxX = 0, maxY = 0;
            var found = false;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a <= alphaThreshold)
                    {
                        continue;
                    }

                    found = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return found ? Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1) : new Rect(0, 0, width, height);
        }
    }
}
