#nullable enable

using System;

namespace Root.Service
{
    /// 毛糸残高の永続化スナップショット
    /// Version != CurrentVersion の場合は破棄して残高 0 で初期化する
    [Serializable]
    public class UserPointSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public int YarnBalance;
    }
}
