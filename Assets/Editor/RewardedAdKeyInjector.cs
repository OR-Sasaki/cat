using System;
using System.Collections.Generic;
using System.IO;
using Root.Service;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
    /// ビルド時に LevelPlay の App Key / Ad Unit ID を RewardedAdConfig へ注入する。
    /// 解決順: 環境変数（CI 向け）→ プロジェクト直下 .env（ローカル向け・gitignored）。
    /// コミット済みアセットは空のまま維持し、ビルド後は空へ戻すため
    /// public リポジトリにも作業ツリーにも鍵を残さない。
    ///
    /// キー名（環境変数 / .env 共通）:
    ///   LEVELPLAY_ANDROID_APP_KEY / LEVELPLAY_ANDROID_AD_UNIT_ID
    ///   LEVELPLAY_IOS_APP_KEY     / LEVELPLAY_IOS_AD_UNIT_ID
    public sealed class RewardedAdKeyInjector : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string ConfigResourcePath = "RewardedAdConfig";
        const string AndroidAppKeyName = "LEVELPLAY_ANDROID_APP_KEY";
        const string AndroidAdUnitIdName = "LEVELPLAY_ANDROID_AD_UNIT_ID";
        const string IosAppKeyName = "LEVELPLAY_IOS_APP_KEY";
        const string IosAdUnitIdName = "LEVELPLAY_IOS_AD_UNIT_ID";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            var envFile = LoadDotEnv();

            var androidAppKey = string.Empty;
            var androidAdUnitId = string.Empty;
            var iosAppKey = string.Empty;
            var iosAdUnitId = string.Empty;

            if (target == BuildTarget.Android)
            {
                androidAppKey = Resolve(envFile, AndroidAppKeyName);
                androidAdUnitId = Resolve(envFile, AndroidAdUnitIdName);
                WarnIfMissing(target, androidAppKey, androidAdUnitId);
            }
            else if (target == BuildTarget.iOS)
            {
                iosAppKey = Resolve(envFile, IosAppKeyName);
                iosAdUnitId = Resolve(envFile, IosAdUnitIdName);
                WarnIfMissing(target, iosAppKey, iosAdUnitId);
            }

            WriteKeys(androidAppKey, androidAdUnitId, iosAppKey, iosAdUnitId);

            if (target is BuildTarget.Android or BuildTarget.iOS)
            {
                Debug.Log($"[RewardedAdKeyInjector] Injected LevelPlay keys for {target}.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // 注入した鍵を空へ戻し、作業ツリー・リポジトリに残さない
            WriteKeys(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        [MenuItem("Tools/Rewarded Ad/Clear Injected Keys")]
        static void ClearInjectedKeys()
        {
            WriteKeys(string.Empty, string.Empty, string.Empty, string.Empty);
            Debug.Log("[RewardedAdKeyInjector] Cleared LevelPlay keys from RewardedAdConfig.");
        }

        // 環境変数を優先し、無ければローカル .env を引く
        static string Resolve(IReadOnlyDictionary<string, string> envFile, string name)
        {
            var fromEnv = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;

            return envFile.TryGetValue(name, out var value) ? value : string.Empty;
        }

        // <project root>/.env を読み KEY=VALUE を辞書化する（無ければ空辞書）
        static IReadOnlyDictionary<string, string> LoadDotEnv()
        {
            var result = new Dictionary<string, string>();
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".env"));
            if (!File.Exists(path)) return result;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var separator = line.IndexOf('=');
                if (separator <= 0) continue;

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                result[key] = value;
            }

            return result;
        }

        static void WriteKeys(string androidAppKey, string androidAdUnitId, string iosAppKey, string iosAdUnitId)
        {
            var config = Resources.Load<RewardedAdConfig>(ConfigResourcePath);
            if (config == null)
            {
                Debug.LogWarning($"[RewardedAdKeyInjector] Resources/{ConfigResourcePath} が見つかりません。");
                return;
            }

            var so = new SerializedObject(config);
            SetString(so, "_androidAppKey", androidAppKey);
            SetString(so, "_androidRewardedAdUnitId", androidAdUnitId);
            SetString(so, "_iosAppKey", iosAppKey);
            SetString(so, "_iosRewardedAdUnitId", iosAdUnitId);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        static void SetString(SerializedObject so, string propertyName, string value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[RewardedAdKeyInjector] property '{propertyName}' が見つかりません。");
                return;
            }

            prop.stringValue = value;
        }

        static void WarnIfMissing(BuildTarget target, string appKey, string adUnitId)
        {
            if (!string.IsNullOrEmpty(appKey) && !string.IsNullOrEmpty(adUnitId)) return;

            Debug.LogWarning($"[RewardedAdKeyInjector] {target} 向けの LevelPlay 鍵が環境変数にも .env にも未設定です。" +
                             "鍵が空のままビルドされ、広告はロードされません。");
        }
    }
}
