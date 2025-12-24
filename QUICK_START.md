# Quick Start - Run Locally in 5 Minutes

## Fastest Way to Test

### 1. Update Configuration (2 minutes)

Edit `TelegramPhotoBot.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=test.db"
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "MtProto": {
      "ApiId": "12345",
      "ApiHash": "your_api_hash",
      "PhoneNumber": "+1234567890"
    }
  }
}
```

### 2. Add SQLite Support (1 minute)

```bash
dotnet add TelegramPhotoBot.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
```

Update `ServiceCollectionExtensions.cs`:
```csharp
// Change this line:
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// To this (for SQLite):
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
```

### 3. Run (1 minute)

```bash
cd TelegramPhotoBot.Presentation
dotnet run
```

### 4. Test in Telegram (1 minute)

1. Open Telegram
2. Find your bot
3. Send `/start`
4. Check console for logs

---

## What You'll See

When you run `dotnet run`, you should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Application started. Press Ctrl+C to shut down.
```

The database will be created automatically when the app starts.

---

## Minimal Configuration for Testing

If you just want to test the application structure without Telegram:

1. **Use in-memory database** (no SQL Server needed):

Update `ServiceCollectionExtensions.cs`:
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

2. **Skip Telegram configuration** (comment out the checks):

Temporarily modify `ServiceCollectionExtensions.cs`:
```csharp
// Comment out these lines for testing without Telegram
// var botToken = configuration["Telegram:BotToken"] ?? throw new InvalidOperationException("Telegram:BotToken is required");
// services.AddScoped<ITelegramBotService>(sp => new TelegramBotService(botToken));
```

3. **Run the application**:
```bash
dotnet run --project TelegramPhotoBot.Presentation
```

---

## Verify It Works

After running, check:

1. **No errors in console** ✅
2. **Database file created** (if using SQLite: `test.db` file exists) ✅
3. **Application is listening** on a port ✅

---

## Next: Add Test Data

Once running, you can add test data using the database or create a simple seeder.

