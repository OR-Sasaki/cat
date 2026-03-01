namespace Root.Service
{
    /// API通信結果を表すジェネリック型
    public class ApiResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public ApiError Error { get; }

        ApiResult(bool isSuccess, T data, ApiError error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        public static ApiResult<T> Success(T data)
        {
            return new ApiResult<T>(true, data, null);
        }

        public static ApiResult<T> Failure(ApiError error)
        {
            return new ApiResult<T>(false, default, error);
        }
    }
}
