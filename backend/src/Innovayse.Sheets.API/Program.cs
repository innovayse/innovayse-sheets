using Innovayse.Sheets.API.Cells;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Sharing;
using Innovayse.Sheets.API.Spreadsheets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SheetsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SheetsDb")));

builder.Services.AddScoped<Innovayse.Sheets.API.Access.ISpreadsheetAccessService, Innovayse.Sheets.API.Access.SpreadsheetAccessService>();

builder.Services.AddHttpClient<Innovayse.Sheets.API.Users.ISsoUserDirectory, Innovayse.Sheets.API.Users.HttpSsoUserDirectory>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Sso:Authority"] ?? throw new InvalidOperationException("Sso:Authority is not configured"));
    var serviceApiKey = builder.Configuration["ServiceAuth:ApiKey"] ?? throw new InvalidOperationException("ServiceAuth:ApiKey is not configured");
    client.DefaultRequestHeaders.Add("X-Service-Key", serviceApiKey);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Sso:Authority"];
        options.Audience = builder.Configuration["Sso:Audience"];
        // Local/dev SSO is served over plain HTTP (http://sso.local); relax the
        // HTTPS metadata requirement outside Production to avoid startup failures.
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        // SignalR's browser client can't set a custom Authorization header on the
        // WebSocket handshake, so it sends the token as an "access_token" query
        // param instead. JwtBearer only reads the Authorization header by default;
        // this event teaches it to also accept the query-string token, but only
        // for the hub's own path, so REST endpoints keep requiring a real header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSignalR();

// The Sheets client (innovayse-sheets/client) is always a different origin from
// this API — a browser fetch/SignalR connection would otherwise be blocked by
// the browser's CORS policy before ever reaching authentication. No credentials
// mode is needed since auth is a bearer token (REST) / query-string token
// (SignalR), never a cookie, so wildcard-free explicit origins is sufficient
// without AllowCredentials().
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapSpreadsheetEndpoints();
app.MapSheetEndpoints();
app.MapCellEndpoints();
app.MapSharesEndpoints();
app.MapLinksEndpoints();

app.MapHub<Innovayse.Sheets.API.Realtime.SheetHub>("/hubs/sheets");

app.Run();

public partial class Program { } // exposed for WebApplicationFactory in tests
