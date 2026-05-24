#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// API通信エラーのハンドリングサービスインターフェース
    public interface IApiErrorHandler
    {
        UniTask<T> ExecuteWithErrorHandling<T>(
            Func<CancellationToken, UniTask<ApiResult<T>>> apiCall,
            CancellationToken cancellationToken);
    }
}
