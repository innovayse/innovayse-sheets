// src/Innovayse.Sheets.Formulas/Parser.cs
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Innovayse.Sheets.Formulas;

public static class Parser
{
    public static AstNode Parse(string formulaBody)
    {
        var tokens = Tokenizer.Tokenize(formulaBody);
        int pos = 0;
        var result = ParseExpression(tokens, ref pos, 0);

        if (tokens[pos].Type != TokenType.End)
            throw new FormatException($"Unexpected token '{tokens[pos].Text}' after end of expression");

        return result;
    }

    private static int Precedence(string op) => op switch
    {
        "+" or "-" => 1,
        "*" or "/" => 2,
        _ => 0
    };

    private static AstNode ParseExpression(List<Token> tokens, ref int pos, int minPrecedence)
    {
        var left = ParsePrimary(tokens, ref pos);

        while (tokens[pos].Type == TokenType.Operator && Precedence(tokens[pos].Text) >= minPrecedence && Precedence(tokens[pos].Text) > 0)
        {
            var op = tokens[pos].Text;
            pos++;
            var right = ParseExpression(tokens, ref pos, Precedence(op) + 1);
            left = new BinaryOpNode(op, left, right);
        }

        return left;
    }

    private static AstNode ParsePrimary(List<Token> tokens, ref int pos)
    {
        var token = tokens[pos];

        switch (token.Type)
        {
            case TokenType.Number:
                pos++;
                return new NumberNode(double.Parse(token.Text, CultureInfo.InvariantCulture));

            case TokenType.CellRef:
                pos++;
                return new CellRefNode(token.Text);

            case TokenType.RangeRef:
                pos++;
                return new RangeRefNode(token.Text);

            case TokenType.Identifier:
                pos++;
                if (tokens[pos].Type != TokenType.LParen)
                    throw new FormatException($"Expected '(' after function name '{token.Text}'");
                pos++; // consume '('
                var args = new List<AstNode>();
                if (tokens[pos].Type != TokenType.RParen)
                {
                    args.Add(ParseExpression(tokens, ref pos, 0));
                    while (tokens[pos].Type == TokenType.Comma)
                    {
                        pos++;
                        args.Add(ParseExpression(tokens, ref pos, 0));
                    }
                }
                if (tokens[pos].Type != TokenType.RParen)
                    throw new FormatException($"Expected ')' to close call to '{token.Text}'");
                pos++; // consume ')'
                return new FunctionCallNode(token.Text, args);

            case TokenType.LParen:
                pos++;
                var inner = ParseExpression(tokens, ref pos, 0);
                if (tokens[pos].Type != TokenType.RParen)
                    throw new FormatException("Expected ')' to close parenthesized expression");
                pos++;
                return inner;

            default:
                throw new FormatException($"Unexpected token '{token.Text}'");
        }
    }
}
