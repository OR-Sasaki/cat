namespace Root.Dialog
{
    public interface IDialogCloser
    {
        void Close(DialogViewBase dialog, DialogResult result, int closeCount = 1);
    }
}
