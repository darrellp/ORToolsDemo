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

    private readonly List<TextBox> _addendTextBoxes = new();
    private readonly List<TextBlock> _solvedAddendTextBlocks = new();

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

        (string soln, int[] values, int solutionCount) = Sum.Solve(sum, addends);

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
            _solvedAddendTextBlocks[i].Text = values[i].ToString().PadLeft(addends[i].Length, '0');
        }

        SolvedSumTextBlock.Text = values[^1].ToString().PadLeft(sum.Length, '0');
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
}