#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Root.Service
{
    /// 毛糸 (ポイント系通貨) の残高を読み書き・通知・永続化するサービス契約
    public interface IUserPointService
    {
        /// 現在の毛糸残高を取得する
        int GetYarnBalance();

        /// 毛糸を加算する (amount > 0、int.MaxValue 超過時は Overflow)
        PointOperationResult AddYarn(int amount);

        /// 毛糸を減算する (amount > 0、残高不足時は Insufficient)
        PointOperationResult SpendYarn(int amount);

        /// 残高変更通知 (変更後の残高)
        event Action<int> YarnBalanceChanged;

        /// 初期化 (現状はコンストラクタで Load 済みのためノーオペ)
        UniTask InitializeAsync(CancellationToken cancellationToken);

        /// 明示的な保存要求 (通常は書き込み操作時に自動保存される)
        UniTask SaveAsync(CancellationToken cancellationToken);
    }

    public enum PointOperationErrorCode
    {
        InvalidArgument,
        Insufficient,
        Overflow,
    }

    public readonly struct PointOperationResult
    {
        public bool IsSuccess { get; }
        public PointOperationErrorCode? Error { get; }
        public int Balance { get; }

        PointOperationResult(bool isSuccess, PointOperationErrorCode? error, int balance)
        {
            IsSuccess = isSuccess;
            Error = error;
            Balance = balance;
        }

        public static PointOperationResult Ok(int balance) => new(true, null, balance);

        public static PointOperationResult Fail(PointOperationErrorCode code, int balance) => new(false, code, balance);
    }
}
