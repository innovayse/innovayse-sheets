using System.Collections.Generic;
using Innovayse.Sheets.Formulas;
using Xunit;

public class FakeCellLookup : ICellValueLookup
{
    private readonly Dictionary<CellAddress, double> _values = new();

    public FakeCellLookup Set(string reference, double value)
    {
        _values[CellAddress.Parse(reference)] = value;
        return this;
    }

    public FormulaResult GetValue(CellAddress address) =>
        _values.TryGetValue(address, out var v) ? FormulaResult.Ok(v) : FormulaResult.Ok(0);
}

public class EvaluatorTests
{
    [Fact]
    public void Evaluate_NumberLiteral_ReturnsItsValue()
    {
        var result = Evaluator.Evaluate(Parser.Parse("42"), new FakeCellLookup());
        Assert.False(result.IsError);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Evaluate_CellReference_ResolvesFromLookup()
    {
        var lookup = new FakeCellLookup().Set("A1", 7);
        var result = Evaluator.Evaluate(Parser.Parse("A1"), lookup);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Evaluate_Addition_SumsBothSides()
    {
        var lookup = new FakeCellLookup().Set("A1", 3).Set("B1", 4);
        var result = Evaluator.Evaluate(Parser.Parse("A1+B1"), lookup);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Evaluate_DivisionByZero_ReturnsDivByZeroError()
    {
        var result = Evaluator.Evaluate(Parser.Parse("10/0"), new FakeCellLookup());
        Assert.True(result.IsError);
        Assert.Equal(FormulaErrorType.DivideByZero, result.Error.Type);
    }

    [Fact]
    public void Evaluate_SumOverRange_AddsAllCellsInRange()
    {
        var lookup = new FakeCellLookup().Set("B1", 1).Set("B2", 2).Set("B3", 3);
        var result = Evaluator.Evaluate(Parser.Parse("SUM(B1:B3)"), lookup);
        Assert.Equal(6, result.Value);
    }

    [Fact]
    public void Evaluate_Average_DividesSumByCount()
    {
        var lookup = new FakeCellLookup().Set("B1", 2).Set("B2", 4);
        var result = Evaluator.Evaluate(Parser.Parse("AVERAGE(B1:B2)"), lookup);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public void Evaluate_IfWithTruthyCondition_ReturnsTrueBranch()
    {
        var lookup = new FakeCellLookup().Set("A1", 1);
        var result = Evaluator.Evaluate(Parser.Parse("IF(A1,10,20)"), lookup);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Evaluate_IfWithFalsyCondition_ReturnsFalseBranch()
    {
        var lookup = new FakeCellLookup().Set("A1", 0);
        var result = Evaluator.Evaluate(Parser.Parse("IF(A1,10,20)"), lookup);
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void Evaluate_UnknownFunction_ReturnsValueError()
    {
        var result = Evaluator.Evaluate(Parser.Parse("NOPE(A1)"), new FakeCellLookup());
        Assert.True(result.IsError);
        Assert.Equal(FormulaErrorType.InvalidValue, result.Error.Type);
    }

    [Fact]
    public void Evaluate_MalformedCellReference_ReturnsRefErrorInsteadOfThrowing()
    {
        // The tokenizer never produces a CellRef without trailing digits, but a malformed
        // reference could still reach the evaluator (e.g. from a future parser change or
        // programmatic AST construction), so it must not throw.
        var result = Evaluator.Evaluate(new CellRefNode("A"), new FakeCellLookup());
        Assert.True(result.IsError);
        Assert.Equal(FormulaErrorType.InvalidReference, result.Error.Type);
    }

    [Fact]
    public void Evaluate_RoundWithOutOfRangeDigits_ReturnsValueErrorInsteadOfThrowing()
    {
        var result = Evaluator.Evaluate(Parser.Parse("ROUND(1.2345,20)"), new FakeCellLookup());
        Assert.True(result.IsError);
        Assert.Equal(FormulaErrorType.InvalidValue, result.Error.Type);
    }

    [Fact]
    public void Evaluate_RoundWithValidDigits_RoundsCorrectly()
    {
        var result = Evaluator.Evaluate(Parser.Parse("ROUND(1.2345,2)"), new FakeCellLookup());
        Assert.False(result.IsError);
        Assert.Equal(1.23, result.Value);
    }
}
