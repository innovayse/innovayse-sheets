namespace Innovayse.Sheets.API.Data.Entities;

public class Sheet
{
    public Guid Id { get; set; }
    public Guid SpreadsheetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
