using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

namespace Root.Service
{
    /// ゲームサーバとのHTTP通信を行うAPIクライアント
    public class ApiClient : IApiClient
    {
        string _bearerToken;

        [Inject]
        public ApiClient()
        {
        }

        public void SetBearerToken(string token)
        {
            _bearerToken = token;
        }

        public void ClearBearerToken()
        {
            _bearerToken = null;
        }

        public UniTask<ApiResult<TResponse>> GetAsync<TResponse>(
            string path, CancellationToken cancellationToken)
        {
            return SendAsync<TResponse>("GET", path, null, cancellationToken);
        }

        public UniTask<ApiResult<TResponse>> PostAsync<TResponse>(
            string path, object body, CancellationToken cancellationToken)
        {
            return SendAsync<TResponse>("POST", path, body, cancellationToken);
        }

        public UniTask<ApiResult<TResponse>> PutAsync<TResponse>(
            string path, object body, CancellationToken cancellationToken)
        {
            return SendAsync<TResponse>("PUT", path, body, cancellationToken);
        }

        public UniTask<ApiResult<TResponse>> DeleteAsync<TResponse>(
            string path, CancellationToken cancellationToken)
        {
            return SendAsync<TResponse>("DELETE", path, null, cancellationToken);
        }

        async UniTask<ApiResult<TResponse>> SendAsync<TResponse>(
            string method, string path, object body, CancellationToken cancellationToken)
        {
            var url = ApiClientSettings.BaseUrl + path;

            using var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (!string.IsNullOrEmpty(_bearerToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _bearerToken);
            }

#if !PRODUCTION
            Debug.Log($"[ApiClient] Request: {method} {path} {(body != null ? JsonConvert.SerializeObject(body) : "")}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

            try
            {
                await request.SendWebRequest()
                    .WithCancellation(cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(ApiClientSettings.TimeoutSeconds));
            }
            catch (TimeoutException)
            {
                request.Abort();
                var error = new ApiError(ApiErrorType.Timeout, 0, "Request timed out");
                Debug.LogError($"[ApiClient] {error.Message}");
                return ApiResult<TResponse>.Failure(error);
            }
            catch (UnityWebRequestException e)
            {
#if !PRODUCTION
                stopwatch.Stop();
                Debug.Log($"[ApiClient] Response: {path} status={e.ResponseCode} time={stopwatch.ElapsedMilliseconds}ms body={e.Text}");
#endif
                return ApiResult<TResponse>.Failure(ClassifyError(e));
            }

#if !PRODUCTION
            stopwatch.Stop();
            Debug.Log($"[ApiClient] Response: {path} status={request.responseCode} time={stopwatch.ElapsedMilliseconds}ms body={request.downloadHandler.text}");
#endif

            try
            {
                var data = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                return ApiResult<TResponse>.Success(data);
            }
            catch (JsonException e)
            {
                var error = new ApiError(ApiErrorType.ParseError, request.responseCode, e.Message, request.downloadHandler.text);
                Debug.LogError($"[ApiClient] {error.Message}");
                return ApiResult<TResponse>.Failure(error);
            }
        }

        static ApiError ClassifyError(UnityWebRequestException e)
        {
            ApiErrorType type;
            if (e.Result == UnityWebRequest.Result.ConnectionError)
            {
                type = ApiErrorType.NetworkError;
            }
            else if (e.ResponseCode == 401)
            {
                type = ApiErrorType.AuthenticationError;
            }
            else if (e.ResponseCode >= 400 && e.ResponseCode < 500)
            {
                type = ApiErrorType.ClientError;
            }
            else if (e.ResponseCode >= 500)
            {
                type = ApiErrorType.ServerError;
            }
            else
            {
                type = ApiErrorType.NetworkError;
            }

            var error = new ApiError(type, e.ResponseCode, e.Message, e.Text ?? "");
            Debug.LogError($"[ApiClient] {error.Message}");
            return error;
        }
    }
}
