#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Root.Service;
using Root.State;
using Root.View;

namespace Tests.Editor
{
    public class ApiErrorHandlerTest
    {
        MockDialogService _dialogService = null!;
        TestableApiErrorHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _dialogService = new MockDialogService();
            var sceneLoader = new SceneLoader(new SceneLoaderState());
            _handler = new TestableApiErrorHandler(_dialogService, sceneLoader);
        }

        [Test(Description = "成功時に結果をそのまま返却する")]
        public void Success_ReturnsData()
        {
            var task = _handler.ExecuteWithErrorHandling(
                _ => UniTask.FromResult(ApiResult<string>.Success("data")),
                CancellationToken.None);

            Assert.AreEqual("data", task.GetAwaiter().GetResult());
        }

        [Test(Description = "リトライ可能エラー時にリトライ選択でデリゲートを再実行する")]
        public void RetryableError_RetrySelected_RetriesAndReturnsData()
        {
            var callCount = 0;
            _dialogService.NextResult = DialogResult.Ok;

            var task = _handler.ExecuteWithErrorHandling<string>(
                _ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return UniTask.FromResult(ApiResult<string>.Failure(
                            new ApiError(ApiErrorType.NetworkError, 0, "error")));
                    }
                    return UniTask.FromResult(ApiResult<string>.Success("ok"));
                },
                CancellationToken.None);

            Assert.AreEqual("ok", task.GetAwaiter().GetResult());
            Assert.AreEqual(2, callCount);
        }

        [Test(Description = "リトライ可能エラー時にタイトル選択でタイトル画面に遷移する")]
        public void RetryableError_TitleSelected_NavigatesToTitleAndThrows()
        {
            _dialogService.NextResult = DialogResult.Cancel;

            Assert.Throws<OperationCanceledException>(() =>
            {
                _handler.ExecuteWithErrorHandling(
                    _ => UniTask.FromResult(ApiResult<string>.Failure(
                        new ApiError(ApiErrorType.ServerError, 500, "error"))),
                    CancellationToken.None).GetAwaiter().GetResult();
            });

            Assert.IsTrue(_handler.NavigateToTitleCalled);
        }

        [Test(Description = "リトライ不可エラー時にメッセージダイアログ表示後タイトル画面に遷移する")]
        public void NonRetryableError_NavigatesToTitleAndThrows()
        {
            _dialogService.NextResult = DialogResult.Ok;

            Assert.Throws<OperationCanceledException>(() =>
            {
                _handler.ExecuteWithErrorHandling(
                    _ => UniTask.FromResult(ApiResult<string>.Failure(
                        new ApiError(ApiErrorType.ClientError, 400, "bad request"))),
                    CancellationToken.None).GetAwaiter().GetResult();
            });

            Assert.IsTrue(_handler.NavigateToTitleCalled);
        }

        class TestableApiErrorHandler : ApiErrorHandler
        {
            public bool NavigateToTitleCalled { get; private set; }

            public TestableApiErrorHandler(IDialogService dialogService, SceneLoader sceneLoader)
                : base(dialogService, sceneLoader) { }

            protected override void NavigateToTitle()
            {
                NavigateToTitleCalled = true;
            }
        }

        class MockDialogService : IDialogService
        {
            public DialogResult NextResult { get; set; } = DialogResult.Ok;

            public bool HasOpenDialog => false;

            public UniTask<DialogResult> OpenAsync<TDialog>(CancellationToken cancellationToken)
                where TDialog : BaseDialogView
            {
                return UniTask.FromResult(NextResult);
            }

            public UniTask<DialogResult> OpenAsync<TDialog, TArgs>(TArgs args, CancellationToken cancellationToken)
                where TDialog : BaseDialogView, IDialogWithArgs<TArgs>
                where TArgs : IDialogArgs
            {
                return UniTask.FromResult(NextResult);
            }

            public void Close(DialogResult result, bool closeParent = false) { }
        }
    }
}
