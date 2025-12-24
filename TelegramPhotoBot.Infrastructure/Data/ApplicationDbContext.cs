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
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchasePhoto> PurchasePhotos => Set<PurchasePhoto>();
    public DbSet<PurchaseSubscription> PurchaseSubscriptions => Set<PurchaseSubscription>();
    
    // Marketplace entities
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelSubscription> ModelSubscriptions => Set<ModelSubscription>();
    public DbSet<DemoAccess> DemoAccesses => Set<DemoAccess>();
    public DbSet<UserState> UserStates => Set<UserState>();
    public DbSet<ViewHistory> ViewHistories => Set<ViewHistory>();
    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Query Filters for Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Photo>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(sp => !sp.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Model>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<DemoAccess>().HasQueryFilter(da => !da.IsDeleted);
        modelBuilder.Entity<UserState>().HasQueryFilter(us => !us.IsDeleted);
        modelBuilder.Entity<ViewHistory>().HasQueryFilter(vh => !vh.IsDeleted);
        modelBuilder.Entity<PlatformSettings>().HasQueryFilter(ps => !ps.IsDeleted);
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
                    // CreatedAt is set automatically in constructor
                    // No need to set it here
                }
                else if (entry.State == EntityState.Modified)
                {
                    // MarkAsUpdated is protected, but we can access it through reflection
                    // Or we can just let the entity handle it
                    // For now, we'll skip this as entities should call MarkAsUpdated themselves
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

