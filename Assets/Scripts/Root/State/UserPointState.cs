#nullable enable

namespace Root.State
{
    public class UserPointState
    {
        public int YarnBalance { get; private set; }

        internal void SetYarnBalance(int balance)
        {
            YarnBalance = balance;
        }
    }
}
