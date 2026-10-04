using Avalonia.Controls;
using Avalonia.Interactivity;
using ORToolsDemos.cryptarithms;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORToolsDemos;

public partial class MainWindow : Window
{
    private const int MinimumAddends = 2;
    private const double PartialProductCharWidth = 13;

    private readonly List<TextBox> _addendTextBoxes = new();
    private readonly List<TextBlock> _solvedAddendTextBlocks = new();
    private readonly List<TextBox> _partialProductTextBoxes = new();
    private readonly List<TextBlock> _solvedPartialProductTextBlocks = new();

    public MainWindow()
    {
        InitializeComponent();

        AddAddendRow();
        AddAddendRow();
        UpdateMinusButtonEnabled();

        Opened += (_, _) => _addendTextBoxes[0].Focus();
    }

    private void AddAddendRow()
    {
        int index = _addendTextBoxes.Count + 1;

        var textBox = new TextBox
        {
            Height = 36,
            Watermark = $"Addend {index}",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            BorderThickness = new Avalonia.Thickness(0),
            FontFamily = "Consolas, Courier New, monospace",
            FontSize = 20,
        };
        textBox.TextChanged += InputTextBox_TextChanged;

        var textBlock = new TextBlock
        {
            Height = 36,
            TextAlignment = Avalonia.Media.TextAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontFamily = "Consolas, Courier New, monospace",
            FontSize = 20,
        };

        _addendTextBoxes.Add(textBox);
        _solvedAddendTextBlocks.Add(textBlock);

        AddendsPanel.Children.Add(textBox);
        SolvedAddendsPanel.Children.Add(textBlock);
    }

    private void RemoveAddendRow()
    {
        if (_addendTextBoxes.Count <= MinimumAddends)
        {
            return;
        }

        int lastIndex = _addendTextBoxes.Count - 1;

        AddendsPanel.Children.RemoveAt(lastIndex);
        SolvedAddendsPanel.Children.RemoveAt(lastIndex);

        _addendTextBoxes.RemoveAt(lastIndex);
        _solvedAddendTextBlocks.RemoveAt(lastIndex);
    }

    private void UpdateMinusButtonEnabled()
    {
        MinusButton.IsEnabled = _addendTextBoxes.Count > MinimumAddends;
    }

    private void PlusButton_Click(object sender, RoutedEventArgs e)
    {
        AddAddendRow();
        UpdateMinusButtonEnabled();
    }

