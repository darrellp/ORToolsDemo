using Avalonia.Controls;
using Avalonia.Interactivity;
using ORToolsDemos.cryptarithms;
using System.Collections.Generic;
using System.Text;

namespace ORToolsDemos;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SolveButton_Click(object sender, RoutedEventArgs e)
    {
        string addendOne = AddendOneTextBox.Text ?? string.Empty;
        string addendTwo = AddendTwoTextBox.Text ?? string.Empty;
        string sum = SumTextBox.Text ?? string.Empty;

        string soln = Sum.Solve(sum, new[] { addendOne, addendTwo });

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

        SolvedAddendOneTextBlock.Text = ToDigits(addendOne, digitsByLetter);
        SolvedAddendTwoTextBlock.Text = ToDigits(addendTwo, digitsByLetter);
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
        AddendOneTextBox.Text = string.Empty;
        AddendTwoTextBox.Text = string.Empty;
        SumTextBox.Text = string.Empty;
        ResultTextBlock.Text = string.Empty;
        SolvedAddendOneTextBlock.Text = string.Empty;
        SolvedAddendTwoTextBlock.Text = string.Empty;
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