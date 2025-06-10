using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Controls;
using Aesir.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Ursa.Controls;
using Notification = Avalonia.Controls.Notifications.Notification;
using WindowNotificationManager = Avalonia.Controls.Notifications.WindowNotificationManager;

namespace Aesir.Client.Services.Implementations.Standard
{
    public class DialogService : IDialogService
    {
        public async Task<string> ShowInputDialogAsync(string title, string inputValue, string label = "Value", string defaultValue = "")
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

        public async Task ShowErrorDialogAsync(string title, string message)
        {
            var topLevel = TopLevel.GetTopLevel(GetMainView());
            var manager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
            manager.Show(new Notification(title, message, NotificationType.Error));
            
            await Task.CompletedTask;
        }
        
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
