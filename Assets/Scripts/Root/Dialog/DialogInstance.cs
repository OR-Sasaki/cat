using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Root.Dialog
{
    public class DialogInstance
    {
        public string DialogId { get; }
        public MonoBehaviour View { get; }
        public UniTaskCompletionSource<DialogResult> CompletionSource { get; }
        public int SortingOrder { get; }

        public DialogInstance(string dialogId, MonoBehaviour view, int sortingOrder)
        {
            DialogId = dialogId;
            View = view;
            SortingOrder = sortingOrder;
            CompletionSource = new UniTaskCompletionSource<DialogResult>();
        }
    }
}
