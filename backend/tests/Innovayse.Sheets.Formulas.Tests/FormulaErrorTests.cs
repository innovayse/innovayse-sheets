using Innovayse.Sheets.Formulas;
using Xunit;

public class FormulaErrorTests
{
    [Theory]
    [InlineData(FormulaErrorType.DivideByZero, "#DIV/0!")]
    [InlineData(FormulaErrorType.InvalidReference, "#REF!")]
    [InlineData(FormulaErrorType.InvalidValue, "#VALUE!")]
    [InlineData(FormulaErrorType.Circular, "#CIRCULAR!")]
    public void Display_ReturnsSpreadsheetStyleErrorCode(FormulaErrorType type, string expected)
    {
        var error = new FormulaError(type);
        Assert.Equal(expected, error.Display);
    }
}
