using System.Threading.Tasks;
using Aesir.Client.Controls;
using Aesir.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Aesir.Client.Services.Implementations.Standard
{
    /// <summary>
    /// Represents a service for displaying dialogs to the user, such as input, confirmation, and error dialogs.
    /// </summary>
    /// <remarks>
    /// This class provides asynchronous methods for showing various types of dialogs, offering customization through options such as title, message, and dialog buttons.
    /// </remarks>
    public class DialogService : IDialogService
    {
        /// Displays an input dialog asynchronously to collect user input with specified parameters.
        /// <param name="title">The title displayed on the input dialog.</param>
        /// <param name="inputValue">The default value displayed in the input field of the dialog.</param>
        /// <param name="label">The label text shown above the input field. Defaults to "Value".</param>
        /// <param name="defaultValue">The value to return if the user cancels the dialog. Defaults to an empty string.</param>
        /// <returns>
        /// A string containing the user input if the dialog is confirmed; otherwise, the specified default value if the dialog is canceled.
        /// </returns>
        public async Task<string> ShowInputDialogAsync(string title, string inputValue, string label = "Value",
            string defaultValue = "")
        {
            var options = new OverlayDialogOptions()
            {
                Mode = DialogMode.Info,
                Buttons = DialogButton.OKCancel,
                Title = title,
                CanLightDismiss = true,
                CanDragMove = true,
                IsCloseButtonVisible = true,
                CanResize = false
            };
            var vm = new InputDialogViewModel
            {
                InputText = inputValue,
                IsInputTextRequired = true,
                LabelText = label
            };
            var result = await OverlayDialog.ShowModal<InputDialog, InputDialogViewModel>(
                vm, options: options);

            return result == DialogResult.OK ? vm.InputText : defaultValue;
        }

        /// Asynchronously displays a confirmation dialog with the specified title and message.
        /// Offers options for the user to confirm or decline the action.
        /// <param name="title">The title of the confirmation dialog.</param>
        /// <param name="message">The message or question to display within the dialog.</param>
        /// <returns>A task that resolves to a boolean value indicating whether the user confirmed (true) or declined (false) the action.</returns>
        public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
        {
            var options = new OverlayDialogOptions()
            {
                Mode = DialogMode.Info,
                Buttons = DialogButton.YesNo,
                Title = title,
                CanLightDismiss = true,
                CanDragMove = true,
                IsCloseButtonVisible = true,
                CanResize = false
            };

            var vm = new ConfirmDialogViewModel
            {
                MessageText = message
            };
            var result = await OverlayDialog.ShowModal<ConfirmDialog, ConfirmDialogViewModel>(
                vm, options: options);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Displays an error notification in a dialog.
        /// </summary>
        /// <param name="title">The title of the error dialog.</param>
        /// <param name="message">The message to display within the error dialog.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task ShowErrorDialogAsync(string title, string message)
        {
            var topLevel = TopLevel.GetTopLevel(GetMainView());
            var manager = new WindowNotificationManager(topLevel)
            {
                MaxItems = 3
            };

            var notification = new Notification(
                title,
                message
            );

            manager.Show(
                content: notification,
                showClose: true,
                showIcon: true,
                type: NotificationType.Error,
                classes: ["Light"]
            );

            await Task.CompletedTask;
        }

        /// Retrieves the main view of the application, identifying it based on the application's lifetime (desktop or single view).
        /// This method handles different application lifetimes, such as IClassicDesktopStyleApplicationLifetime and ISingleViewApplicationLifetime, to determine the appropriate main view control.
        /// <returns>
        /// A ContentControl representing the main view of the application, or null if no applicable lifetime is found.
        /// </returns>
        private ContentControl? GetMainView()
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    return desktop.MainWindow;
                case ISingleViewApplicationLifetime singleView:
                    return singleView.MainView as ContentControl;
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}