using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Data;

public class SheetsDbContext : DbContext
{
    public SheetsDbContext(DbContextOptions<SheetsDbContext> options) : base(options) { }

    public DbSet<Spreadsheet> Spreadsheets => Set<Spreadsheet>();
    public DbSet<Sheet> Sheets => Set<Sheet>();
    public DbSet<Cell> Cells => Set<Cell>();
    public DbSet<SpreadsheetShare> Shares => Set<SpreadsheetShare>();
    public DbSet<SharingLink> Links => Set<SharingLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cell>()
            .HasIndex(c => new { c.SheetId, c.Row, c.Col })
            .IsUnique();

        modelBuilder.Entity<Sheet>()
            .HasIndex(s => s.SpreadsheetId);

        modelBuilder.Entity<Sheet>()
            .HasOne<Spreadsheet>()
            .WithMany()
            .HasForeignKey(s => s.SpreadsheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cell>()
            .HasOne<Sheet>()
            .WithMany()
            .HasForeignKey(c => c.SheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SpreadsheetShare>()
            .HasIndex(s => new { s.SpreadsheetId, s.UserId })
            .IsUnique();

        modelBuilder.Entity<SpreadsheetShare>()
            .HasOne<Spreadsheet>()
            .WithMany()
            .HasForeignKey(s => s.SpreadsheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharingLink>()
            .HasIndex(l => l.Token)
            .IsUnique();

        modelBuilder.Entity<SharingLink>()
            .HasOne<Spreadsheet>()
            .WithMany()
            .HasForeignKey(l => l.SpreadsheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
