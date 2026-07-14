// src/Innovayse.Sheets.Formulas/Ast.cs
using System.Collections.Generic;
using System.Linq;

namespace Innovayse.Sheets.Formulas;

public abstract record AstNode;
public record NumberNode(double Value) : AstNode;
public record CellRefNode(string Reference) : AstNode;
public record RangeRefNode(string Reference) : AstNode;
public record BinaryOpNode(string Op, AstNode Left, AstNode Right) : AstNode;

public record FunctionCallNode(string Name, List<AstNode> Args) : AstNode
{
    public virtual bool Equals(FunctionCallNode? other)
    {
        if (other is null) return false;
        return Name == other.Name && Args.SequenceEqual(other.Args);
    }

    public override int GetHashCode()
    {
        var hash = new System.HashCode();
        hash.Add(Name);
        foreach (var arg in Args) hash.Add(arg);
        return hash.ToHashCode();
    }
}