    private void MinusButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveAddendRow();
        UpdateMinusButtonEnabled();
    }

    private void SolveButton_Click(object sender, RoutedEventArgs e)
    {
        string[] addends = _addendTextBoxes.Select(tb => tb.Text ?? string.Empty).ToArray();
        string sum = SumTextBox.Text ?? string.Empty;

        (string soln, long[] values, int solutionCount) = Sum.Solve(sum, addends);

        if (solutionCount == 0)
        {
            ResultTextBlock.Text = "No Solution";
            SolvedPanel.Opacity = 0;
            return;
        }

        var characters = new StringBuilder();
        var digitsText = new StringBuilder();
        for (int digit = 0; digit < soln.Length; digit++)
        {
            if (soln[digit] != ' ')
            {
                characters.Append(soln[digit]).Append(' ');
                digitsText.Append(digit).Append(' ');
            }
        }

        string resultText = $"{characters.ToString().TrimEnd()}\n{digitsText.ToString().TrimEnd()}";
        if (solutionCount > 1)
        {
            resultText += "\n(solution is not unique)";
        }

        ResultTextBlock.Text = resultText;

        for (int i = 0; i < _addendTextBoxes.Count; i++)
        {
            _solvedAddendTextBlocks[i].Text = FormatSolvedWord(addends[i], values[i]);
        }

        SolvedSumTextBlock.Text = FormatSolvedWord(sum, values[^1]);
        SolvedPanel.Opacity = 1;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (TextBox addendTextBox in _addendTextBoxes)
        {
            addendTextBox.Text = string.Empty;
        }

        SumTextBox.Text = string.Empty;
        ResultTextBlock.Text = string.Empty;

        foreach (TextBlock solvedAddendTextBlock in _solvedAddendTextBlocks)
        {
            solvedAddendTextBlock.Text = string.Empty;
        }

        SolvedSumTextBlock.Text = string.Empty;
        SolvedPanel.Opacity = 0;

        _addendTextBoxes[0].Focus();
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Text is null)
        {
            return;
        }

        string upper = textBox.Text.ToUpperInvariant();
        if (upper != textBox.Text)
        {
            int caretIndex = textBox.CaretIndex;
            textBox.Text = upper;
            textBox.CaretIndex = caretIndex;
        }
    }

    /// <summary>
    /// Formats a solved word's numeric value padded to the word's length, except that a
    /// leading '+' wildcard which solved to 0 is rendered as a blank space rather than '0'.
    /// </summary>
    private static string FormatSolvedWord(string word, long value)
    {
        string text = value.ToString().PadLeft(word.Length, '0');
        if (word.Length > 0 && word[0] == '+' && text[0] == '0')
        {
            text = ' ' + text.Substring(1);
        }

        return text;
    }

    private void MultiplierTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        InputTextBox_TextChanged(sender, e);
        UpdatePartialProductRows();
    }

    private void UpdatePartialProductRows()
    {
        int digitCount = (MultiplierTextBox.Text ?? string.Empty).Length;

        while (_partialProductTextBoxes.Count > digitCount)
        {
            int lastIndex = _partialProductTextBoxes.Count - 1;

            PartialProductsPanel.Children.RemoveAt(lastIndex);
            SolvedPartialProductsPanel.Children.RemoveAt(lastIndex);

            _partialProductTextBoxes.RemoveAt(lastIndex);
            _solvedPartialProductTextBlocks.RemoveAt(lastIndex);
        }

        while (_partialProductTextBoxes.Count < digitCount)
        {
            int index = _partialProductTextBoxes.Count;

            var textBox = new TextBox
            {
                Height = 36,
                Watermark = $"Partial {index + 1}",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                BorderThickness = new Avalonia.Thickness(0),
                FontFamily = "Consolas, Courier New, monospace",
                FontSize = 20,
                Margin = new Avalonia.Thickness(0, 0, index * PartialProductCharWidth, 0),
            };
            textBox.TextChanged += InputTextBox_TextChanged;

            var textBlock = new TextBlock
            {
                Height = 36,
                TextAlignment = Avalonia.Media.TextAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontFamily = "Consolas, Courier New, monospace",
                FontSize = 20,
                Margin = new Avalonia.Thickness(0, 0, index * PartialProductCharWidth, 0),
            };

            _partialProductTextBoxes.Add(textBox);
            _solvedPartialProductTextBlocks.Add(textBlock);

            PartialProductsPanel.Children.Add(textBox);
            SolvedPartialProductsPanel.Children.Add(textBlock);
        }
    }

    private void MultiplySolveButton_Click(object sender, RoutedEventArgs e)
    {
        string multiplicand = MultiplicandTextBox.Text ?? string.Empty;
        string multiplier = MultiplierTextBox.Text ?? string.Empty;
        string product = ProductTextBox.Text ?? string.Empty;
        string[] partialProducts = _partialProductTextBoxes
            .Select(tb => tb.Text ?? string.Empty)
            .Select(text => string.IsNullOrEmpty(text)
                ? new string('+', multiplicand.Length + 1)
                : text)
            .ToArray();

        (string soln, long[] values, int solutionCount) = Multiply.Solve(multiplicand, multiplier, partialProducts, product);

        if (solutionCount == 0)
        {
            MultiplyResultTextBlock.Text = "No Solution";
            MultiplySolvedPanel.Opacity = 0;
            return;
        }

        var characters = new StringBuilder();
        var digitsText = new StringBuilder();
        for (int digit = 0; digit < soln.Length; digit++)
        {
            if (soln[digit] != ' ')
            {
                characters.Append(soln[digit]).Append(' ');
                digitsText.Append(digit).Append(' ');
            }
        }

        string resultText = $"{characters.ToString().TrimEnd()}\n{digitsText.ToString().TrimEnd()}";
        if (solutionCount > 1)
        {
            resultText += "\n(solution is not unique)";
        }

        MultiplyResultTextBlock.Text = resultText;

        SolvedMultiplicandTextBlock.Text = FormatSolvedWord(multiplicand, values[0]);
        SolvedMultiplierTextBlock.Text = FormatSolvedWord(multiplier, values[1]);
        for (int i = 0; i < _partialProductTextBoxes.Count; i++)
        {
            _solvedPartialProductTextBlocks[i].Text = FormatSolvedWord(partialProducts[i], values[2 + i]);
        }
        SolvedProductTextBlock.Text = FormatSolvedWord(product, values[^1]);

        MultiplySolvedPanel.Opacity = 1;
    }

    private void MultiplyResetButton_Click(object sender, RoutedEventArgs e)
    {
        MultiplicandTextBox.Text = string.Empty;
        MultiplierTextBox.Text = string.Empty;
        ProductTextBox.Text = string.Empty;
        MultiplyResultTextBlock.Text = string.Empty;

        foreach (TextBox partialProductTextBox in _partialProductTextBoxes)
        {
            partialProductTextBox.Text = string.Empty;
        }

        SolvedMultiplicandTextBlock.Text = string.Empty;
        SolvedMultiplierTextBlock.Text = string.Empty;
        foreach (TextBlock solvedPartialProductTextBlock in _solvedPartialProductTextBlocks)
        {
            solvedPartialProductTextBlock.Text = string.Empty;
        }
        SolvedProductTextBlock.Text = string.Empty;
        MultiplySolvedPanel.Opacity = 0;

        MultiplicandTextBox.Focus();
    }
}