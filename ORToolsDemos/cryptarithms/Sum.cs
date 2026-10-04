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
    /// Characters '0'-'9' are treated as literal fixed digits, and '*' is a wildcard that
    /// may be assigned any digit independently (not constrained to be distinct from anything).
    /// </summary>
    /// <param name="sum">The word representing the sum.</param>
    /// <param name="addends">The words representing the addends being summed.</param>
    /// <returns>
    /// A tuple of:
    /// - Soln: a string where soln[i] is the letter assigned the digit i in the solution
    ///   (wildcard and literal-digit characters are not represented here), or null if unsolvable.
    /// - Values: an (addends.Length + 1) array of ints giving the numeric value of each addend
    ///   (in order) followed by the sum, with wildcards filled in, or null if unsolvable.
    /// - SolutionCount: 0 if unsolvable, 1 if the solution is unique, or 2 if there is more
    ///   than one solution.
    /// </returns>
    public static (string Soln, int[] Values, int SolutionCount) Solve(string sum, string[] addends)
    {
        var model = new CpModel();

        var words = new List<string>(addends) { sum };

        // Collect all distinct letters (excluding literal digits and the '*' wildcard)
        // across the sum and the addends.
        var letters = new List<char>();
        foreach (string word in words)
        {
            foreach (char c in word)
            {
                if (!char.IsDigit(c) && c != '*' && !letters.Contains(c))
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

        // Builds the per-character variables for a word: shared letter variables, fresh
        // unconstrained variables for each '*' wildcard, and constants for literal digits.
        var wildcardVars = new List<IntVar>();
        List<IntVar> BuildWordVars(string word)
        {
            var vars = new List<IntVar>(word.Length);
            foreach (char c in word)
            {
                if (char.IsDigit(c))
                {
                    vars.Add(model.NewConstant(c - '0'));
                }
                else if (c == '*')
                {
                    IntVar wildcard = model.NewIntVar(0, 9, "*");
                    wildcardVars.Add(wildcard);
                    vars.Add(wildcard);
                }
                else
                {
                    vars.Add(digits[c]);
                }
            }

            return vars;
        }

        var wordVarsList = words.Select(BuildWordVars).ToList();

        // Leading characters of each word cannot be assigned 0 (literal leading digits are
        // left as given, since they are not variables).
        for (int w = 0; w < words.Count; w++)
        {
            string word = words[w];
            if (word.Length > 1 && !char.IsDigit(word[0]))
            {
                model.Add(wordVarsList[w][0] != 0);
            }
        }

        // Build the linear equation: sum of addends == sum.
        LinearExpr BuildWordExpr(List<IntVar> vars)
        {
            var coeffs = new long[vars.Count];
            long placeValue = 1;
            for (int i = vars.Count - 1; i >= 0; i--)
            {
                coeffs[i] = placeValue;
                placeValue *= 10;
            }
            return LinearExpr.WeightedSum(vars, coeffs);
        }

        LinearExpr total = null;
        for (int w = 0; w < addends.Length; w++)
        {
            LinearExpr expr = BuildWordExpr(wordVarsList[w]);
            total = total is null ? expr : LinearExpr.Sum(new[] { total, expr });
        }

        model.Add(total == BuildWordExpr(wordVarsList[^1]));

        var solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);

        if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
        {
            return (null, null, 0);
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

        long WordValue(List<IntVar> vars)
        {
            long value = 0;
            foreach (IntVar v in vars)
            {
                value = (value * 10) + solver.Value(v);
            }

            return value;
        }

        var values = new int[addends.Length + 1];
        for (int w = 0; w < words.Count; w++)
        {
            values[w] = (int)WordValue(wordVarsList[w]);
        }

        // Determine whether the solution is unique by forbidding the found assignment of
        // every decision variable (letters and wildcards) and checking for another solution.
        var allVars = digits.Values.Concat(wildcardVars).ToList();
        var diffLiterals = new List<ILiteral>();
        foreach (IntVar v in allVars)
        {
            long value = solver.Value(v);
            BoolVar differs = model.NewBoolVar($"{v.Name()}_differs");
            model.Add(v != value).OnlyEnforceIf(differs);
            model.Add(v == value).OnlyEnforceIf(differs.Not());
            diffLiterals.Add(differs);
        }

        model.AddBoolOr(diffLiterals);

        var uniquenessSolver = new CpSolver();
        CpSolverStatus uniquenessStatus = uniquenessSolver.Solve(model);

        int solutionCount = (uniquenessStatus == CpSolverStatus.Optimal || uniquenessStatus == CpSolverStatus.Feasible) ? 2 : 1;

        return (new string(soln), values, solutionCount);
    }
}
