using System;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Controls;

namespace Aesir.Client.Views;

public partial class ChatView : UserControl, IRecipient<ShowGeneralSettingsMessage>, IDisposable
{
    private readonly ApplicationState _appState;
    
    public ChatView()
    {
        InitializeComponent();
        
        _appState = Ioc.Default.GetService<ApplicationState>()!;
        
        TitleTextBlock.Margin = new Thickness(35, 0, 0, 0);
        
        if (MessageAiTextBox != null)
        {
            MessageAiTextBox.AttachedToVisualTree += (s,e) => MessageAiTextBox.Focus();
            MessageAiTextBox.SendMessageRequested += (s, e) => SendMessageButton.Command.Execute(null);
            
            _appState.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ReadyForNewAiMessage" && _appState.ReadyForNewAiMessage)
                {
                    MessageAiTextBox.Focus();
                }
            };
        }
        
        WeakReferenceMessenger.Default.Register<ShowGeneralSettingsMessage>(this);
    }

    private void SplitView_OnPaneOpening(object? sender, CancelRoutedEventArgs e)
    {
        TitleTextBlock.Margin = new Thickness(35, 0, 0, 0);
    }

    private void SplitView_OnPaneClosing(object? sender, CancelRoutedEventArgs e)
    {
        TitleTextBlock.Margin = new Thickness(85, 0, 0, 0);
    }

    private void MessageAiTextBox_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property.Name == "IsEnabled" && e.NewValue is true)
        {
            MessageAiTextBox.Focus();
        }
    }

    public void Receive(ShowGeneralSettingsMessage message)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var configurationService = Ioc.Default.GetService<IConfigurationService>()!;
                var modelService = Ioc.Default.GetService<IModelService>()!;
                
                var generalSettingsViewModel =
                    new GeneralSettingsViewViewModel(notificationService, configurationService, modelService);
                
                generalSettingsViewModel.IsActive = true;
                
                var options = new OverlayDialogOptions()
                {
                    FullScreen = false,
                    HorizontalAnchor = HorizontalPosition.Center,
                    VerticalAnchor = VerticalPosition.Center,
                    Mode = DialogMode.None,
                    Buttons = DialogButton.None,
                    Title = "General Settings",
                    CanLightDismiss = false,
                    CanDragMove = true,
                    IsCloseButtonVisible = true,
                    CanResize = false
                };

                var dialogResult =
                    await OverlayDialog.ShowModal<GeneralSettingsView, GeneralSettingsViewViewModel>(
                        generalSettingsViewModel, options: options);

                generalSettingsViewModel.IsActive = false;
                
                if (dialogResult == DialogResult.OK)
                {
                    // TODO
                }
            }
            catch (Exception e)
            {
                // TODO handle exception
            }
        });
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<ShowGeneralSettingsMessage>(this);
    }
}