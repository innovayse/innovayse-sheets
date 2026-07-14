using Innovayse.Sheets.Formulas;

namespace Innovayse.Sheets.API.Cells;

public class DbCellValueLookup : ICellValueLookup
{
    private readonly Dictionary<CellAddress, string> _rawValues;
    private readonly Dictionary<CellAddress, FormulaResult> _cache = new();
    private readonly HashSet<CellAddress> _evaluating = new();

    public DbCellValueLookup(Dictionary<CellAddress, string> rawValues)
    {
        _rawValues = rawValues;
    }

    public FormulaResult GetValue(CellAddress address)
    {
        if (_cache.TryGetValue(address, out var cached)) return cached;

        if (!_rawValues.TryGetValue(address, out var raw) || raw.Length == 0)
            return FormulaResult.Ok(0);

        if (_evaluating.Contains(address))
            return FormulaResult.Err(new FormulaError(FormulaErrorType.Circular));

        _evaluating.Add(address);

        FormulaResult result;
        if (raw.StartsWith('='))
        {
            var ast = Parser.Parse(raw[1..]);
            result = Evaluator.Evaluate(ast, this);
        }
        else if (double.TryParse(raw, out var literal))
        {
            result = FormulaResult.Ok(literal);
        }
        else
        {
            result = FormulaResult.Err(new FormulaError(FormulaErrorType.InvalidValue));
        }

        _evaluating.Remove(address);
        _cache[address] = result;
        return result;
    }
}
