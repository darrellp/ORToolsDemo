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

        string soln = Sum.Solve(sum, addends);

        if (soln is null)
        {
            ResultTextBlock.Text = "No Solution";
            SolvedPanel.Opacity = 0;
            return;
        }

        var characters = new StringBuilder();
        var values = new StringBuilder();
        for (int digit = 0; digit < soln.Length; digit++)
        {
            if (soln[digit] != ' ')
            {
                characters.Append(soln[digit]).Append(' ');
                values.Append(digit).Append(' ');
            }
        }

        ResultTextBlock.Text = $"{characters.ToString().TrimEnd()}\n{values.ToString().TrimEnd()}";

        var digitsByLetter = new Dictionary<char, int>();
        for (int digit = 0; digit < soln.Length; digit++)
        {
            if (soln[digit] != ' ')
            {
                digitsByLetter[soln[digit]] = digit;
            }
        }

        for (int i = 0; i < _addendTextBoxes.Count; i++)
        {
            _solvedAddendTextBlocks[i].Text = ToDigits(addends[i], digitsByLetter);
        }

        SolvedSumTextBlock.Text = ToDigits(sum, digitsByLetter);
        SolvedPanel.Opacity = 1;
    }

    private static string ToDigits(string word, Dictionary<char, int> digitsByLetter)
    {
        var result = new StringBuilder();
        foreach (char c in word)
        {
            result.Append(digitsByLetter.TryGetValue(c, out int digit) ? digit.ToString() : c.ToString());
        }

        return result.ToString();
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