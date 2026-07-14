namespace Innovayse.Sheets.Formulas;

public enum FormulaErrorType
{
    DivideByZero,
    InvalidReference,
    InvalidValue,
    Circular
}

public readonly record struct FormulaError(FormulaErrorType Type)
{
    public string Display => Type switch
    {
        FormulaErrorType.DivideByZero => "#DIV/0!",
        FormulaErrorType.InvalidReference => "#REF!",
        FormulaErrorType.InvalidValue => "#VALUE!",
        FormulaErrorType.Circular => "#CIRCULAR!",
        _ => "#ERROR!"
    };
}
