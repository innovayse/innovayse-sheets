using System.Collections.Generic;
using Innovayse.Sheets.Formulas;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_SimpleAddition_ReturnsCellRefOperatorCellRef()
    {
        var tokens = Tokenizer.Tokenize("A1+B2");

        Assert.Equal(new[]
        {
            new Token(TokenType.CellRef, "A1"),
            new Token(TokenType.Operator, "+"),
            new Token(TokenType.CellRef, "B2"),
            new Token(TokenType.End, "")
        }, tokens);
    }

    [Fact]
    public void Tokenize_FunctionCallWithRange_ReturnsIdentifierParensRange()
    {
        var tokens = Tokenizer.Tokenize("SUM(B1:B10)");

        Assert.Equal(new[]
        {
            new Token(TokenType.Identifier, "SUM"),
            new Token(TokenType.LParen, "("),
            new Token(TokenType.RangeRef, "B1:B10"),
            new Token(TokenType.RParen, ")"),
            new Token(TokenType.End, "")
        }, tokens);
    }

    [Fact]
    public void Tokenize_NumberLiteral_ReturnsNumberToken()
    {
        var tokens = Tokenizer.Tokenize("42.5");

        Assert.Equal(new[]
        {
            new Token(TokenType.Number, "42.5"),
            new Token(TokenType.End, "")
        }, tokens);
    }

    [Fact]
    public void Tokenize_FunctionWithMultipleArgs_ReturnsCommaSeparatedTokens()
    {
        var tokens = Tokenizer.Tokenize("IF(A1,B1,C1)");

        Assert.Equal(new[]
        {
            new Token(TokenType.Identifier, "IF"),
            new Token(TokenType.LParen, "("),
            new Token(TokenType.CellRef, "A1"),
            new Token(TokenType.Comma, ","),
            new Token(TokenType.CellRef, "B1"),
            new Token(TokenType.Comma, ","),
            new Token(TokenType.CellRef, "C1"),
            new Token(TokenType.RParen, ")"),
            new Token(TokenType.End, "")
        }, tokens);
    }
}
