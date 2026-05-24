#nullable enable

using NUnit.Framework;
using Root.Service;

namespace Tests.Editor
{
    public class ApiResultTest
    {
        [Test(Description = "Successファクトリメソッドで成功結果を構築できる")]
        public void Success_SetsIsSuccessTrue()
        {
            var result = ApiResult<string>.Success("data");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("data", result.Data);
            Assert.IsNull(result.Error);
        }

        [Test(Description = "Failureファクトリメソッドでエラー結果を構築できる")]
        public void Failure_SetsIsSuccessFalse()
        {
            var error = new ApiError(ApiErrorType.NetworkError, 0, "error");

            var result = ApiResult<string>.Failure(error);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Data);
            Assert.AreEqual(error, result.Error);
        }

        [TestCase(ApiErrorType.NetworkError, true, TestName = "NetworkError はリトライ可能")]
        [TestCase(ApiErrorType.Timeout, true, TestName = "Timeout はリトライ可能")]
        [TestCase(ApiErrorType.ServerError, true, TestName = "ServerError はリトライ可能")]
        [TestCase(ApiErrorType.AuthenticationError, false, TestName = "AuthenticationError はリトライ不可")]
        [TestCase(ApiErrorType.ClientError, false, TestName = "ClientError はリトライ不可")]
        [TestCase(ApiErrorType.ParseError, false, TestName = "ParseError はリトライ不可")]
        public void ApiError_IsRetryable_ReturnsCorrectValue(ApiErrorType type, bool expected)
        {
            var error = new ApiError(type, 0, "test");

            Assert.AreEqual(expected, error.IsRetryable);
        }
    }
}
