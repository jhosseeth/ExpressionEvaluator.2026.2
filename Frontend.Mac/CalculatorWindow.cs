using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Backend;

namespace Frontend.Mac;

internal sealed class CalculatorWindow : Window
{
    private readonly TextBox _display;
    private readonly TextBlock _status;
    private string _expression = string.Empty;
    private string? _lastResult;
    private bool _showingResult;

    public CalculatorWindow()
    {
        Title = "Functions Evaluator";
        Width = 560;
        Height = 520;
        MinWidth = 440;
        MinHeight = 460;
        Background = Brushes.Black;

        _display = new TextBox
        {
            IsReadOnly = true,
            FontSize = 25,
            Height = 58,
            Padding = new Thickness(10),
            Background = Brushes.Green,
            Foreground = Brushes.White,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        _status = new TextBlock
        {
            Foreground = Brushes.OrangeRed,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 20,
        };

        var layout = new StackPanel
        {
            Margin = new Thickness(12),
            Spacing = 8,
        };
        layout.Children.Add(_display);
        layout.Children.Add(_status);
        layout.Children.Add(CreateKeypad());
        Content = layout;

        UpdateDisplay();
    }

    private Grid CreateKeypad()
    {
        var keypad = new Grid
        {
            Height = 380,
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Star),
            },
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(2, GridUnitType.Star)),
            },
        };

        AddButton(keypad, "7", 0, 0);
        AddButton(keypad, "8", 0, 1);
        AddButton(keypad, "9", 0, 2);
        AddButton(keypad, "(", 0, 3, isOperator: true);
        AddButton(keypad, ")", 0, 4, isOperator: true);
        AddButton(keypad, "Delete", 0, 5, isOperator: true, action: DeleteLastCharacter);

        AddButton(keypad, "4", 1, 0);
        AddButton(keypad, "5", 1, 1);
        AddButton(keypad, "6", 1, 2);
        AddButton(keypad, "*", 1, 3, isOperator: true);
        AddButton(keypad, "/", 1, 4, isOperator: true);
        AddButton(keypad, "Clear", 1, 5, isOperator: true, action: ClearExpression);

        AddButton(keypad, "1", 2, 0);
        AddButton(keypad, "2", 2, 1);
        AddButton(keypad, "3", 2, 2);
        AddButton(keypad, "+", 2, 3, isOperator: true);
        AddButton(keypad, "-", 2, 4, isOperator: true);
        AddButton(keypad, "^", 2, 5, isOperator: true);

        AddButton(keypad, "0", 3, 0, columnSpan: 2);
        AddButton(keypad, ".", 3, 2);
        AddButton(keypad, "=", 3, 3, columnSpan: 3, isOperator: true, action: EvaluateExpression);

        return keypad;
    }

    private void AddButton(
        Grid keypad,
        string label,
        int row,
        int column,
        int columnSpan = 1,
        bool isOperator = false,
        Action? action = null)
    {
        var button = new Button
        {
            Content = label,
            FontSize = 22,
            Margin = new Thickness(3),
            Padding = new Thickness(5),
            Background = isOperator ? Brushes.DarkOrange : Brushes.White,
            Foreground = Brushes.Black,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        button.Click += (_, _) => (action ?? (() => Append(label)))();
        Grid.SetRow(button, row);
        Grid.SetColumn(button, column);
        Grid.SetColumnSpan(button, columnSpan);
        keypad.Children.Add(button);
    }

    private void Append(string value)
    {
        if (_showingResult)
        {
            _expression = IsBinaryOperator(value) && _lastResult is not null
                ? _lastResult
                : string.Empty;
            _showingResult = false;
            _lastResult = null;
        }

        _expression += value;
        _status.Text = string.Empty;
        UpdateDisplay();
    }

    private void DeleteLastCharacter()
    {
        if (_showingResult)
        {
            _showingResult = false;
            _lastResult = null;
        }

        if (_expression.Length > 0)
        {
            _expression = _expression[..^1];
        }

        _status.Text = string.Empty;
        UpdateDisplay();
    }

    private void ClearExpression()
    {
        _expression = string.Empty;
        _lastResult = null;
        _showingResult = false;
        _status.Text = string.Empty;
        UpdateDisplay();
    }

    private void EvaluateExpression()
    {
        try
        {
            var result = ExpressionEvaluator.Evaluate(_expression);
            _lastResult = result.ToString("G", CultureInfo.InvariantCulture);
            _display.Text = $"{_expression} = {_lastResult}";
            _showingResult = true;
            _status.Text = string.Empty;
        }
        catch (FormatException exception)
        {
            ShowError(exception.Message);
        }
        catch (DivideByZeroException exception)
        {
            ShowError(exception.Message);
        }
        catch (ArithmeticException exception)
        {
            ShowError(exception.Message);
        }
    }

    private void ShowError(string message)
    {
        _showingResult = false;
        _lastResult = null;
        _display.Text = _expression;
        _status.Text = message;
    }

    private void UpdateDisplay()
    {
        _display.Text = _expression;
    }

    private static bool IsBinaryOperator(string value) => value is "+" or "-" or "*" or "/" or "^";
}
