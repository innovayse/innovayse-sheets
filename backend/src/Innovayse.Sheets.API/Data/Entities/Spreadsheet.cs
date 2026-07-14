namespace Innovayse.Sheets.API.Data.Entities;

public class Spreadsheet
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
