#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Editor
{
    /// iOS ビルド時に App Privacy 関連の宣言を生成する
    /// - ATT 用の NSUserTrackingUsageDescription を Info.plist へ注入
    /// - アプリ自身の App Privacy (広告ID・トラッキング) を PrivacyInfo.xcprivacy へ注入
    /// 宣言内容は docs/store-privacy-declaration.md と対応させる
    public static class RewardedAdBuildPostProcessor
    {
        const string TrackingUsageDescription =
            "広告の表示と最適化のためにトラッキング情報の使用を許可してください。";

        /// Unity はエンジン自身の Required Reason API 宣言を同名ファイルで生成する。
        /// 既にあれば読み込んで追記し、無ければ新規作成してメインターゲットへ追加する
        const string PrivacyManifestName = "PrivacyInfo.xcprivacy";

        /// アプリ自身がトラッキング目的で通信するドメイン。
        /// 本アプリは広告 SDK 経由でしか通信しないため空のまま (SDK 側が自身のマニフェストで宣言する)
        static readonly string[] TrackingDomains = { };

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            InjectTrackingUsageDescription(pathToBuiltProject);
            InjectPrivacyManifest(pathToBuiltProject);
        }

        static void InjectTrackingUsageDescription(string pathToBuiltProject)
        {
            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", TrackingUsageDescription);
            plist.WriteToFile(plistPath);
        }

        static void InjectPrivacyManifest(string pathToBuiltProject)
        {
            var manifestPath = Path.Combine(pathToBuiltProject, PrivacyManifestName);

            var manifest = new PlistDocument();
            if (File.Exists(manifestPath))
            {
                // Unity が出力した NSPrivacyAccessedAPITypes を保持したまま追記する
                manifest.ReadFromFile(manifestPath);
            }
            else
            {
                manifest.Create();
            }

            // 広告 SDK (LevelPlay) が IDFA を用いるため、アプリとしては「トラッキングあり」
            manifest.root.SetBoolean("NSPrivacyTracking", true);

            var domains = manifest.root.CreateArray("NSPrivacyTrackingDomains");
            foreach (var domain in TrackingDomains)
            {
                domains.AddString(domain);
            }

            // CreateArray は既存の値を置き換えるため、再ビルドしても重複しない
            var dataTypes = manifest.root.CreateArray("NSPrivacyCollectedDataTypes");
            AddAdvertisingDataType(dataTypes, "NSPrivacyCollectedDataTypeDeviceID");
            AddAdvertisingDataType(dataTypes, "NSPrivacyCollectedDataTypeAdvertisingData");

            manifest.WriteToFile(manifestPath);

            AddManifestToXcodeProject(pathToBuiltProject);
        }

        /// 第三者広告目的・トラッキングありで収集するデータ種別を 1 件追加する
        static void AddAdvertisingDataType(PlistElementArray dataTypes, string dataType)
        {
            var entry = dataTypes.AddDict();
            entry.SetString("NSPrivacyCollectedDataType", dataType);
            // 広告 ID はアカウントと結び付けていない (本アプリはログイン機構を持たない)
            entry.SetBoolean("NSPrivacyCollectedDataTypeLinked", false);
            entry.SetBoolean("NSPrivacyCollectedDataTypeTracking", true);

            var purposes = entry.CreateArray("NSPrivacyCollectedDataTypePurposes");
            purposes.AddString("NSPrivacyCollectedDataTypePurposeThirdPartyAdvertising");
        }

        static void AddManifestToXcodeProject(string pathToBuiltProject)
        {
            var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            if (project.FindFileGuidByProjectPath(PrivacyManifestName) != null)
            {
                Debug.Log($"[RewardedAdBuildPostProcessor] {PrivacyManifestName} は既に Xcode プロジェクトへ登録済み。内容のみ更新した。");
                return;
            }

            var targetGuid = project.GetUnityMainTargetGuid();
            var fileGuid = project.AddFile(PrivacyManifestName, PrivacyManifestName);
            project.AddFileToBuildSection(targetGuid, project.GetResourcesBuildPhaseByTarget(targetGuid), fileGuid);
            project.WriteToFile(projectPath);

            Debug.Log($"[RewardedAdBuildPostProcessor] {PrivacyManifestName} をメインターゲットへ追加した。");
        }
    }
}
#endif
