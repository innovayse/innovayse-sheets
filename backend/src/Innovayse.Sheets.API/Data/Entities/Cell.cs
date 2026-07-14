namespace Innovayse.Sheets.API.Data.Entities;

public class Cell
{
    public Guid Id { get; set; }
    public Guid SheetId { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public string? FormatJson { get; set; }
}
