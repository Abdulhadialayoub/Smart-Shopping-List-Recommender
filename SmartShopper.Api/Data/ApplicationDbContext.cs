using Microsoft.EntityFrameworkCore;
using SmartShopper.Api.Models;

namespace SmartShopper.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<FridgeItem> FridgeItems { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingItem> ShoppingItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.TelegramChatId).HasMaxLength(50);
            entity.Property(e => e.TelegramUsername).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // FridgeItem configuration
        modelBuilder.Entity<FridgeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.AddedDate).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ShoppingList configuration
        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(e => e.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ShoppingItem configuration
        modelBuilder.Entity<ShoppingItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(50);
        });
    }
}
