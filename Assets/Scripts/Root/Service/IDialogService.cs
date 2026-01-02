#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Root.View;

namespace Root.Service
{
    public interface IDialogService
    {
        UniTask<DialogResult> OpenAsync<TDialog>(CancellationToken cancellationToken)
            where TDialog : BaseDialogView;

        UniTask<DialogResult> OpenAsync<TDialog, TArgs>(TArgs args, CancellationToken cancellationToken)
            where TDialog : BaseDialogView, IDialogWithArgs<TArgs>
            where TArgs : IDialogArgs;

        void Close(DialogResult result, bool closeParent = false);

        bool HasOpenDialog { get; }
    }
}
