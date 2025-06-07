using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace Aesir.Client.Services.Implementations
{
    public class DialogService : IDialogService
    {
        public async Task<string> ShowInputDialogAsync(string title, string message, string defaultValue = "")
        {
            var window = GetMainView();
            if (window == null) return string.Empty;
            
            var parameters = new MessageBoxCustomParams
            {
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new() { Name = "Ok", IsDefault = true },
                    new() { Name = "Cancel", IsCancel = true }
                },
                ContentTitle = title,
                ContentMessage = message,
                Icon = Icon.None,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                MaxWidth = 400,
                MaxHeight = 300,
                SizeToContent = SizeToContent.WidthAndHeight,
                InputParams = new InputParams()
                {
                    DefaultValue =  defaultValue
                }
            };

            var msgBox = MessageBoxManager.GetMessageBoxCustom(parameters);

            var result = await msgBox.ShowAsPopupAsync(window);
            
            return result == "Ok" ? msgBox.InputValue : string.Empty;
        }
        
        public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
        {
            var window = GetMainView();
            if (window == null) return false;
            
            var parameters = new MessageBoxCustomParams
            {
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new() { Name = "Yes", IsDefault = true },
                    new() { Name = "No", IsCancel = true }
                },
                ContentTitle = title,
                ContentMessage = message,
                Icon = Icon.None,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                MaxWidth = 400,
                MaxHeight = 300,
                SizeToContent = SizeToContent.WidthAndHeight
            };
            
            var msgBox = MessageBoxManager.GetMessageBoxCustom(parameters);

            var result = await msgBox.ShowAsPopupAsync(window);
            
            return result == "Yes";
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
