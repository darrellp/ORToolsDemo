using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.OrTools.Sat;

namespace ORToolsDemos.cryptarithms;

static internal class Sum
{
    /// <summary>
    /// Solves a cryptarithm of the form addends[0] + addends[1] + ... + addends[n-1] == sum,
    /// where each distinct letter represents a distinct digit (0-9) using OR-Tools CP-SAT.
    /// </summary>
    /// <param name="sum">The word representing the sum.</param>
    /// <param name="addends">The words representing the addends being summed.</param>
    /// <returns>
    /// A string "soln" where soln[i] is the character assigned the digit i in the solution,
    /// or null if no solution exists.
    /// </returns>
    public static string Solve(string sum, string[] addends)
    {
        var model = new CpModel();

        // Collect all distinct letters across the sum and the addends.
        var letters = new List<char>();
        foreach (char c in sum)
        {
            if (!letters.Contains(c))
            {
                letters.Add(c);
            }
        }
        foreach (string addend in addends)
        {
            foreach (char c in addend)
            {
                if (!letters.Contains(c))
                {
                    letters.Add(c);
                }
            }
        }

        if (letters.Count > 10)
        {
            throw new ArgumentException("Too many distinct letters; cannot assign unique digits 0-9.");
        }

        // One variable per letter, domain 0-9.
        var digits = new Dictionary<char, IntVar>();
        foreach (char c in letters)
        {
            digits[c] = model.NewIntVar(0, 9, c.ToString());
        }

        // All letters must be assigned distinct digits.
        model.AddAllDifferent(digits.Values);

        // Leading letters of each word cannot be assigned 0.
        var words = new List<string>(addends) { sum };
        foreach (string word in words)
        {
            if (word.Length > 1)
            {
                model.Add(digits[word[0]] != 0);
            }
        }

        // Build the linear equation: sum of addends == sum.
        LinearExpr BuildWordExpr(string word)
        {
            var terms = new List<IntVar>();
            var coeffs = new List<long>();
            long placeValue = 1;
            for (int i = word.Length - 1; i >= 0; i--)
            {
                terms.Add(digits[word[i]]);
                coeffs.Add(placeValue);
                placeValue *= 10;
            }
            return LinearExpr.WeightedSum(terms, coeffs);
        }

        LinearExpr total = null;
        foreach (string addend in addends)
        {
            LinearExpr expr = BuildWordExpr(addend);
            total = total is null ? expr : LinearExpr.Sum(new[] { total, expr });
        }

        model.Add(total == BuildWordExpr(sum));

        var solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);

        if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
        {
            return null;
        }

        var soln = new char[10];
        for (int i = 0; i < 10; i++)
        {
            soln[i] = ' ';
        }
        foreach (char c in letters)
        {
            int digit = (int)solver.Value(digits[c]);
            soln[digit] = c;
        }

        return new string(soln);
    }
}
