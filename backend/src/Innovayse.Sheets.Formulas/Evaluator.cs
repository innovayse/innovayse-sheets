// src/Innovayse.Sheets.Formulas/Evaluator.cs
namespace Innovayse.Sheets.Formulas;

public static class Evaluator
{
    public static FormulaResult Evaluate(AstNode node, ICellValueLookup lookup)
    {
        switch (node)
        {
            case NumberNode n:
                return FormulaResult.Ok(n.Value);

            case CellRefNode c:
                if (!CellAddress.TryParse(c.Reference, out var address))
                    return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidReference));
                return lookup.GetValue(address);

            case BinaryOpNode b:
                return EvaluateBinary(b, lookup);

            case FunctionCallNode f:
                return Functions.Call(f.Name, f.Args, lookup);

            case RangeRefNode:
                // A bare range outside a function call has no defined scalar value.
                return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));

            default:
                return FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));
        }
    }

    private static FormulaResult EvaluateBinary(BinaryOpNode node, ICellValueLookup lookup)
    {
        var left = Evaluate(node.Left, lookup);
        if (left.IsError) return left;
        var right = Evaluate(node.Right, lookup);
        if (right.IsError) return right;

        return node.Op switch
        {
            "+" => FormulaResult.Ok(left.Value + right.Value),
            "-" => FormulaResult.Ok(left.Value - right.Value),
            "*" => FormulaResult.Ok(left.Value * right.Value),
            "/" => right.Value == 0
                ? FormulaResult.Err(new FormulaError(FormulaErrorType.DivideByZero))
                : FormulaResult.Ok(left.Value / right.Value),
            _ => FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue))
        };
    }
}
