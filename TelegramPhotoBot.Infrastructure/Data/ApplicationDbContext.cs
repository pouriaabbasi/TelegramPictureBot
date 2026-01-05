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
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchasePhoto> PurchasePhotos => Set<PurchasePhoto>();
    
    // Marketplace entities
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelSubscription> ModelSubscriptions => Set<ModelSubscription>();
    public DbSet<DemoAccess> DemoAccesses => Set<DemoAccess>();
    public DbSet<UserState> UserStates => Set<UserState>();
    public DbSet<ViewHistory> ViewHistories => Set<ViewHistory>();
    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();
    public DbSet<MtProtoAccessToken> MtProtoAccessTokens => Set<MtProtoAccessToken>();
    public DbSet<UserContactVerification> UserContactVerifications => Set<UserContactVerification>();
    public DbSet<ModelTermsAcceptance> ModelTermsAcceptances => Set<ModelTermsAcceptance>();
    public DbSet<ModelPayout> ModelPayouts => Set<ModelPayout>();
    public DbSet<ContentNotification> ContentNotifications => Set<ContentNotification>();
    public DbSet<PendingStarPayment> PendingStarPayments => Set<PendingStarPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Query Filters for Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Photo>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Model>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<DemoAccess>().HasQueryFilter(da => !da.IsDeleted);
        modelBuilder.Entity<UserState>().HasQueryFilter(us => !us.IsDeleted);
        modelBuilder.Entity<ViewHistory>().HasQueryFilter(vh => !vh.IsDeleted);
        modelBuilder.Entity<PlatformSettings>().HasQueryFilter(ps => !ps.IsDeleted);
        modelBuilder.Entity<UserContactVerification>().HasQueryFilter(ucv => !ucv.IsDeleted);
        modelBuilder.Entity<ModelTermsAcceptance>().HasQueryFilter(mta => !mta.IsDeleted);
        modelBuilder.Entity<ModelPayout>().HasQueryFilter(mp => !mp.IsDeleted);
        modelBuilder.Entity<ContentNotification>().HasQueryFilter(cn => !cn.IsDeleted);
        modelBuilder.Entity<PendingStarPayment>().HasQueryFilter(psp => !psp.IsDeleted);
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

