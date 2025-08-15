using System;
using System.Threading;
using Aesir.Client.Services;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Aesir.Client.Controls;

public partial class AssistantAvatarUserControl : UserControl
{
    private const string DefaultAvatarColor = "#4A90E2";

    public static readonly StyledProperty<HandsFreeState> CurrentStateProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, HandsFreeState>(nameof(CurrentState),
            defaultValue: HandsFreeState.Idle);

    public static readonly StyledProperty<string> SvgCssProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, string>(nameof(SvgCss),
            defaultValue: $".st0 {{fill: {DefaultAvatarColor}}}");

    public static readonly StyledProperty<string> StateTextProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, string>(nameof(StateText),
            defaultValue: string.Empty);

    public static readonly StyledProperty<string> UtteranceTextProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, string>(nameof(UtteranceText),
            defaultValue: string.Empty);
    
    public static readonly StyledProperty<string> UtteranceDisplayTextProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, string>(nameof(UtteranceDisplayText),
            defaultValue: "No speech detected.");

    public static readonly StyledProperty<double> AnimationSpeedProperty =
        AvaloniaProperty.Register<AssistantAvatarUserControl, double>(
            nameof(AnimationSpeed),
            defaultValue: 1.0,
            coerce: (_, value) => Math.Clamp(value, 1.0, 10.0));

    public HandsFreeState CurrentState
    {
        get => GetValue(CurrentStateProperty);
        set => SetValue(CurrentStateProperty, value);
    }

    private string SvgCss
    {
        get => GetValue(SvgCssProperty);
        set => SetValue(SvgCssProperty, value);
    }

    public double AnimationSpeed
    {
        get => GetValue(AnimationSpeedProperty);
        set => SetValue(AnimationSpeedProperty, value);
    }
    
    private string StateText
    {
        get => GetValue(StateTextProperty);
        set => SetValue(StateTextProperty, value);
    }

    public string UtteranceText
    {
        get => GetValue(UtteranceTextProperty);
        set => SetValue(UtteranceTextProperty, value);
    }
    
    private string UtteranceDisplayText
    {
        get => GetValue(UtteranceDisplayTextProperty);
        set => SetValue(UtteranceDisplayTextProperty, value);
    }

    private DispatcherTimer? _animationTimer;
    private readonly TimeSpan _animationDuration = TimeSpan.FromSeconds(1.5);
    private DateTime _animationStartTime;
    private CancellationTokenSource? _fadeOutCancellationTokenSource;
    
    public AssistantAvatarUserControl()
    {
        InitializeComponent();

        this.GetObservable(CurrentStateProperty).Subscribe(value =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StateText = value.ToString();
                
                if (value is HandsFreeState.Listening or HandsFreeState.Processing)
                {
                    StartAnimation();
                }
                else
                {
                    StopAnimation();
                    SvgCss = value switch
                    {
                        HandsFreeState.Idle => $".st0 {{fill: {DefaultAvatarColor}}}",
                        HandsFreeState.Error => ".st0 {fill: #FF6B6B}",
                        _ => $".st0 {{fill: {DefaultAvatarColor}}}"
                    };
                }
            });
        });
        
        this.GetObservable(UtteranceTextProperty).Subscribe(value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            
            UtteranceDisplayText = value;
            Dispatcher.UIThread.InvokeAsync(StartUtteranceFadeOutAnimation);
        });
    }

    private void StartAnimation()
    {
        if (_animationTimer is { IsEnabled: true }) return;

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationStartTime = DateTime.Now;
        _animationTimer.Start();
    }

    private void StopAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer = null;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _animationStartTime;
        // A full cycle (forward and back) will take twice the animation duration.
        var totalCycleDuration = _animationDuration.TotalMilliseconds * 2;

        // Calculate a progress value that loops from 0.0 to 1.0 over the cycle duration.
        var cycleProgress = (elapsed.TotalMilliseconds * AnimationSpeed / totalCycleDuration) % 1.0;

        // Use a sinusoidal wave to create a smooth "ping-pong" effect.
        // This will map the progress from 0 -> 1 -> 0 over one cycle.
        var easedProgress = Math.Sin(cycleProgress * Math.PI);

        var startColor = Color.Parse(DefaultAvatarColor);
        var endColor = Color.Parse("#F9F9F9");

        var r = (byte)(startColor.R + (endColor.R - startColor.R) * easedProgress);
        var g = (byte)(startColor.G + (endColor.G - startColor.G) * easedProgress);
        var b = (byte)(startColor.B + (endColor.B - startColor.B) * easedProgress);

        var fillValue = $"#{r:X2}{g:X2}{b:X2}";
        SvgCss = $".st0 {{fill: {fillValue};}}";
    }

    private void SvgImage_OnInvalidated(object? sender, EventArgs e)
    {
        AvatarImage?.InvalidateVisual();
    }
    
    private async void StartUtteranceFadeOutAnimation()
    {
        if (string.IsNullOrWhiteSpace(UtteranceText))
            return;

        _fadeOutCancellationTokenSource?.Cancel();
        _fadeOutCancellationTokenSource = new CancellationTokenSource();
        var token = _fadeOutCancellationTokenSource.Token;

        try
        {
            var utteranceTextBlock = this.FindControl<TextBlock>("UtteranceDisplayTextBlock");
            if (utteranceTextBlock == null) return;

            utteranceTextBlock.Opacity = 1.0;

            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(4),
                Easing = new LinearEasing(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(OpacityProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(OpacityProperty, 0.0) }
                    }
                }
            };

            await animation.RunAsync(utteranceTextBlock, token);
            
            if (!token.IsCancellationRequested)
            {
                utteranceTextBlock.Opacity = 0.0;
                UtteranceDisplayText = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    
    public void Dispose()
    {
        _animationTimer?.Stop();
        _fadeOutCancellationTokenSource?.Cancel();
        _fadeOutCancellationTokenSource?.Dispose();
    }


}