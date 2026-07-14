using Innovayse.Sheets.Formulas;
using Xunit;

public class DependencyGraphTests
{
    [Fact]
    public void HasCycle_NoCycle_ReturnsFalse()
    {
        var graph = new DependencyGraph();
        // A1 = B1 + 1, B1 = 5 (no formula on B1, so no dependency to add)
        graph.AddCell(CellAddress.Parse("A1"), Parser.Parse("B1+1"));

        Assert.False(graph.HasCycle(CellAddress.Parse("A1")));
    }

    [Fact]
    public void HasCycle_DirectCycle_ReturnsTrue()
    {
        var graph = new DependencyGraph();
        graph.AddCell(CellAddress.Parse("A1"), Parser.Parse("B1+1"));
        graph.AddCell(CellAddress.Parse("B1"), Parser.Parse("A1+1"));

        Assert.True(graph.HasCycle(CellAddress.Parse("A1")));
    }

    [Fact]
    public void HasCycle_IndirectCycle_ReturnsTrue()
    {
        var graph = new DependencyGraph();
        graph.AddCell(CellAddress.Parse("A1"), Parser.Parse("B1+1"));
        graph.AddCell(CellAddress.Parse("B1"), Parser.Parse("C1+1"));
        graph.AddCell(CellAddress.Parse("C1"), Parser.Parse("A1+1"));

        Assert.True(graph.HasCycle(CellAddress.Parse("A1")));
    }

    [Fact]
    public void GetDependents_RangeReference_IncludesCellsInRange()
    {
        var graph = new DependencyGraph();
        graph.AddCell(CellAddress.Parse("A1"), Parser.Parse("SUM(B1:B2)"));

        var dependents = graph.GetDependents(CellAddress.Parse("B1"));

        Assert.Contains(CellAddress.Parse("A1"), dependents);
    }

    [Fact]
    public void AddCell_ReAddWithDifferentFormula_RemovesStaleDependentEdges()
    {
        var graph = new DependencyGraph();
        var a1 = CellAddress.Parse("A1");
        var b1 = CellAddress.Parse("B1");
        var c1 = CellAddress.Parse("C1");

        graph.AddCell(a1, Parser.Parse("B1+1"));
        Assert.Contains(a1, graph.GetDependents(b1));

        graph.AddCell(a1, Parser.Parse("C1+1"));

        Assert.DoesNotContain(a1, graph.GetDependents(b1));
        Assert.Contains(a1, graph.GetDependents(c1));
    }
}
