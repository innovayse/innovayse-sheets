using System;
using System.Collections.Generic;
using System.Text;

namespace Innovayse.Sheets.Formulas;

public enum TokenType { Number, CellRef, RangeRef, Identifier, Operator, LParen, RParen, Comma, End }

public readonly record struct Token(TokenType Type, string Text);

public static class Tokenizer
{
    public static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c is '+' or '-' or '*' or '/' or '=' or '<' or '>')
            {
                tokens.Add(new Token(TokenType.Operator, c.ToString()));
                i++;
                continue;
            }

            if (c == '(') { tokens.Add(new Token(TokenType.LParen, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RParen, ")")); i++; continue; }
            if (c == ',') { tokens.Add(new Token(TokenType.Comma, ",")); i++; continue; }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++;
                tokens.Add(new Token(TokenType.Number, input[start..i]));
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < input.Length && char.IsLetter(input[i])) i++;

                // A cell/range reference is letters immediately followed by digits (A1, B10).
                // A bare identifier (SUM, IF) is letters with no digits following.
                if (i < input.Length && char.IsDigit(input[i]))
                {
                    while (i < input.Length && char.IsDigit(input[i])) i++;

                    if (i < input.Length && input[i] == ':')
                    {
                        int rangeStart = start;
                        i++; // consume ':'
                        int secondStart = i;
                        while (i < input.Length && char.IsLetter(input[i])) i++;
                        while (i < input.Length && char.IsDigit(input[i])) i++;
                        if (i == secondStart)
                            throw new FormatException($"Invalid range reference at position {rangeStart}");
                        tokens.Add(new Token(TokenType.RangeRef, input[rangeStart..i]));
                        continue;
                    }

                    tokens.Add(new Token(TokenType.CellRef, input[start..i]));
                    continue;
                }

                tokens.Add(new Token(TokenType.Identifier, input[start..i]));
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' at position {i}");
        }

        tokens.Add(new Token(TokenType.End, ""));
        return tokens;
    }
}
