#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.State;
using UnityEngine;
using VContainer;

namespace Root.Service
{
    public class UserPointService : IUserPointService
    {
        readonly UserPointState _state;
        readonly PlayerPrefsService _playerPrefsService;

        public event Action<int>? YarnBalanceChanged;

        [Inject]
        public UserPointService(UserPointState state, PlayerPrefsService playerPrefsService)
        {
            _state = state;
            _playerPrefsService = playerPrefsService;
            Load();
        }

        public int GetYarnBalance() => _state.YarnBalance;

        public PointOperationResult AddYarn(int amount)
        {
            if (amount <= 0)
            {
                return PointOperationResult.Fail(PointOperationErrorCode.InvalidArgument, _state.YarnBalance);
            }

            var currentBalance = _state.YarnBalance;
            if ((long)currentBalance + amount > int.MaxValue)
            {
                return PointOperationResult.Fail(PointOperationErrorCode.Overflow, currentBalance);
            }

            var newBalance = currentBalance + amount;
            _state.SetYarnBalance(newBalance);
            FireYarnBalanceChanged(newBalance);
            Save();
            return PointOperationResult.Ok(newBalance);
        }

        public PointOperationResult SpendYarn(int amount)
        {
            if (amount <= 0)
            {
                return PointOperationResult.Fail(PointOperationErrorCode.InvalidArgument, _state.YarnBalance);
            }

            var currentBalance = _state.YarnBalance;
            if (currentBalance < amount)
            {
                return PointOperationResult.Fail(PointOperationErrorCode.Insufficient, currentBalance);
            }

            var newBalance = currentBalance - amount;
            _state.SetYarnBalance(newBalance);
            FireYarnBalanceChanged(newBalance);
            Save();
            return PointOperationResult.Ok(newBalance);
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public UniTask SaveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Save();
            return UniTask.CompletedTask;
        }

        void Load()
        {
            UserPointSnapshot? snapshot;
            try
            {
                snapshot = _playerPrefsService.Load<UserPointSnapshot>(PlayerPrefsKey.UserPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserPointService] {e.Message}\n{e.StackTrace}");
                _state.SetYarnBalance(0);
                return;
            }

            if (snapshot is null || snapshot.Version != UserPointSnapshot.CurrentVersion)
            {
                _state.SetYarnBalance(0);
                return;
            }

            _state.SetYarnBalance(snapshot.YarnBalance);
        }

        void Save()
        {
            try
            {
                var snapshot = new UserPointSnapshot
                {
                    Version = UserPointSnapshot.CurrentVersion,
                    YarnBalance = _state.YarnBalance,
                };
                _playerPrefsService.Save(PlayerPrefsKey.UserPoint, snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserPointService] {e.Message}\n{e.StackTrace}");
            }
        }

        void FireYarnBalanceChanged(int newBalance)
        {
            var handlers = YarnBalanceChanged;
            if (handlers is null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<int>)handler)(newBalance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserPointService] {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
