#nullable enable
using System;
using UnityEngine;

namespace EffectDemo
{
    /// 実行時に手続き生成する白抜き (アルファ) スプライト集。演出側は SpriteRenderer.color でティントして使う
    /// 一度生成したものはキャッシュし、以降は使い回す
    public static class EffectSprites
    {
        const int Size = 128;
        const int Ppu = 128;
        const int CloudSize = 128;
        const int CloudPpu = 128;
        const int SuperSample = 8;

        static Sprite? _circle;
        static Sprite? _ring;
        static Sprite? _star4;
        static Sprite? _heart;
        static Sprite? _paw;
        static Sprite? _cloud;
        static Sprite? _square;

        public static Sprite Circle => _circle ??= Build(nameof(Circle), InsideCircle, Size, Ppu);
        public static Sprite Ring => _ring ??= Build(nameof(Ring), InsideRing, Size, Ppu);
        public static Sprite Star4 => _star4 ??= Build(nameof(Star4), InsideStar4, Size, Ppu);
        public static Sprite Heart => _heart ??= Build(nameof(Heart), InsideHeart, Size, Ppu);
        public static Sprite Paw => _paw ??= Build(nameof(Paw), InsidePaw, Size, Ppu);
        public static Sprite Cloud => _cloud ??= Build(nameof(Cloud), InsideCloud, CloudSize, CloudPpu);
        public static Sprite Square => _square ??= Build(nameof(Square), InsideSquare, Size, Ppu);

        /// 形状データは Build が画素ごとに述語を呼ぶため、呼び出しごとに配列を作らず static に持つ
        static readonly (float x, float y, float rx, float ry)[] PawToes =
        {
                (-0.55f, 0.40f, 0.13f, 0.17f),
                (-0.20f, 0.58f, 0.13f, 0.17f),
                (0.20f, 0.58f, 0.13f, 0.17f),
                (0.55f, 0.40f, 0.13f, 0.17f),
        };

        static readonly (float x, float y, float r)[] CloudCircles =
        {
                (-0.5f, -0.2f, 0.36f),
                (-0.2f, 0.05f, 0.48f),
                (0.2f, 0.12f, 0.46f),
                (0.52f, -0.18f, 0.36f),
                (0f, -0.3f, 0.42f),
        };

        static bool InsideCircle(float x, float y) => x * x + y * y <= 0.85f * 0.85f;

        static bool InsideRing(float x, float y)
        {
            var d = Mathf.Sqrt(x * x + y * y);
            return d <= 0.85f && d >= 0.62f;
        }

        static bool InsideSquare(float x, float y) => Mathf.Abs(x) <= 0.75f && Mathf.Abs(y) <= 0.75f;

        /// アストロイド曲線。鋭い 4 方向の先端と凹んだ辺を持つキラキラ形状
        static bool InsideStar4(float x, float y) =>
            Mathf.Pow(Mathf.Abs(x), 2f / 3f) + Mathf.Pow(Mathf.Abs(y), 2f / 3f) <= 0.95f;

        /// 古典的なハート陰関数 (x^2+y^2-1)^3 <= x^2*y^3
        static bool InsideHeart(float x, float y)
        {
            var hx = x * 1.2f;
            var hy = y * 1.2f + 0.1f;
            var f = Mathf.Pow(hx * hx + hy * hy - 1f, 3f) - hx * hx * hy * hy * hy;
            return f <= 0f;
        }

        /// 肉球: 大きな掌 (下) + 指 4 つ (上に扇状)。掌と指、指同士の間には隙間を空ける
        static bool InsidePaw(float x, float y)
        {
            var padX = x / 0.55f;
            var padY = (y + 0.32f) / 0.38f;
            if (padX * padX + padY * padY <= 1f)
            {
                return true;
            }

            foreach (var toe in PawToes)
            {
                var tx = (x - toe.x) / toe.rx;
                var ty = (y - toe.y) / toe.ry;
                if (tx * tx + ty * ty <= 1f)
                {
                    return true;
                }
            }

            return false;
        }

        /// 円 4 つを重ねたもくもく雲
        static bool InsideCloud(float x, float y)
        {
            foreach (var c in CloudCircles)
            {
                var dx = x - c.x;
                var dy = y - c.y;
                if (dx * dx + dy * dy <= c.r * c.r)
                {
                    return true;
                }
            }

            return false;
        }

        /// テクセルごとに SuperSample^2 回サブサンプリングしてカバレッジをアルファに変換し、縁を滑らかにする
        static Sprite Build(string name, Func<float, float, bool> inside, int size, int ppu)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"EffectSprite_{name}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    var coverage = 0;
                    for (var sy = 0; sy < SuperSample; sy++)
                    {
                        for (var sx = 0; sx < SuperSample; sx++)
                        {
                            var u = (px + (sx + 0.5f) / SuperSample) / size * 2f - 1f;
                            var v = (py + (sy + 0.5f) / SuperSample) / size * 2f - 1f;
                            if (inside(u, v))
                            {
                                coverage++;
                            }
                        }
                    }

                    var alpha = (byte)(255 * coverage / (SuperSample * SuperSample));
                    pixels[py * size + px] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
