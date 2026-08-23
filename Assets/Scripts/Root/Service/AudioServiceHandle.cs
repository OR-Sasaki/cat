#nullable enable

namespace Root.Service
{
    /// DI 経路外 (動的生成される MonoBehaviour 等) から IAudioService へ到達するための静的ブリッジ
    /// プロジェクトで唯一の静的サービスアクセスであり、他用途への流用は禁止
    public static class AudioServiceHandle
    {
        public static IAudioService? Current { get; private set; }

        public static void SetCurrent(IAudioService? service)
        {
            Current = service;
        }
    }
}
