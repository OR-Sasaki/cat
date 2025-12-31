using Cysharp.Threading.Tasks;
using UnityEngine;

using Root.View;

namespace Root.State
{
    public class DialogInstance
    {
        public string DialogId { get; }
        public MonoBehaviour View { get; }
        public UniTaskCompletionSource<DialogResult> CompletionSource { get; }
        public int SortingOrder { get; }

        public DialogInstance(string dialogId, MonoBehaviour view, int sortingOrder)
        {
            if (string.IsNullOrEmpty(dialogId))
            {
                throw new System.ArgumentException("Dialog ID cannot be null or empty.", nameof(dialogId));
            }
            if (view == null)
            {
                throw new System.ArgumentNullException(nameof(view));
            }

            DialogId = dialogId;
            View = view;
            SortingOrder = sortingOrder;
            CompletionSource = new UniTaskCompletionSource<DialogResult>();
        }
    }
}
