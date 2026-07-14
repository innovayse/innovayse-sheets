using System.Collections.Generic;

namespace Innovayse.Sheets.Formulas;

public class DependencyGraph
{
    // dependency[cell] = set of cells that `cell`'s formula reads from
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> _dependsOn = new();
    // dependents[cell] = set of cells whose formulas read from `cell`
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> _dependents = new();

    public void AddCell(CellAddress cell, AstNode formula)
    {
        if (_dependsOn.TryGetValue(cell, out var previouslyReferenced))
        {
            foreach (var oldDep in previouslyReferenced)
            {
                if (_dependents.TryGetValue(oldDep, out var oldSet))
                {
                    oldSet.Remove(cell);
                }
            }
        }

        var referenced = new HashSet<CellAddress>();
        CollectReferences(formula, referenced);

        _dependsOn[cell] = referenced;
        foreach (var dep in referenced)
        {
            if (!_dependents.TryGetValue(dep, out var set))
            {
                set = new HashSet<CellAddress>();
                _dependents[dep] = set;
            }
            set.Add(cell);
        }
    }

    public IReadOnlyList<CellAddress> GetDependents(CellAddress cell) =>
        _dependents.TryGetValue(cell, out var set) ? new List<CellAddress>(set) : new List<CellAddress>();

    public bool HasCycle(CellAddress start)
    {
        var visiting = new HashSet<CellAddress>();
        var visited = new HashSet<CellAddress>();
        return Visit(start, visiting, visited);
    }

    private bool Visit(CellAddress cell, HashSet<CellAddress> visiting, HashSet<CellAddress> visited)
    {
        if (visited.Contains(cell)) return false;
        if (visiting.Contains(cell)) return true;

        visiting.Add(cell);
        if (_dependsOn.TryGetValue(cell, out var deps))
        {
            foreach (var dep in deps)
            {
                if (Visit(dep, visiting, visited)) return true;
            }
        }
        visiting.Remove(cell);
        visited.Add(cell);
        return false;
    }

    private static void CollectReferences(AstNode node, HashSet<CellAddress> into)
    {
        switch (node)
        {
            case CellRefNode c:
                into.Add(CellAddress.Parse(c.Reference));
                break;
            case RangeRefNode r:
                var (start, end) = CellAddress.ParseRange(r.Reference);
                for (int row = start.Row; row <= end.Row; row++)
                for (int col = start.Col; col <= end.Col; col++)
                    into.Add(new CellAddress(row, col));
                break;
            case BinaryOpNode b:
                CollectReferences(b.Left, into);
                CollectReferences(b.Right, into);
                break;
            case FunctionCallNode f:
                foreach (var arg in f.Args) CollectReferences(arg, into);
                break;
        }
    }
}
