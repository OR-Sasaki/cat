#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.View;
using VContainer;

namespace Root.Service
{
    /// API通信エラーに対する共通UI処理とリトライ制御
    public class ApiErrorHandler : IApiErrorHandler
    {
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;

        [Inject]
        public ApiErrorHandler(IDialogService dialogService, SceneLoader sceneLoader)
        {
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
        }

        public async UniTask<T> ExecuteWithErrorHandling<T>(
            Func<CancellationToken, UniTask<ApiResult<T>>> apiCall,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await apiCall(cancellationToken);

                if (result.IsSuccess)
                {
                    return result.Data!;
                }

                var error = result.Error!;

                if (error.IsRetryable)
                {
                    var dialogArgs = new CommonConfirmDialogArgs(
                        "通信エラー",
                        error.Message,
                        "リトライ",
                        "タイトルに戻る");

                    var dialogResult = await _dialogService
                        .OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                            dialogArgs, cancellationToken);

                    if (dialogResult == DialogResult.Ok)
                    {
                        continue;
                    }

                    NavigateToTitle();
                    throw new OperationCanceledException();
                }

                var messageArgs = new CommonMessageDialogArgs(
                    "エラー",
                    error.Message);

                await _dialogService
                    .OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                        messageArgs, cancellationToken);

                NavigateToTitle();
                throw new OperationCanceledException();
            }
        }

        protected virtual void NavigateToTitle()
        {
            _sceneLoader.Load(Const.SceneName.Title);
        }
    }
}
