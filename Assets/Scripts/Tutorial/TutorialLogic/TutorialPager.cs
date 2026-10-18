#nullable enable

using System;

namespace Tutorial.TutorialLogic
{
    public sealed class TutorialPager
    {
        public int Index { get; private set; }
        public int PageCount { get; }
        public bool HasReachedLast { get; private set; }
        public bool ShowsNavigation => PageCount > 1;

        public TutorialPager(int pageCount)
        {
            if (pageCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount));
            }

            PageCount = pageCount;
            HasReachedLast = pageCount == 1;
        }

        public bool Next() => JumpTo(Index + 1);

        public bool Previous() => JumpTo(Index - 1);

        public bool JumpTo(int index)
        {
            if (index < 0 || index >= PageCount || index == Index)
            {
                return false;
            }

            Index = index;
            if (Index == PageCount - 1)
            {
                HasReachedLast = true;
            }

            return true;
        }
    }
}
