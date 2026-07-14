namespace Innovayse.Sheets.API.Cells;

public record CellDto(int Row, int Col, string RawValue, double? ComputedValue, string? TextValue, string? Error, string? FormatJson);
public record CellWriteRequest(int Row, int Col, string RawValue, string? FormatJson);
public record BatchCellWriteRequest(List<CellWriteRequest> Cells);
