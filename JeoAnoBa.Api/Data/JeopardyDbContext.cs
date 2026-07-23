using JeoAnoBa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Data;

// EF Core's bridge between your C# model classes and the actual jeopardy.db database
public class JeopardyDbContext : DbContext
{
    // Passes connection/configuration options (like which provider and connection string to use)
    // up to the base DbContext class
    public JeopardyDbContext(DbContextOptions<JeopardyDbContext> options)
        : base(options) { }

    // Each DbSet represents one table in the database — querying/adding to these
    public DbSet<GameDb> Games => Set<GameDb>();
    public DbSet<CategoryDb> Categories => Set<CategoryDb>();
    public DbSet<ClueDb> Clues => Set<ClueDb>();
    public DbSet<GameResultDb> GameResults => Set<GameResultDb>();

    // Explicitly configures relationships between tables, instead of relying entirely on EF Core's inference
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One GameDb has many CategoryDb rows, linked via CategoryDb.GameId.
        // If a game is deleted, cascade-delete all of its categories too.
        modelBuilder.Entity<GameDb>()
            .HasMany(g => g.Categories)
            .WithOne(c => c.Game)
            .HasForeignKey(c => c.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // One CategoryDb has many ClueDb rows, linked via ClueDb.CategoryId.
        // If a category is deleted, cascade-delete all of its clues too.
        modelBuilder.Entity<CategoryDb>()
            .HasMany(c => c.Clues)
            .WithOne(cl => cl.Category)
            .HasForeignKey(cl => cl.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
