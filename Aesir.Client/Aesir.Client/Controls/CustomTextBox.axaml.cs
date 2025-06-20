using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Aesir.Client.Controls;

public partial class CustomTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CustomTextBox, string>(nameof(Text));

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<CustomTextBox, string>(nameof(Watermark), string.Empty);

    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<CustomTextBox, bool>(nameof(IsEnabled), true);

    public static readonly StyledProperty<object?> InnerLeftContentProperty =
        AvaloniaProperty.Register<CustomTextBox, object?>(nameof(InnerLeftContent));

    public static readonly StyledProperty<object?> InnerRightContentProperty =
        AvaloniaProperty.Register<CustomTextBox, object?>(nameof(InnerRightContent));

    public static readonly StyledProperty<string> ClassesProperty =
        AvaloniaProperty.Register<CustomTextBox, string>(nameof(Classes), string.Empty);

    public static readonly StyledProperty<double> MinHeightProperty =
        AvaloniaProperty.Register<CustomTextBox, double>(nameof(MinHeight), double.NaN);

    public static readonly StyledProperty<double> MaxHeightProperty =
        AvaloniaProperty.Register<CustomTextBox, double>(nameof(MaxHeight), double.NaN);

    public static readonly StyledProperty<string> NewLineProperty =
        AvaloniaProperty.Register<CustomTextBox, string>(nameof(NewLine), string.Empty);

    public static readonly StyledProperty<Avalonia.Layout.VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<CustomTextBox, Avalonia.Layout.VerticalAlignment>(nameof(VerticalContentAlignment));

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        AvaloniaProperty.Register<CustomTextBox, TextWrapping>(nameof(TextWrapping));

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<CustomTextBox, double>(nameof(LineHeight), double.NaN);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public new bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public object? InnerLeftContent
    {
        get => GetValue(InnerLeftContentProperty);
        set => SetValue(InnerLeftContentProperty, value);
    }

    public object? InnerRightContent
    {
        get => GetValue(InnerRightContentProperty);
        set => SetValue(InnerRightContentProperty, value);
    }

    public string Classes
    {
        get => GetValue(ClassesProperty);
        set => SetValue(ClassesProperty, value);
    }

    public new double MinHeight
    {
        get => GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public new double MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }

    public string NewLine
    {
        get => GetValue(NewLineProperty);
        set => SetValue(NewLineProperty, value);
    }

    public Avalonia.Layout.VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public event EventHandler<KeyEventArgs>? KeyUp;
    public event EventHandler<KeyEventArgs>? KeyDown;
    
    public event EventHandler? SendMessageRequested;

    public CustomTextBox()
    {
        InitializeComponent();
        SetupTextBox();
    }

    private void SetupTextBox()
    {
        InnerTextBox.KeyDown += OnKeyDown;
        InnerTextBox.KeyUp += OnKeyUp;

        this.GetObservable(WatermarkProperty).Subscribe(value => InnerTextBox.Watermark = value);
        this.GetObservable(IsEnabledProperty).Subscribe(value => InnerTextBox.IsEnabled = value);
        this.GetObservable(InnerLeftContentProperty).Subscribe(value => InnerTextBox.InnerLeftContent = value);
        this.GetObservable(InnerRightContentProperty).Subscribe(value => InnerTextBox.InnerRightContent = value);
        this.GetObservable(ClassesProperty).Subscribe(value =>
        {
            if (!string.IsNullOrEmpty(value))
            {
                var classes = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                InnerTextBox.Classes.Clear();
                foreach (var cls in classes)
                {
                    InnerTextBox.Classes.Add(cls);
                }
            }
        });
        this.GetObservable(MinHeightProperty).Subscribe(value =>
        {
            if (!double.IsNaN(value))
                InnerTextBox.MinHeight = value;
        });
        this.GetObservable(MaxHeightProperty).Subscribe(value =>
        {
            if (!double.IsNaN(value))
                InnerTextBox.MaxHeight = value;
        });
        this.GetObservable(VerticalContentAlignmentProperty)
            .Subscribe(value => InnerTextBox.VerticalContentAlignment = value);
        this.GetObservable(TextWrappingProperty).Subscribe(value => InnerTextBox.TextWrapping = value);
        this.GetObservable(LineHeightProperty).Subscribe(value =>
        {
            if (!double.IsNaN(value))
                InnerTextBox.LineHeight = value;
        });
        this.GetObservable(NewLineProperty).Subscribe(value =>
        {
            if (!string.IsNullOrEmpty(value))
                InnerTextBox.NewLine = value;
        });

        InnerTextBox.GetObservable(TextBox.TextProperty).Subscribe(value => Text = value ?? string.Empty);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return)
        {
            e.Handled = true;
        }
        else
        {
            KeyDown?.Invoke(sender, e);
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Return:
            {
                e.Handled = true;
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    HandleShiftEnter();
                }
                else
                {
                    if (Text?.Length > 0)
                    {
                        SendMessageRequested?.Invoke(this, EventArgs.Empty);
                        Focus();
                    }
                }

                break;
            }
            case Key.Space:
                HandleSpaceKey();
                break;
        }

        KeyUp?.Invoke(sender, e);
    }

    public void Focus()
    {
        InnerTextBox.Focus();
    }

    public int CaretIndex
    {
        get => InnerTextBox.CaretIndex;
        set => InnerTextBox.CaretIndex = value;
    }

    private void HandleShiftEnter()
    {
        var currentText = Text ?? "";
        var caretIndex = CaretIndex;

        var currentLineStart = GetLineStart(currentText, caretIndex);
        var currentLineEnd = GetLineEnd(currentText, caretIndex);
        var currentLine = currentText.Substring(currentLineStart, currentLineEnd - currentLineStart);

        string newLinePrefix = "";
        bool isEmptyListItem = false;

        if (currentLine.TrimStart().StartsWith("- "))
        {
            var contentAfterDash = currentLine.TrimStart().Substring(2);
            if (string.IsNullOrWhiteSpace(contentAfterDash))
            {
                isEmptyListItem = true;
            }
            else
            {
                var indent = GetIndentation(currentLine);
                newLinePrefix = indent + "- ";
            }
        }
        else if (IsNumberedListItem(currentLine.TrimStart()))
        {
            var parts = currentLine.TrimStart().Split(new[] { ". " }, 2, StringSplitOptions.None);
            var contentAfterNumber = parts.Length > 1 ? parts[1] : "";
            if (string.IsNullOrWhiteSpace(contentAfterNumber))
            {
                isEmptyListItem = true;
            }
            else
            {
                var indent = GetIndentation(currentLine);
                var nextNumber = GetNextNumberedListNumber(currentLine.TrimStart());
                newLinePrefix = indent + nextNumber + ". ";
            }
        }

        if (isEmptyListItem)
        {
            var indent = GetIndentation(currentLine);
            var newText = currentText.Remove(currentLineStart, currentLineEnd - currentLineStart)
                .Insert(currentLineStart, indent + Environment.NewLine);
            Text = newText;
            CaretIndex = currentLineStart + indent.Length + Environment.NewLine.Length;
        }
        else
        {
            var newText = currentText.Insert(caretIndex, Environment.NewLine + newLinePrefix);
            Text = newText;
            CaretIndex = caretIndex + Environment.NewLine.Length + newLinePrefix.Length;
        }
    }

    private void HandleSpaceKey()
    {
        var currentText = Text ?? "";
        var caretIndex = CaretIndex;

        if (caretIndex >= 2 && currentText.Substring(caretIndex - 2, 2) == "* ")
        {
            var lineStart = GetLineStart(currentText, caretIndex);
            var beforeAsterisk = currentText.Substring(lineStart, caretIndex - lineStart - 2);

            if (string.IsNullOrWhiteSpace(beforeAsterisk))
            {
                var newText = currentText.Remove(caretIndex - 2, 2).Insert(caretIndex - 2, "- ");
                Text = newText;
                CaretIndex = caretIndex;
            }
        }
    }

    private int GetLineStart(string text, int caretIndex)
    {
        // Need to find line start 
        var lineStart = text.LastIndexOf('\n', caretIndex - 1);
        return lineStart == -1 ? 0 : lineStart + 1;
    }

    private int GetLineEnd(string text, int caretIndex)
    {
        var lineEnd = text.IndexOf('\n', caretIndex);
        return lineEnd == -1 ? text.Length : lineEnd;
    }

    private string GetIndentation(string line)
    {
        var indent = "";
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t')
                indent += c;
            else
                break;
        }

        return indent;
    }

    private bool IsNumberedListItem(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;

        var parts = line.Split(new[] { ". " }, 2, StringSplitOptions.None);
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out _);
    }

    private int GetNextNumberedListNumber(string line)
    {
        var parts = line.Split(new[] { ". " }, 2, StringSplitOptions.None);
        if (parts.Length >= 1 && int.TryParse(parts[0], out int currentNumber))
        {
            return currentNumber + 1;
        }

        return 1;
    }
}
