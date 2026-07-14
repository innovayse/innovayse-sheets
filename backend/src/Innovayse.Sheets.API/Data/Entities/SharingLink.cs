namespace Innovayse.Sheets.API.Data.Entities;

public class SharingLink
{
    public Guid Id { get; set; }
    public Guid SpreadsheetId { get; set; }
    public string Token { get; set; } = string.Empty;
    public ShareRole Role { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
