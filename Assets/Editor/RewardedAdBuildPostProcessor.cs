#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Editor
{
    /// iOS ビルド時に ATT 用の NSUserTrackingUsageDescription を Info.plist へ注入する
    public static class RewardedAdBuildPostProcessor
    {
        const string TrackingUsageDescription =
            "広告の表示と最適化のためにトラッキング情報の使用を許可してください。";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", TrackingUsageDescription);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
