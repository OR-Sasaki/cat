#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cat.Character
{
    /// Walk / Run のループクリップを手続き的に生成するエディタ拡張。
    /// 足裏の接地点をワールド空間の地面に固定したまま脚の回転ピボットを逆算する (簡易 IK) ので、
    /// 体が浮いたり足が滑ったりしない。パラメータを変えたら
    /// Tools/Cat/Rebuild Locomotion Clips を実行して .anim を作り直す。
    public static class CatLocomotionClipBuilder
    {
        const string WalkClipPath = "Assets/Arts/Character/Animation/Walk.anim";
        const string RunClipPath = "Assets/Arts/Character/Animation/Run.anim";
        const string IdleClipPath = "Assets/Arts/Character/Animation/Idle.anim";

        /// 直立 (Idle) で体を既定の高さからどれだけ下げて構えるか (負で低く)。
        /// 足裏は地面に残すので、下げた分だけ脚が体に隠れて短く見える。
        /// Idle.anim 自体はアーティスト作成なので、Root と両 FootRoot の位置カーブを定数で足すだけ
        /// (0 にするとそのカーブを削除して元に戻る)。Walk / Run の BodyDrop はこの値からの相対ではなく
        /// 既定の高さ (0) からの絶対値なので、ここを変えたら同じだけ動かすと段差が保てる
        const float IdleBodyDrop = -0.08f;

        /// 休止ポーズでの脚の回転ピボット位置 (CharacterView.prefab)
        static readonly Vector2 FrontPivotRest = new Vector2(-0.3f, -2.61f);
        static readonly Vector2 BackPivotRest = new Vector2(-2.1f, -2.33f);

        /// 全パーツのスプライトは 1000px / PPU 100 / ピボット中央の共通キャンバスで、
        /// どのパーツも既定ポーズでは Root 原点に重なる。そのため不透明領域の下端が
        /// そのまま「既定ポーズでの接地 Y」になる (CharacterView ローカル空間)。
        const float FrontPawGroundY = -3.65f;
        const float BackPawGroundY = -3.53f;

        /// 既定ポーズでの足裏接地点 X。歩幅はこの位置を中心に前後へ振る
        const float FrontPawRestX = -0.365f;
        const float BackPawRestX = -2.15f;

        /// *FootRoot (回転ピボット) から足裏接地点へのオフセット。
        /// FootRoot の子スプライトの localPosition と不透明領域下端から算出
        static readonly Vector2 FrontPawOffset = new Vector2(-0.065f, -1.04f);
        static readonly Vector2 BackPawOffset = new Vector2(-0.05f, -1.2f);

        // 進行方向は Root ローカルの -X (CharacterWalk が右向き時に localScale.x を反転させる)

        /// Character.prefab の値。歩幅を接地させるのに必要
        const float CharacterScale = 0.23634663f;
        const float WalkWorldSpeed = 0.9f;
        const float RunWorldSpeed = 1.9f;

        const float FrameRate = 60f;

        [MenuItem("Tools/Cat/Rebuild Locomotion Clips")]
        public static void RebuildAll()
        {
            Build(CreateWalkGait());
            Build(CreateRunGait());
            ApplyIdleBodyDrop(IdleBodyDrop);
            AssetDatabase.SaveAssets();
            Debug.Log("[CatLocomotionClipBuilder] Walk.anim / Run.anim を再生成し、Idle.anim の体の高さを更新しました");
        }

        /// 歩き: 接地率 62% (両足接地が入る) / 腰は接地で沈み抜重で戻る。
        /// 足は靴なので接地中はほぼ水平のまま体の下を前後に送る。LegAngle を小さくした分だけ
        /// 歩幅が脚の付け根の平行移動に回るので、回転だけで足踏みしているようには見えない
        static Gait CreateWalkGait()
        {
            return new Gait
            {
                ClipPath = WalkClipPath,
                Cycle = 1.0f,
                Samples = 30,
                DecorSamples = 16,
                Duty = 0.66f,
                LegAngle = 16f,
                StepLift = 0.36f,
                BodyDrop = -0.10f,
                SwingSnap = 2.5f,
                LiftPeak = 0.6f,
                FrontPawCenterShift = -0.6f,
                BackPawCenterShift = 0.75f,
                BackPivotMinX = -2.6f,
                GroundSpeed = WalkWorldSpeed / CharacterScale,
                // 半周期ぶんの (位相, Root Y)。接地で一気に沈み、抜重で既定の高さまで戻ってしばらく浮く。
                // 既定の高さより上には行かないので Idle から繋いでも浮かない
                BobTable = new[]
                {
                    new Vector2(0.00f, -0.17f),
                    new Vector2(0.06f, -0.21f),
                    new Vector2(0.20f, 0.00f),
                    new Vector2(0.32f, 0.00f),
                    new Vector2(0.42f, -0.10f),
                },
                LeanBase = 2.2f,
                LeanAmp = 1.2f,
                LeanPhase = 0.12f,
                TailBase = 9f,
                TailAmp = 14f,
                TailPhase = 0.18f,
                TailHarmonic = 1,
                FrontHandBase = 16.4f,
                FrontHandAmp = 16f,
                BackHandBase = 9.7f,
                BackHandAmp = -17f,
                HandPhase = 0.06f,
                ClothBase = 11.43f,
                ClothAmp = 4f,
                ClothPhase = 0.2f,
                TorsoAmp = -2.5f,
                TorsoPhase = 0.2f,
                SquashIn = 0.035f,
                SquashOut = 0.02f,
                SquashPhase = 0.05f,
            };
        }

        /// 走り: 接地率 38% で短い空中期が入る。空中期の浮きは弾道的に小さく保ち、
        /// 上下動の大半は接地中の沈み込みで作る
        static Gait CreateRunGait()
        {
            return new Gait
            {
                ClipPath = RunClipPath,
                Cycle = 0.6f,
                Samples = 36,
                DecorSamples = 16,
                Duty = 0.4f,
                LegAngle = 26f,
                StepLift = 0.4f,
                BodyDrop = -0.06f,
                SwingSnap = 2f,
                LiftPeak = 0.6f,
                FrontPawCenterShift = -0.4f,
                BackPawCenterShift = 0.9f,
                BackPivotMinX = -2.6f,
                GroundSpeed = RunWorldSpeed / CharacterScale,
                // 着地 (0.00) → 最大沈み (0.12) → 伸び上がり (0.28) → 離地 (0.40) → 頂点 (0.45) → 着地 (0.50)
                BobTable = new[]
                {
                    new Vector2(0.00f, -0.06f),
                    new Vector2(0.12f, -0.22f),
                    new Vector2(0.28f, -0.12f),
                    new Vector2(0.40f, -0.04f),
                    new Vector2(0.45f, 0.08f),
                },
                LeanBase = 12f,
                LeanAmp = 3f,
                LeanPhase = 0.12f,
                TailBase = 15f,
                TailAmp = 16f,
                TailPhase = 0.12f,
                TailHarmonic = 2,
                FrontHandBase = 22.5f,
                FrontHandAmp = 46f,
                BackHandBase = -25f,
                BackHandAmp = -48f,
                HandPhase = 0.03f,
                ClothBase = 2.65f,
                ClothAmp = -10f,
                ClothPhase = 0.06f,
                TorsoAmp = -3f,
                TorsoPhase = 0.2f,
                SquashIn = 0.045f,
                SquashOut = 0.025f,
                SquashPhase = 0.12f,
            };
        }

        static void Build(Gait gait)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(gait.ClipPath);
            if (clip == null)
            {
                Debug.LogError($"[CatLocomotionClipBuilder] クリップが見つかりません: {gait.ClipPath}");
                return;
            }

            // 1 本の脚が接地中に体に対して後ろへ送られる距離の半分。
            // 接地中の足裏速度が進行速度と一致するので、ワールド空間では完全に静止する
            var reach = gait.GroundSpeed * gait.Duty * gait.Cycle * 0.5f;
            var bob = BuildPeriodicCurve(gait.BobTable, 0.5f);

            var tracks = new Dictionary<string, List<Keyframe>>();
            var smooth = new Dictionary<string, List<Keyframe>>();

            // 接地解を載せるカーブは線形補間で密にサンプリングする。
            // Samples は偶数でないと半周期ごとのサンプル位相がずれて左右の歩幅が揃わない
            for (var i = 0; i <= gait.Samples; i++)
            {
                var phase = (float)i / gait.Samples;
                var time = phase * gait.Cycle;

                var rootY = bob.Evaluate(Mathf.Repeat(phase, 0.5f)) + gait.BodyDrop;
                var rootPos = new Vector2(0f, rootY);
                var rootZ = gait.LeanBase + gait.LeanAmp * Cos(phase - gait.LeanPhase, 2);

                SolveLeg(gait, reach, phase, FrontPawRestX + gait.FrontPawCenterShift, FrontPawGroundY, FrontPawOffset,
                    rootPos, rootZ, float.NegativeInfinity, out var frontPivot, out var frontAngle);
                // 遠い脚は体の後ろに描かれるので、前に出し過ぎると付け根 (スプライト上端) が体の輪郭から
                // はみ出して見える。振りの中心を後ろへずらし、それでも足りない分は付け根の位置を止める
                SolveLeg(gait, reach, phase + 0.5f, BackPawRestX + gait.BackPawCenterShift, BackPawGroundY,
                    BackPawOffset, rootPos, rootZ, gait.BackPivotMinX, out var backPivot, out var backAngle);

                Add(tracks, "Root/m_LocalPosition.y", time, rootY);
                Add(tracks, "Root/localEulerAnglesRaw.z", time, rootZ);

                Add(tracks, "Root/FrontFootRoot/m_LocalPosition.x", time, frontPivot.x);
                Add(tracks, "Root/FrontFootRoot/m_LocalPosition.y", time, frontPivot.y);
                Add(tracks, "Root/FrontFootRoot/localEulerAnglesRaw.z", time, frontAngle);

                Add(tracks, "Root/BackFootRoot/m_LocalPosition.x", time, backPivot.x);
                Add(tracks, "Root/BackFootRoot/m_LocalPosition.y", time, backPivot.y);
                Add(tracks, "Root/BackFootRoot/localEulerAnglesRaw.z", time, backAngle);
            }

            // 接地解に関わらない装飾パーツは粗いキー + 自動タンジェントで十分
            for (var i = 0; i <= gait.DecorSamples; i++)
            {
                var phase = (float)i / gait.DecorSamples;
                var time = phase * gait.Cycle;

                // 腕は対角の脚と逆位相に振る
                Add(smooth, "Root/FrontHandRoot/localEulerAnglesRaw.z", time,
                    gait.FrontHandBase + gait.FrontHandAmp * Cos(phase - gait.HandPhase, 1));
                Add(smooth, "Root/BackHandRoot/localEulerAnglesRaw.z", time,
                    gait.BackHandBase + gait.BackHandAmp * Cos(phase - gait.HandPhase, 1));
                // 尻尾と服は本体より遅れて追従させる
                Add(smooth, "Root/TailRoot/localEulerAnglesRaw.z", time,
                    gait.TailBase + gait.TailAmp * Cos(phase - gait.TailPhase, gait.TailHarmonic));
                Add(smooth, "Root/FrontHandRoot/ClothFrontRoot/localEulerAnglesRaw.z", time,
                    gait.ClothBase + gait.ClothAmp * Cos(phase - gait.ClothPhase, 1));

                // 腰の沈みに合わせた体の潰し。接地で潰れ、抜重・空中で伸びる
                var squash = 0.5f + 0.5f * Cos(phase - gait.SquashPhase, 2);
                Add(smooth, "Root/BodyRoot/localEulerAnglesRaw.z", time,
                    gait.TorsoAmp * Cos(phase - gait.TorsoPhase, 2));
                Add(smooth, "Root/BodyRoot/m_LocalScale.x", time,
                    1f + gait.SquashIn * 0.8f * squash - gait.SquashOut * 0.8f * (1f - squash));
                Add(smooth, "Root/BodyRoot/m_LocalScale.y", time,
                    1f - gait.SquashIn * squash + gait.SquashOut * (1f - squash));
            }

            // 値が動かない成分も 3 軸そろえておかないと Unity が Transform カーブとしてまとめられない
            AddConstant(tracks, "Root/m_LocalPosition.x", gait.Cycle, 0f);
            AddConstant(tracks, "Root/m_LocalPosition.z", gait.Cycle, 0f);
            AddConstant(tracks, "Root/localEulerAnglesRaw.x", gait.Cycle, 0f);
            AddConstant(tracks, "Root/localEulerAnglesRaw.y", gait.Cycle, 0f);
            foreach (var leg in new[] { "Root/FrontFootRoot", "Root/BackFootRoot" })
            {
                AddConstant(tracks, leg + "/m_LocalPosition.z", gait.Cycle, 0f);
                AddConstant(tracks, leg + "/localEulerAnglesRaw.x", gait.Cycle, 0f);
                AddConstant(tracks, leg + "/localEulerAnglesRaw.y", gait.Cycle, 0f);
            }
            foreach (var part in new[]
                     {
                         "Root/FrontHandRoot", "Root/BackHandRoot", "Root/TailRoot",
                         "Root/FrontHandRoot/ClothFrontRoot", "Root/BodyRoot",
                     })
            {
                AddConstant(tracks, part + "/localEulerAnglesRaw.x", gait.Cycle, 0f);
                AddConstant(tracks, part + "/localEulerAnglesRaw.y", gait.Cycle, 0f);
            }
            AddConstant(tracks, "Root/BodyRoot/m_LocalScale.z", gait.Cycle, 1f);

            Undo.RecordObject(clip, "Rebuild Locomotion Clip");
            clip.ClearCurves();
            clip.frameRate = FrameRate;

            foreach (var track in smooth)
            {
                var curve = new AnimationCurve(track.Value.ToArray());
                for (var i = 0; i < curve.length; i++)
                {
                    curve.SmoothTangents(i, 0f);
                }

                SetCurve(clip, track.Key, curve);
            }

            foreach (var track in tracks)
            {
                var curve = new AnimationCurve(track.Value.ToArray());
                for (var i = 0; i < curve.length; i++)
                {
                    // 接地中の足裏は等速で後ろへ送られる。線形補間にしておかないとキーの間で滑る
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }

                SetCurve(clip, track.Key, curve);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.startTime = 0f;
            settings.stopTime = gait.Cycle;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        /// Idle.anim の体の高さを変える。アーティストの揺れカーブには触れず、位置カーブだけを足し引きする
        static void ApplyIdleBodyDrop(float drop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (clip == null)
            {
                Debug.LogError($"[CatLocomotionClipBuilder] クリップが見つかりません: {IdleClipPath}");
                return;
            }

            Undo.RecordObject(clip, "Apply Idle Body Drop");
            var enabled = !Mathf.Approximately(drop, 0f);
            // 体 (Root) を下げた分だけ脚のピボットを上げ、足裏をもとの接地位置に残す
            SetConstantPosition(clip, "Root", new Vector2(0f, drop), enabled);
            SetConstantPosition(clip, "Root/FrontFootRoot", FrontPivotRest - new Vector2(0f, drop), enabled);
            SetConstantPosition(clip, "Root/BackFootRoot", BackPivotRest - new Vector2(0f, drop), enabled);
            EditorUtility.SetDirty(clip);
        }

        /// 位置カーブを定数で書く (enabled=false なら削除)。3 軸そろえないと Transform カーブにまとまらない
        static void SetConstantPosition(AnimationClip clip, string path, Vector2 value, bool enabled)
        {
            var axes = new[] { "x", "y", "z" };
            var values = new[] { value.x, value.y, 0f };
            for (var i = 0; i < axes.Length; i++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axes[i]);
                AnimationCurve? curve = null;
                if (enabled)
                {
                    curve = new AnimationCurve(new Keyframe(0f, values[i]), new Keyframe(clip.length, values[i]));
                    AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyLeftTangentMode(curve, 1, AnimationUtility.TangentMode.Linear);
                }

                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        /// 足裏の目標位置から *FootRoot の位置を逆算する
        static void SolveLeg(Gait gait, float reach, float phase, float centerX, float groundY, Vector2 pawOffset,
            Vector2 rootPos, float rootZ, float pivotMinX, out Vector2 pivot, out float angle)
        {
            var u = Mathf.Repeat(phase, 1f);
            var span = reach * 2f;
            float pawX;
            float pawY;

            if (u < gait.Duty)
            {
                // 接地期: 足裏はワールド空間で静止 = 体に対して進行速度ぶん後ろ (+X) へ等速で送られる
                var s = u / gait.Duty;
                pawX = centerX - reach + span * s;
                pawY = groundY;
                angle = Mathf.Lerp(-gait.LegAngle, gait.LegAngle, s);
            }
            else
            {
                // 遊脚期: 離地時と着地時の速度を接地期とつないだ 3 次エルミート。
                // 両端でワールド空間の足裏速度が 0 になるので、着地で突っかかったり滑ったりしない
                var w = (u - gait.Duty) / (1f - gait.Duty);
                // 前半で一気に振り出して後半はゆっくり着地位置へ収める (アニメ的なタメとツメ)。
                // 両端の傾きは 1 のままなので離地・着地の速度条件は崩れない
                w += gait.SwingSnap * w * w * (1f - w) * (1f - w);
                var carry = (1f - gait.Duty) / gait.Duty;
                pawX = Hermite(centerX + reach, span * carry, centerX - reach, span * carry, w);
                var lift = Mathf.Sin(Mathf.PI * Mathf.Pow(w, gait.LiftPeak));
                pawY = groundY + gait.StepLift * lift * lift;
                angle = Hermite(gait.LegAngle, 2f * gait.LegAngle * carry, -gait.LegAngle,
                    2f * gait.LegAngle * carry, w);
            }

            var pawInRoot = Rotate(new Vector2(pawX, pawY) - rootPos, -rootZ);
            pivot = pawInRoot - Rotate(pawOffset, angle);
            if (pivot.x < pivotMinX)
            {
                // 付け根が体の輪郭からはみ出す位置までは出さない。その分だけ足裏が滑るのは許容する
                pivot.x = pivotMinX;
            }
        }

        /// "親/子/プロパティ名" 形式のキーをパスとプロパティに分けてクリップへ書き込む
        static void SetCurve(AnimationClip clip, string key, AnimationCurve curve)
        {
            var split = key.LastIndexOf('/');
            var path = key.Substring(0, split);
            var property = key.Substring(split + 1);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        static float Hermite(float p0, float m0, float p1, float m1, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * p0
                   + (t3 - 2f * t2 + t) * m0
                   + (-2f * t3 + 3f * t2) * p1
                   + (t3 - t2) * m1;
        }

        static float Cos(float phase, int harmonic)
        {
            return Mathf.Cos(2f * Mathf.PI * harmonic * phase);
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(rad);
            var s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// (位相, 値) のテーブルを period ごとに繰り返したカーブを作る。
        /// 前後に 1 周期ぶん足してから自動タンジェントを掛けるので、継ぎ目でも滑らかに繋がる
        static AnimationCurve BuildPeriodicCurve(Vector2[] table, float period)
        {
            var keys = new List<Keyframe>();
            for (var repeat = -1; repeat <= 2; repeat++)
            {
                foreach (var entry in table)
                {
                    keys.Add(new Keyframe(entry.x + period * repeat, entry.y));
                }
            }

            var curve = new AnimationCurve(keys.ToArray());
            for (var i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }

        static void Add(Dictionary<string, List<Keyframe>> tracks, string key, float time, float value)
        {
            if (!tracks.TryGetValue(key, out var list))
            {
                list = new List<Keyframe>();
                tracks[key] = list;
            }

            list.Add(new Keyframe(time, value));
        }

        static void AddConstant(Dictionary<string, List<Keyframe>> tracks, string key, float cycle, float value)
        {
            Add(tracks, key, 0f, value);
            Add(tracks, key, cycle, value);
        }

        sealed class Gait
        {
            public string ClipPath = string.Empty;
            /// 1 周期 (2 歩) の秒数
            public float Cycle;
            /// 1 周期に打つキーの数 (接地解を載せるカーブ用)。半周期で左右が揃うよう偶数にする
            public int Samples;
            /// 装飾パーツ用のキー数。こちらも偶数にする
            public int DecorSamples;
            /// 1 本の脚が接地している周期比
            public float Duty;
            /// 脚の最大振り角 (度)
            public float LegAngle;
            /// 遊脚中に足裏を持ち上げる量
            public float StepLift;
            /// 周期を通して体を既定の高さからどれだけ下げて構えるか (負で低く)。
            /// 足裏は地面に固定したままなので、下げた分だけ脚が体に隠れて短く見える
            public float BodyDrop;
            /// 遊脚の前半にどれだけ動きを寄せるか (0 で均等、5 以上は非単調になるので不可)
            public float SwingSnap;
            /// 足裏の持ち上げピークの位置。1 で遊脚の中央、小さいほど離地直後に寄る
            public float LiftPeak;
            /// 近い脚の振り中心を既定位置からずらす量 (+X で後ろ)
            public float FrontPawCenterShift;
            /// 遠い脚の振り中心を既定位置から後ろ (+X) へずらす量
            public float BackPawCenterShift;
            /// 遠い脚の付け根 (Root ローカル X) をこれより前に出さない
            public float BackPivotMinX;
            /// ローカル単位/秒での進行速度
            public float GroundSpeed;
            public Vector2[] BobTable = Array.Empty<Vector2>();
            public float LeanBase;
            public float LeanAmp;
            public float LeanPhase;
            public float TailBase;
            public float TailAmp;
            public float TailPhase;
            public int TailHarmonic;
            public float FrontHandBase;
            public float FrontHandAmp;
            public float BackHandBase;
            public float BackHandAmp;
            public float HandPhase;
            public float ClothBase;
            public float ClothAmp;
            public float ClothPhase;
            public float TorsoAmp;
            public float TorsoPhase;
            public float SquashIn;
            public float SquashOut;
            public float SquashPhase;
        }
    }
}
