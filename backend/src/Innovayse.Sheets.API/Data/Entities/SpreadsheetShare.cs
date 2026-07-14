namespace Innovayse.Sheets.API.Data.Entities;

public enum ShareRole
{
    View = 0,
    Edit = 1
}

public class SpreadsheetShare
{
    public Guid Id { get; set; }
    public Guid SpreadsheetId { get; set; }
    public Guid UserId { get; set; }
    public ShareRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
