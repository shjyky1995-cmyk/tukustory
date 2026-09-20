namespace Tuku.Desktop.ViewModels
{
    using System.Windows;

    public interface IDialogService
    {
        bool Confirm(string message, string title);

        void Info(string message, string title);

        MessageBoxResult ConfirmSaveDiscardCancel(string message, string title);
    }

    public sealed class MessageBoxDialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public void Info(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public MessageBoxResult ConfirmSaveDiscardCancel(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        }
    }
}
