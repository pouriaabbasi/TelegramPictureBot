# How to Run on Your Computer - Simple Guide

## ✅ Yes, you can run it on your computer!

The application is ready to run locally. Here's the simplest way:

## Quick Start (3 Steps)

### Step 1: Update Configuration

Edit `TelegramPhotoBot.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=telegramphotobot.db"
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

**Note:** For basic testing, you can use placeholder values for Telegram settings. The app will start but Telegram features won't work until you add real credentials.

### Step 2: Run the Application

Open PowerShell or Command Prompt in the project folder and run:

```bash
cd TelegramPhotoBot.Presentation
dotnet run
```

### Step 3: Verify It Works

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Application started. Press Ctrl+C to shut down.
✅ Test data seeded successfully!
```

**Success!** The application is running. The database (`telegramphotobot.db`) will be created automatically.

---

## What Happens When You Run

1. ✅ **Database is created** - SQLite database file `telegramphotobot.db` is created
2. ✅ **Test data is seeded** - Admin user, subscription plan, and 3 test photos are added
3. ✅ **Application starts** - Web server starts listening on a port
4. ✅ **Ready for testing** - You can now test the application

---

## Testing Without Telegram (Structure Testing)

If you just want to test the application structure:

1. **Run the application** (it will start successfully)
2. **Check the database** - `telegramphotobot.db` file is created
3. **Verify test data** - Admin user and photos are seeded

The Telegram bot features require actual Telegram credentials, but the core application structure works.

---

## Testing With Telegram (Full Testing)

To test with actual Telegram:

1. **Get Bot Token:**
   - Go to [@BotFather](https://t.me/BotFather) on Telegram
   - Send `/newbot` and follow instructions
   - Copy the bot token

2. **Get API Credentials:**
   - Go to https://my.telegram.org
   - Create an application
   - Get `api_id` and `api_hash`

3. **Update appsettings.json** with real values

4. **Run the application**

5. **Test in Telegram:**
   - Find your bot
   - Send `/start`
   - Check console for logs

---

## Troubleshooting

### "Cannot find dotnet"
- Install .NET 8.0 SDK from https://dotnet.microsoft.com/download

### "Port already in use"
- The app will use a different port automatically
- Or kill the process using the port

### "Database errors"
- Delete `telegramphotobot.db` file and run again
- The database will be recreated

---

## Next Steps

1. ✅ **Application runs** - You've verified the structure works
2. ⏭️ **Add Telegram Bot API** - Implement actual bot functionality
3. ⏭️ **Add MTProto** - Implement content delivery
4. ⏭️ **Test payment flows** - Test with real Telegram Stars

---

## Files Created When Running

- `telegramphotobot.db` - SQLite database file
- `bin/` and `obj/` folders - Build output (can be ignored)

---

## Summary

**Yes, you can absolutely run this on your computer!**

The application is configured to:
- ✅ Use SQLite (no SQL Server needed)
- ✅ Auto-create database
- ✅ Seed test data
- ✅ Start web server

Just run `dotnet run` in the `TelegramPhotoBot.Presentation` folder and you're good to go! 🚀

