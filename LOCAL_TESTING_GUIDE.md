# Local Testing Guide - Running Telegram Photo Bot on Your Computer

## Prerequisites

Before running the bot locally, ensure you have:

1. **.NET 8.0 SDK** installed
   - Check: `dotnet --version` (should show 8.x.x)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

2. **SQL Server** (or SQLite for simpler testing)
   - SQL Server Express (free): https://www.microsoft.com/sql-server/sql-server-downloads
   - OR SQLite (no installation needed, works with EF Core)

3. **Telegram Bot Token**
   - Create a bot via [@BotFather](https://t.me/BotFather)
   - Save the token

4. **Telegram API Credentials** (for MTProto)
   - Go to https://my.telegram.org
   - Create an application
   - Get `api_id` and `api_hash`

---

## Step 1: Configure the Application

### 1.1 Update appsettings.json

Edit `TelegramPhotoBot.Presentation/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TelegramPhotoBotDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "MtProto": {
      "ApiId": "YOUR_API_ID",
      "ApiHash": "YOUR_API_HASH",
      "PhoneNumber": "+1234567890"
    }
  }
}
```

**For SQLite (easier for testing):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=telegramphotobot.db"
  }
}
```

### 1.2 Update Database Provider (if using SQLite)

Edit `TelegramPhotoBot.Presentation/Extensions/ServiceCollectionExtensions.cs`:

```csharp
// For SQLite
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// OR for SQL Server
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

Add SQLite package if needed:
```bash
dotnet add TelegramPhotoBot.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
```

---

## Step 2: Install Required NuGet Packages

```bash
# Navigate to project root
cd D:\repos\Personal\TelegramPhotoBot

# Restore all packages
dotnet restore

# If you need Telegram Bot API (for actual implementation)
dotnet add TelegramPhotoBot.Infrastructure package Telegram.Bot

# If you need MTProto (for actual implementation)
dotnet add TelegramPhotoBot.Infrastructure package WTelegramClient
```

---

## Step 3: Create Database

### Option A: Using EF Core Migrations (Recommended)

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project TelegramPhotoBot.Infrastructure --startup-project TelegramPhotoBot.Presentation

# Apply migration to database
dotnet ef database update --project TelegramPhotoBot.Infrastructure --startup-project TelegramPhotoBot.Presentation
```

### Option B: Auto-create Database (Development Only)

The `Program.cs` already has code to auto-create the database:
```csharp
await dbContext.Database.EnsureCreatedAsync();
```

This will create the database automatically when you run the app.

---

## Step 4: Run the Application

### Option A: Using dotnet CLI

```bash
# Navigate to Presentation project
cd TelegramPhotoBot.Presentation

# Run the application
dotnet run
```

### Option B: Using Visual Studio

1. Open `TelegramPhotoBot.sln` in Visual Studio
2. Set `TelegramPhotoBot.Presentation` as startup project
3. Press F5 to run

### Option C: Using VS Code

1. Open the project folder in VS Code
2. Press F5 or use terminal: `dotnet run --project TelegramPhotoBot.Presentation`

---

## Step 5: Test the Bot

### 5.1 Basic Bot Test

1. **Start the application** (it should show it's running)
2. **Open Telegram** on your phone/desktop
3. **Find your bot** (search for the bot username you created)
4. **Send `/start`** command
5. **Check the console** - you should see logs

### 5.2 Test Commands

Try these commands in Telegram:
- `/start` - Welcome message
- `/subscriptions` - View subscription plans
- `/photos` - View available photos
- `/my_subscription` - Check your subscription

### 5.3 Test Payment Flow

1. **Create a subscription plan** (you'll need to add this via database or admin interface)
2. **Try to purchase** a subscription
3. **Complete payment** with Telegram Stars
4. **Check database** - verify purchase was recorded

---

## Step 6: Verify Database

### Using SQL Server Management Studio

1. Connect to `(localdb)\mssqllocaldb`
2. Find database `TelegramPhotoBotDb`
3. Check tables:
   - `Users` - Should have your test user
   - `Purchases` - Payment records
   - `Subscriptions` - Active subscriptions

### Using Command Line (SQLite)

```bash
# If using SQLite
sqlite3 telegramphotobot.db
.tables
SELECT * FROM Users;
```

---

## Troubleshooting

### Issue: "Cannot connect to database"

**Solution:**
- Check connection string in `appsettings.json`
- Ensure SQL Server is running (for SQL Server)
- For SQLite, ensure the path is correct

### Issue: "Telegram:BotToken is required"

**Solution:**
- Make sure `appsettings.json` has the `Telegram:BotToken` value
- Check the file is in the correct location (`TelegramPhotoBot.Presentation/appsettings.json`)

### Issue: "Port already in use"

**Solution:**
- Change the port in `Properties/launchSettings.json` or
- Kill the process using the port:
  ```bash
  # Windows
  netstat -ano | findstr :5000
  taskkill /PID <PID> /F
  ```

### Issue: "MTProto authentication failed"

**Solution:**
- This is expected if you haven't implemented MTProto yet
- The placeholder implementation will fail
- For now, focus on testing Bot API functionality

---

## Quick Test Checklist

- [ ] Application starts without errors
- [ ] Database is created
- [ ] Bot responds to `/start` command
- [ ] User is created in database when sending `/start`
- [ ] Can view subscription plans (if any exist)
- [ ] Can view photos (if any exist)
- [ ] Payment callback handler works (when payment is made)

---

## Testing Without Real Telegram (Mock Testing)

If you want to test without connecting to Telegram:

1. **Use the test project** we created:
   ```bash
   dotnet test TelegramPhotoBot.Tests
   ```

2. **Mock Telegram services** - The test project includes mock implementations

3. **Test business logic** without external dependencies

---

## Development Tips

### Enable Detailed Logging

Add to `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "TelegramPhotoBot": "Debug"
    }
  }
}
```

### Use In-Memory Database for Quick Testing

In `ServiceCollectionExtensions.cs`, temporarily use:
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

### Hot Reload

Enable hot reload for faster development:
```bash
dotnet watch run --project TelegramPhotoBot.Presentation
```

---

## Next Steps

1. **Implement Telegram Bot API** - Replace placeholder in `TelegramBotService.cs`
2. **Implement MTProto** - Replace placeholder in `MtProtoService.cs`
3. **Add more test data** - Create subscription plans and photos
4. **Test payment flows** - Verify payment verification works
5. **Test content delivery** - Verify photos are sent correctly

---

## Example: Complete Run Command

```bash
# 1. Navigate to project
cd D:\repos\Personal\TelegramPhotoBot

# 2. Restore packages
dotnet restore

# 3. Build
dotnet build

# 4. Run
dotnet run --project TelegramPhotoBot.Presentation
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Application started. Press Ctrl+C to shut down.
```

---

## Need Help?

- Check `ARCHITECTURE.md` for system design
- Check `TESTING_GUIDE.md` for unit testing
- Check `TELEGRAM_API_GUIDE.md` for API implementation

