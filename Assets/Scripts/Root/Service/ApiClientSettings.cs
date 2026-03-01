namespace Root.Service
{
    /// APIクライアントの接続設定
    public class ApiClientSettings
    {
#if PRODUCTION
        public const string BaseUrl = "https://api.example.com";
#else
        public const string BaseUrl = "http://localhost:8080";
#endif
        public const int TimeoutSeconds = 10;
    }
}
