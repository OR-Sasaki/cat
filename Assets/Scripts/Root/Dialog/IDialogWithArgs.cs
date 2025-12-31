namespace Root.Dialog
{
    public interface IDialogWithArgs<in TArgs> where TArgs : IDialogArgs
    {
        void Initialize(TArgs args);
    }
}
