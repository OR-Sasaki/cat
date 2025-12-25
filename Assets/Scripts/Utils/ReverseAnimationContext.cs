namespace Utils
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;

    /// <summary>
    /// アニメーションクリップを逆再生するクリップを生成するエディタ拡張
    /// Assetsメニューから「Create Reversed Clip」で実行可能
    /// </summary>
    public static class ReverseAnimationContext
    {
        /// <summary>
        /// 選択中のAnimationClipから逆再生版のクリップを生成する
        /// 元のファイル名に「_Reversed」を付けた新規ファイルとして保存される　
        /// </summary>
        [MenuItem("Assets/Create Reversed Clip", false, 14)]
        static void ReverseClip()
        {
            // 選択中アセットのパス情報を取得
            string directoryPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject));
            string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject));
            string fileExtension = Path.GetExtension(AssetDatabase.GetAssetPath(Selection.activeObject));
            fileName = fileName.Split('.')[0];

            // 逆再生クリップの保存先パスを生成（例: Walk.anim → Walk_Reversed.anim）
            string copiedFilePath =
                directoryPath + Path.DirectorySeparatorChar + fileName + "_Reversed" + fileExtension;
            var clip = GetSelectedClip();

            // 元のクリップをコピーして新規ファイルを作成
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(Selection.activeObject), copiedFilePath);

            // コピーしたクリップを読み込み
            clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(copiedFilePath, typeof(AnimationClip));

            if (clip == null)
                return;

            float clipLength = clip.length;

            // 全てのアニメーションカーブを取得（子オブジェクトのカーブも含む）
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
            var curves = AnimationUtility.GetAllCurves(clip, true);
#pragma warning restore CS0618 // 型またはメンバーが旧型式です
            clip.ClearCurves();

            foreach (AnimationClipCurveData curve in curves)
            {
                var keys = curve.curve.keys;
                int keyCount = keys.Length;

                // ラップモード（ループ設定）を入れ替え
                (curve.curve.postWrapMode, curve.curve.preWrapMode) = (curve.curve.preWrapMode, curve.curve.postWrapMode);

                // 各キーフレームの時間とタンジェントを反転
                for (int i = 0; i < keyCount; i++)
                {
                    Keyframe K = keys[i];

                    // 時間を反転（例: 0.5秒 → clipLength - 0.5秒）
                    K.time = clipLength - K.time;

                    // タンジェント（傾き）を入れ替えて反転
                    var tmp = -K.inTangent;
                    K.inTangent = -K.outTangent;
                    K.outTangent = tmp;

                    keys[i] = K;
                }

                // 反転したカーブをクリップに設定
                curve.curve.keys = keys;
                clip.SetCurve(curve.path, curve.type, curve.propertyName, curve.curve);
            }

            // アニメーションイベントも時間を反転
            var events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Length > 0)
            {
                for (int i = 0; i < events.Length; i++)
                {
                    events[i].time = clipLength - events[i].time;
                }

                AnimationUtility.SetAnimationEvents(clip, events);
            }

            Debug.Log("Animation reversed!");
        }

        /// <summary>
        /// メニュー項目の有効/無効を判定するバリデーション
        /// AnimationClipが選択されている場合のみ有効
        /// </summary>
        [MenuItem("Assets/Create Reversed Clip", true)]
        static bool ReverseClipValidation()
        {
            return Selection.activeObject.GetType() == typeof(AnimationClip);
        }

        /// <summary>
        /// 現在選択中のAnimationClipを取得する
        /// </summary>
        /// <returns>選択中のAnimationClip、なければnull</returns>
        public static AnimationClip GetSelectedClip()
        {
            var clips = Selection.GetFiltered(typeof(AnimationClip), SelectionMode.Assets);
            if (clips.Length > 0)
            {
                return clips[0] as AnimationClip;
            }

            return null;
        }
    }
}
