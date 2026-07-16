using JeoAnoBa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Data;

public class JeopardyDbContext : DbContext
{
    public JeopardyDbContext(DbContextOptions<JeopardyDbContext> options)
        : base(options) { }

    public DbSet<GameDb> Games => Set<GameDb>();
    public DbSet<CategoryDb> Categories => Set<CategoryDb>();
    public DbSet<ClueDb> Clues => Set<ClueDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameDb>()
            .HasMany(g => g.Categories)
            .WithOne(c => c.Game)
            .HasForeignKey(c => c.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategoryDb>()
            .HasMany(c => c.Clues)
            .WithOne(cl => cl.Category)
            .HasForeignKey(cl => cl.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
