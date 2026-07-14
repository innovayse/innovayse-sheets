// tests/Innovayse.Sheets.Formulas.Tests/ParserTests.cs
using System.Collections.Generic;
using Innovayse.Sheets.Formulas;
using Xunit;

public class ParserTests
{
    [Fact]
    public void Parse_NumberLiteral_ReturnsNumberNode()
    {
        var ast = Parser.Parse("42");
        Assert.Equal(new NumberNode(42), ast);
    }

    [Fact]
    public void Parse_CellReference_ReturnsCellRefNode()
    {
        var ast = Parser.Parse("A1");
        Assert.Equal(new CellRefNode("A1"), ast);
    }

    [Fact]
    public void Parse_Addition_ReturnsBinaryOpNode()
    {
        var ast = Parser.Parse("A1+B2");
        Assert.Equal(new BinaryOpNode("+", new CellRefNode("A1"), new CellRefNode("B2")), ast);
    }

    [Fact]
    public void Parse_RespectsOperatorPrecedence()
    {
        // 1+2*3 should parse as 1+(2*3), not (1+2)*3
        var ast = Parser.Parse("1+2*3");
        var expected = new BinaryOpNode("+",
            new NumberNode(1),
            new BinaryOpNode("*", new NumberNode(2), new NumberNode(3)));
        Assert.Equal(expected, ast);
    }

    [Fact]
    public void Parse_FunctionCallWithRangeArg_ReturnsFunctionCallNode()
    {
        var ast = Parser.Parse("SUM(B1:B10)");
        var expected = new FunctionCallNode("SUM", new List<AstNode> { new RangeRefNode("B1:B10") });
        Assert.Equal(expected, ast);
    }

    [Fact]
    public void Parse_FunctionCallWithMultipleArgs_ReturnsAllArgs()
    {
        var ast = Parser.Parse("IF(A1,B1,C1)");
        var expected = new FunctionCallNode("IF", new List<AstNode>
        {
            new CellRefNode("A1"), new CellRefNode("B1"), new CellRefNode("C1")
        });
        Assert.Equal(expected, ast);
    }
}
