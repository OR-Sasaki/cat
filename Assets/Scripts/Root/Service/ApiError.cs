namespace Root.Service
{
    /// API通信エラーの詳細情報
    public class ApiError
    {
        public ApiErrorType Type { get; }
        public long StatusCode { get; }
        public string Message { get; }
        public string ResponseBody { get; }
        public bool IsRetryable { get; }

        public ApiError(ApiErrorType type, long statusCode, string message, string responseBody = "")
        {
            Type = type;
            StatusCode = statusCode;
            Message = message;
            ResponseBody = responseBody;
            IsRetryable = type is ApiErrorType.NetworkError
                or ApiErrorType.Timeout
                or ApiErrorType.ServerError;
        }
    }
}
