using Microsoft.EntityFrameworkCore;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Infrastructure.Configurations;

namespace TelegramPhotoBot.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchasePhoto> PurchasePhotos => Set<PurchasePhoto>();
    public DbSet<PurchaseSubscription> PurchaseSubscriptions => Set<PurchaseSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Query Filters for Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(r => !r.IsDeleted);
        modelBuilder.Entity<UserRole>().HasQueryFilter(ur => !ur.IsDeleted);
        modelBuilder.Entity<Photo>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(sp => !sp.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => !p.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update UpdatedAt for entities
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.MarkAsUpdated();
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

