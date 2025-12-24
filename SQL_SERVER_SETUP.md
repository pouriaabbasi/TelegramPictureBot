# SQL Server Setup Complete ✅

## Database Configuration

- **Server:** MPC10002154
- **Database:** TelegramPhotoBotDb
- **Authentication:** Windows Authentication (Integrated Security)
- **Connection String:** `Server=MPC10002154;Database=TelegramPhotoBotDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

## Migration Status

✅ **Initial migration created and applied successfully!**

The database has been created with the following tables:

### Tables Created:
1. **Users** - Telegram users
2. **Roles** - User roles
3. **UserRoles** - User-role relationships
4. **Photos** - Available content
5. **SubscriptionPlans** - Available subscription plans
6. **Subscriptions** - User subscriptions
7. **Purchases** - Base table for all purchases
8. **PurchasePhotos** - Individual photo purchases
9. **PurchaseSubscriptions** - Subscription purchases
10. **__EFMigrationsHistory** - EF Core migration tracking

### Indexes Created:
- `IX_Users_TelegramUserId` (Unique) - Fast user lookup by Telegram ID
- `IX_Purchases_TelegramPaymentId` (Unique) - Prevents duplicate payment processing
- `IX_Purchases_UserId` - Fast purchase queries
- `IX_Subscriptions_UserId` - Fast subscription queries
- And many more for optimal query performance

## Verify Database

You can verify the database was created by:

1. **SQL Server Management Studio (SSMS):**
   - Connect to server: `MPC10002154`
   - Database: `TelegramPhotoBotDb` should be visible
   - Expand Tables to see all created tables

2. **Or using SQL query:**
   ```sql
   USE TelegramPhotoBotDb;
   SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
   ```

## Running the Application

The application is now configured to use SQL Server. Simply run:

```bash
dotnet run --project TelegramPhotoBot.Presentation
```

The application will:
- ✅ Connect to SQL Server on MPC10002154
- ✅ Use Windows Authentication
- ✅ Seed test data automatically on first run
- ✅ Be ready for testing

## Future Migrations

When you make changes to the domain model:

1. **Create a new migration:**
   ```bash
   dotnet ef migrations add MigrationName --project TelegramPhotoBot.Infrastructure --startup-project TelegramPhotoBot.Presentation
   ```

2. **Apply the migration:**
   ```bash
   dotnet ef database update --project TelegramPhotoBot.Infrastructure --startup-project TelegramPhotoBot.Presentation
   ```

## Connection String Location

The connection string is in:
- `TelegramPhotoBot.Presentation/appsettings.json`

You can update it there if the server name or database name changes.

## Notes

- ✅ Windows Authentication is used (no username/password needed)
- ✅ `TrustServerCertificate=True` is set for development
- ✅ `MultipleActiveResultSets=true` for better performance
- ✅ All migrations are tracked in `__EFMigrationsHistory` table

