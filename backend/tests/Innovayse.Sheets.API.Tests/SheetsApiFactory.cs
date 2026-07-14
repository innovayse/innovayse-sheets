using Innovayse.Sheets.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class SheetsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SheetsDbContext>>();

            // Give the in-memory provider its own internal service provider. Program.cs's
            // AddDbContext(UseNpgsql) call left Npgsql's provider services registered in this
            // service collection (RemoveAll<DbContextOptions<T>>() only removes the options
            // descriptor, not the provider services), and without an explicit internal
            // provider EF Core resolves provider services from the shared app container,
            // where both Npgsql and InMemory would then be visible together and throw
            // "Only a single database provider can be registered". Isolating the in-memory
            // provider's services here also keeps its store-cache singleton stable across
            // DbContext instances so writes made in one request are visible in the next.
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            var databaseName = $"sheets-tests-{Guid.NewGuid()}";
            services.AddDbContext<SheetsDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.RemoveAll<Innovayse.Sheets.API.Users.ISsoUserDirectory>();
            var fakeDirectory = new FakeSsoUserDirectory();
            services.AddSingleton<Innovayse.Sheets.API.Users.ISsoUserDirectory>(fakeDirectory);
            services.AddSingleton(fakeDirectory); // exposed so tests can call .Register(...) on the same instance
        });
    }

    protected override void ConfigureClient(System.Net.Http.HttpClient client)
    {
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test");
        base.ConfigureClient(client);
    }
}
