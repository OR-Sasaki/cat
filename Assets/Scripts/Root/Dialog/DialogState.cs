#nullable enable

using System.Collections.Generic;

namespace Root.Dialog
{
    public class DialogState
    {
        const int BaseSortingOrder = 1000;
        const int SortingOrderIncrement = 10;

        readonly Stack<DialogInstance> _dialogStack = new();

        public int Count => _dialogStack.Count;
        public bool HasDialog => _dialogStack.Count > 0;

        public DialogInstance? Current
        {
            get
            {
                if (_dialogStack.Count == 0)
                {
                    return null;
                }
                return _dialogStack.Peek();
            }
        }

        public int GetNextSortingOrder()
        {
            return BaseSortingOrder + (_dialogStack.Count * SortingOrderIncrement);
        }

        public void Push(DialogInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }
            _dialogStack.Push(instance);
        }

        public DialogInstance? Pop()
        {
            if (_dialogStack.Count == 0)
            {
                return null;
            }
            return _dialogStack.Pop();
        }

        public IReadOnlyList<DialogInstance> PopUntil(DialogInstance target)
        {
            if (target == null)
            {
                throw new System.ArgumentNullException(nameof(target));
            }

            var poppedInstances = new List<DialogInstance>();

            while (_dialogStack.Count > 0)
            {
                var instance = _dialogStack.Pop();
                poppedInstances.Add(instance);

                if (instance == target)
                {
                    break;
                }
            }

            return poppedInstances;
        }
    }
}
