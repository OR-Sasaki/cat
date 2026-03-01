using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// APIクライアントのインターフェース
    public interface IApiClient
    {
        void SetBearerToken(string token);
        void ClearBearerToken();

        UniTask<ApiResult<TResponse>> GetAsync<TResponse>(
            string path, CancellationToken cancellationToken);

        UniTask<ApiResult<TResponse>> PostAsync<TResponse>(
            string path, object body, CancellationToken cancellationToken);

        UniTask<ApiResult<TResponse>> PutAsync<TResponse>(
            string path, object body, CancellationToken cancellationToken);

        UniTask<ApiResult<TResponse>> DeleteAsync<TResponse>(
            string path, CancellationToken cancellationToken);
    }
}
