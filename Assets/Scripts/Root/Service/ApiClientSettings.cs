namespace Root.Service
{
    /// APIクライアントの接続設定
    public static class ApiClientSettings
    {
#if PRODUCTION
        // TODO: 本番APIエンドポイントに更新する
        public const string BaseUrl = "https://api.example.com";
#else
        public const string BaseUrl = "http://localhost:8080";
#endif
        public const int TimeoutSeconds = 10;
    }
}
