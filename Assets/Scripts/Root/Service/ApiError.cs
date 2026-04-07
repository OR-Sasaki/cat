#nullable enable

namespace Root.Service
{
    /// API通信エラーの詳細情報
    public record ApiError(
        ApiErrorType Type,
        long StatusCode,
        string Message,
        string ResponseBody = "")
    {
        public bool IsRetryable => Type is ApiErrorType.NetworkError
            or ApiErrorType.Timeout
            or ApiErrorType.ServerError;
    }
}
