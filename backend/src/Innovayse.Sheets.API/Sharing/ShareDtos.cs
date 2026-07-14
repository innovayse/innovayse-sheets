namespace Innovayse.Sheets.API.Sharing;

public record ShareDto(Guid UserId, string Role);
public record CreateShareRequest(string UserIdentifier, string Role);
public record LinkDto(string Token, string Role);
public record CreateLinkRequest(string Role);
public record ClaimLinkResponse(Guid SpreadsheetId);
