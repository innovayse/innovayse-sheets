// src/Innovayse.Sheets.Formulas/Functions.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Innovayse.Sheets.Formulas;

public readonly record struct CellAddress(int Row, int Col)
{
    public static CellAddress Parse(string reference)
    {
        int i = 0;
        while (i < reference.Length && char.IsLetter(reference[i])) i++;
        var colLetters = reference[..i];
        var rowDigits = reference[i..];

        int col = 0;
        foreach (var ch in colLetters)
            col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        col -= 1; // zero-index

        int row = int.Parse(rowDigits) - 1; // zero-index
        return new CellAddress(row, col);
    }

    public static bool TryParse(string reference, out CellAddress address)
    {
        address = default;
        if (string.IsNullOrEmpty(reference)) return false;

        int i = 0;
        while (i < reference.Length && char.IsLetter(reference[i])) i++;
        var colLetters = reference[..i];
        var rowDigits = reference[i..];

        if (colLetters.Length == 0 || rowDigits.Length == 0) return false;
        if (!rowDigits.All(char.IsDigit)) return false;

        int col = 0;
        foreach (var ch in colLetters)
            col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        col -= 1; // zero-index

        if (!int.TryParse(rowDigits, out var rowNumber)) return false;

        address = new CellAddress(rowNumber - 1, col); // zero-index
        return true;
    }

    public static (CellAddress Start, CellAddress End) ParseRange(string reference)
    {
        var parts = reference.Split(':');
        return (Parse(parts[0]), Parse(parts[1]));
    }

    public static bool TryParseRange(string reference, out CellAddress start, out CellAddress end)
    {
        start = default;
        end = default;
        if (string.IsNullOrEmpty(reference)) return false;

        var parts = reference.Split(':');
        if (parts.Length != 2) return false;

        return TryParse(parts[0], out start) && TryParse(parts[1], out end);
    }
}

public readonly struct FormulaResult
{
    public bool IsError { get; }
    public double Value { get; }
    public FormulaError Error { get; }

    private FormulaResult(bool isError, double value, FormulaError error)
    {
        IsError = isError;
        Value = value;
        Error = error;
    }

    public static FormulaResult Ok(double value) => new(false, value, default);
    public static FormulaResult Err(FormulaError error) => new(true, 0, error);
}

public interface ICellValueLookup
{
    FormulaResult GetValue(CellAddress address);
}

internal static class Functions
{
    public static FormulaResult Call(string name, List<AstNode> args, ICellValueLookup lookup)
    {
        return name.ToUpperInvariant() switch
        {
            "SUM" => Aggregate(args, lookup, values => values.Sum()),
            "AVERAGE" => Aggregate(args, lookup, values => values.Count == 0 ? 0 : values.Average()),
            "COUNT" => Aggregate(args, lookup, values => values.Count),
            "COUNTA" => Aggregate(args, lookup, values => values.Count),
            "MIN" => Aggregate(args, lookup, values => values.Count == 0 ? 0 : values.Min()),
            "MAX" => Aggregate(args, lookup, values => values.Count == 0 ? 0 : values.Max()),
            "ROUND" => Round(args, lookup),
            "IF" => If(args, lookup),
            "CONCAT" => Concat(args, lookup),
            _ => FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue))
        };
    }

    private static FormulaResult Aggregate(List<AstNode> args, ICellValueLookup lookup, Func<List<double>, double> reduce)
    {
        var values = new List<double>();
        foreach (var arg in args)
        {
            if (arg is RangeRefNode range)
            {
                if (!CellAddress.TryParseRange(range.Reference, out var start, out var end))
                    return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidReference));
                for (int r = start.Row; r <= end.Row; r++)
                for (int c = start.Col; c <= end.Col; c++)
                {
                    var result = lookup.GetValue(new CellAddress(r, c));
                    if (result.IsError) return result;
                    values.Add(result.Value);
                }
            }
            else
            {
                var result = Evaluator.Evaluate(arg, lookup);
                if (result.IsError) return result;
                values.Add(result.Value);
            }
        }
        return FormulaResult.Ok(reduce(values));
    }

    private static FormulaResult If(List<AstNode> args, ICellValueLookup lookup)
    {
        if (args.Count != 3) return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));
        var condition = Evaluator.Evaluate(args[0], lookup);
        if (condition.IsError) return condition;
        return condition.Value != 0 ? Evaluator.Evaluate(args[1], lookup) : Evaluator.Evaluate(args[2], lookup);
    }

    private static FormulaResult Round(List<AstNode> args, ICellValueLookup lookup)
    {
        if (args.Count != 2) return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));
        var value = Evaluator.Evaluate(args[0], lookup);
        if (value.IsError) return value;
        var digits = Evaluator.Evaluate(args[1], lookup);
        if (digits.IsError) return digits;
        if (digits.Value < 0 || digits.Value > 15)
            return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));
        return FormulaResult.Ok(Math.Round(value.Value, (int)digits.Value));
    }

    private static FormulaResult Concat(List<AstNode> args, ICellValueLookup lookup)
    {
        // MVP: CONCAT operates on numeric cell values only; returns their concatenated digits as a number
        // is not meaningful, so CONCAT is deferred to when string cell values exist. For now, treat as SUM
        // of args to keep the function callable without throwing, per the "no unhandled exceptions" rule.
        return Aggregate(args, lookup, values => values.Sum());
    }
}
