using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.Sat;

namespace ORToolsDemos.cryptarithms;

static internal class Multiply
{
    /// <summary>
    /// Solves a multiplication cryptarithm of the form multiplicand * multiplier == product,
    /// where each distinct letter represents a distinct digit (0-9) using OR-Tools CP-SAT.
    /// Characters '0'-'9' are treated as literal fixed digits, and '*' and '+' are wildcards
    /// that may be assigned any digit independently (not constrained to be distinct from
    /// anything). As a leading digit, '*' cannot be assigned 0, but '+' can. Additionally, a
    /// '*' that resolves to 0 must have a non-zero digit somewhere to its left in the same
    /// word (e.g. "**5" cannot be solved as "05").
    /// partialProducts[i] is the partial product produced by multiplying the multiplicand by
    /// the i-th digit of the multiplier, counting from the rightmost (least significant) digit.
    /// </summary>
    /// <returns>
    /// A tuple of:
    /// - Soln: a string where soln[i] is the letter assigned the digit i in the solution
    ///   (wildcard and literal-digit characters are not represented here), or null if unsolvable.
    /// - Values: an array giving the numeric value of the multiplicand, the multiplier, each
    ///   partial product (in order), and finally the product, with wildcards filled in,
    ///   or null if unsolvable.
    /// - SolutionCount: 0 if unsolvable, 1 if the solution is unique, or 2 if there is more
    ///   than one solution.
    /// </returns>
    public static (string Soln, long[] Values, int SolutionCount) Solve(
        string multiplicand, string multiplier, string[] partialProducts, string product)
    {
        var model = new CpModel();

        var words = new List<string> { multiplicand, multiplier };
        words.AddRange(partialProducts);
        words.Add(product);

        // Collect all distinct letters (excluding literal digits and the '*' wildcard)
        // across all words.
        var letters = new List<char>();
        foreach (string word in words)
        {
            foreach (char c in word)
            {
                if (!char.IsDigit(c) && c != '*' && c != '+' && !letters.Contains(c))
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
                else if (c == '*' || c == '+')
                {
                    IntVar wildcard = model.NewIntVar(0, 9, c.ToString());
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
            if (word.Length > 1 && !char.IsDigit(word[0]) && word[0] != '+')
            {
                model.Add(wordVarsList[w][0] != 0);
            }
        }

        // A '*' wildcard that resolves to 0 must have a non-zero digit somewhere to its left
        // in the same word (so e.g. "**5" cannot be solved as "05").
        var nonZeroLiterals = new Dictionary<IntVar, BoolVar>();
        BoolVar NonZero(IntVar v)
        {
            if (!nonZeroLiterals.TryGetValue(v, out BoolVar literal))
            {
                literal = model.NewBoolVar($"{v.Name()}_nonzero");
                model.Add(v != 0).OnlyEnforceIf(literal);
                model.Add(v == 0).OnlyEnforceIf(literal.Not());
                nonZeroLiterals[v] = literal;
            }

            return literal;
        }

        for (int w = 0; w < words.Count; w++)
        {
            string word = words[w];
            for (int i = 1; i < word.Length; i++)
            {
                if (word[i] != '*')
                {
                    continue;
                }

                var literals = new List<ILiteral> { NonZero(wordVarsList[w][i]) };
                for (int j = 0; j < i; j++)
                {
                    literals.Add(NonZero(wordVarsList[w][j]));
                }

                model.AddBoolOr(literals);
            }
        }

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

        long MaxValue(int length) => (long)Math.Pow(10, length) - 1;

        List<IntVar> multiplicandVars = wordVarsList[0];
        List<IntVar> multiplierVars = wordVarsList[1];
        List<List<IntVar>> partialProductVarsList = wordVarsList.Skip(2).Take(partialProducts.Length).ToList();
        List<IntVar> productVars = wordVarsList[^1];

        IntVar multiplicandValue = model.NewIntVar(0, MaxValue(multiplicand.Length), "multiplicandValue");
        model.Add(multiplicandValue == BuildWordExpr(multiplicandVars));

        // One partial product per digit of the multiplier, counting from the right.
        LinearExpr productTotal = null;
        for (int i = 0; i < partialProducts.Length; i++)
        {
            IntVar multiplierDigit = multiplierVars[^(i + 1)];

            IntVar partialValue = model.NewIntVar(0, MaxValue(partialProducts[i].Length), $"partialValue{i}");
            model.Add(partialValue == BuildWordExpr(partialProductVarsList[i]));

            model.AddMultiplicationEquality(partialValue, new[] { multiplicandValue, multiplierDigit });

            long shift = (long)Math.Pow(10, i);
            LinearExpr shiftedExpr = LinearExpr.WeightedSum(new[] { partialValue }, new[] { shift });
            productTotal = productTotal is null ? shiftedExpr : LinearExpr.Sum(new[] { productTotal, shiftedExpr });
        }

        model.Add(productTotal == BuildWordExpr(productVars));

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

        var values = new long[2 + partialProducts.Length + 1];
        values[0] = WordValue(multiplicandVars);
        values[1] = WordValue(multiplierVars);
        for (int i = 0; i < partialProducts.Length; i++)
        {
            values[2 + i] = WordValue(partialProductVarsList[i]);
        }
        values[^1] = WordValue(productVars);

        foreach (long value in values)
        {
            if (value < 0)
            {
                throw new InvalidOperationException($"Solved value is negative: {value}.");
            }
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
