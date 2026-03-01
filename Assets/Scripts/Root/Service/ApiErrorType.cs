namespace Root.Service
{
    /// API通信エラーの種別
    public enum ApiErrorType
    {
        NetworkError,
        Timeout,
        AuthenticationError,
        ClientError,
        ServerError,
        ParseError,
    }
}
