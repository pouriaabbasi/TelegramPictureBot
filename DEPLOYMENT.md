# 🚀 راهنمای استقرار (Deployment Guide)

## 📋 پیش‌نیازها

### نرم‌افزارهای مورد نیاز:
- **.NET 8 Runtime** (برای اجرای برنامه)
- **PostgreSQL** یا **SQL Server** (دیتابیس)
- **Git** (برای دریافت کد)

### اطلاعات مورد نیاز:
1. **Telegram Bot Token** - از [@BotFather](https://t.me/BotFather)
2. **MTProto Credentials** (برای ارسال محتوای محافظت شده):
   - API ID
   - API Hash
   - شماره تلفن
   از [https://my.telegram.org/apps](https://my.telegram.org/apps)
3. **Connection String دیتابیس**
4. **Admin Telegram ID** - آیدی عددی ادمین

---

## 📦 مرحله 1: دریافت فایل‌های Publish شده

### گزینه A: استفاده از فایل‌های Publish موجود
فایل‌های آماده در پوشه `publish/` موجود هستند.

### گزینه B: Build از کد منبع
```bash
git clone https://github.com/pouriaabbasi/TelegramPictureBot.git
cd TelegramPictureBot
dotnet publish TelegramPhotoBot.Presentation/TelegramPhotoBot.Presentation.csproj -c Release -o ./publish
```

---

## ⚙️ مرحله 2: پیکربندی (Configuration)

### 1. فایل `appsettings.json` را ویرایش کنید:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=telegram_photo_bot;Username=your_user;Password=your_password"
  },
  "TelegramBot": {
    "BotToken": "YOUR_BOT_TOKEN_FROM_BOTFATHER",
    "BotUsername": "YourBotUsername",
    "WebhookUrl": "https://yourdomain.com/api/telegram/webhook"
  },
  "MTProto": {
    "ApiId": 0,
    "ApiHash": "",
    "PhoneNumber": "",
    "SessionPath": "./mtproto_session"
  },
  "AdminSettings": {
    "AdminTelegramIds": "123456789,987654321"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 2. تنظیمات محیط (Environment Variables)
به جای استفاده از `appsettings.json`، می‌توانید از متغیرهای محیطی استفاده کنید:

```bash
export ConnectionStrings__DefaultConnection="Host=..."
export TelegramBot__BotToken="YOUR_TOKEN"
export TelegramBot__WebhookUrl="https://..."
export MTProto__ApiId="12345"
export MTProto__ApiHash="abc123..."
export AdminSettings__AdminTelegramIds="123456789"
```

---

## 🗄️ مرحله 3: راه‌اندازی دیتابیس

### 1. ساخت دیتابیس:
```sql
CREATE DATABASE telegram_photo_bot;
```

### 2. اجرای Migration:
```bash
cd publish
dotnet TelegramPhotoBot.Presentation.dll
```
در اولین اجرا، جداول به صورت خودکار ساخته می‌شوند (اگر `AutoMigrate` فعال باشد).

یا اجرای دستی Migration:
```bash
cd TelegramPhotoBot.Infrastructure
dotnet ef database update --startup-project ../TelegramPhotoBot.Presentation
```

---

## 🚀 مرحله 4: اجرای برنامه

### روش 1: اجرای مستقیم
```bash
cd publish
dotnet TelegramPhotoBot.Presentation.dll
```

### روش 2: استفاده از systemd (لینوکس)
ساخت فایل service:

```bash
sudo nano /etc/systemd/system/telegram-photo-bot.service
```

محتوای فایل:
```ini
[Unit]
Description=Telegram Photo Bot
After=network.target

[Service]
WorkingDirectory=/path/to/publish
ExecStart=/usr/bin/dotnet /path/to/publish/TelegramPhotoBot.Presentation.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=telegram-photo-bot
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

فعال‌سازی و اجرا:
```bash
sudo systemctl daemon-reload
sudo systemctl enable telegram-photo-bot
sudo systemctl start telegram-photo-bot
sudo systemctl status telegram-photo-bot
```

مشاهده لاگ‌ها:
```bash
sudo journalctl -u telegram-photo-bot -f
```

### روش 3: استفاده از Docker (توصیه می‌شود)

ساخت `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "TelegramPhotoBot.Presentation.dll"]
```

اجرا:
```bash
docker build -t telegram-photo-bot .
docker run -d \
  --name telegram-bot \
  -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=..." \
  -e TelegramBot__BotToken="YOUR_TOKEN" \
  --restart unless-stopped \
  telegram-photo-bot
```

---

## 🔐 مرحله 5: راه‌اندازی MTProto (ارسال محتوای محافظت شده)

1. پس از اجرای برنامه، به بات خود بروید
2. به عنوان ادمین وارد شوید
3. از منوی ادمین، گزینه **"MTProto Setup"** را انتخاب کنید
4. مراحل احراز هویت را دنبال کنید:
   - وارد کردن API ID
   - وارد کردن API Hash
   - وارد کردن شماره تلفن
   - وارد کردن کد تایید ارسال شده به تلگرام
   - اگر نیاز بود، رمز دو مرحله‌ای (2FA)

---

## 🌐 مرحله 6: تنظیم Webhook

### روش A: استفاده از Nginx (توصیه می‌شود)

نصب Certbot برای SSL:
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
```

تنظیم Nginx:
```nginx
server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### روش B: ثبت Webhook مستقیم
```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -d "url=https://yourdomain.com/api/telegram/webhook" \
  -d "max_connections=100" \
  -d "drop_pending_updates=true"
```

بررسی وضعیت Webhook:
```bash
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

---

## 🔧 مرحله 7: تنظیمات اولیه بات

1. به بات خود در تلگرام بروید
2. دستور `/start` را بفرستید
3. به عنوان ادمین وارد شوید
4. از منوی ادمین:
   - تنظیم زبان پیش‌فرض (فارسی/انگلیسی)
   - فعال/غیرفعال کردن حالت تک مدل
   - تنظیمات دیگر

---

## 📊 مرحله 8: Monitoring و Logs

### مشاهده لاگ‌های برنامه:
```bash
# اگر از systemd استفاده می‌کنید:
sudo journalctl -u telegram-photo-bot -f

# اگر مستقیم اجرا می‌کنید:
# لاگ‌ها در کنسول نمایش داده می‌شوند
```

### بررسی وضعیت دیتابیس:
```sql
-- تعداد کاربران
SELECT COUNT(*) FROM "Users";

-- تعداد مدل‌ها
SELECT COUNT(*) FROM "Models" WHERE "Status" = 1;

-- تعداد محتوا
SELECT COUNT(*) FROM "Photos" WHERE "IsForSale" = true;

-- آخرین خریدها
SELECT * FROM "Purchases" ORDER BY "PurchaseDate" DESC LIMIT 10;
```

---

## 🔄 بروزرسانی (Update)

### مراحل بروزرسانی:
```bash
# 1. گرفتن backup از دیتابیس
pg_dump telegram_photo_bot > backup_$(date +%Y%m%d).sql

# 2. توقف سرویس
sudo systemctl stop telegram-photo-bot

# 3. دریافت نسخه جدید
cd /path/to/TelegramPictureBot
git pull origin main
dotnet publish -c Release -o /path/to/publish

# 4. اجرای Migration جدید (در صورت وجود)
cd TelegramPhotoBot.Infrastructure
dotnet ef database update --startup-project ../TelegramPhotoBot.Presentation

# 5. راه‌اندازی مجدد
sudo systemctl start telegram-photo-bot
```

---

## 🐛 عیب‌یابی (Troubleshooting)

### مشکل: بات پاسخ نمی‌دهد
```bash
# بررسی وضعیت Webhook
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"

# حذف Webhook (برای تست)
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/deleteWebhook"

# بررسی لاگ‌ها
sudo journalctl -u telegram-photo-bot -n 50
```

### مشکل: خطای دیتابیس
```bash
# بررسی اتصال به دیتابیس
psql -h localhost -U your_user -d telegram_photo_bot

# اجرای مجدد Migration
cd TelegramPhotoBot.Infrastructure
dotnet ef database drop --startup-project ../TelegramPhotoBot.Presentation
dotnet ef database update --startup-project ../TelegramPhotoBot.Presentation
```

### مشکل: MTProto کار نمی‌کند
- مطمئن شوید API ID و API Hash صحیح هستند
- مطمئن شوید شماره تلفن به فرمت بین‌المللی است (+989123456789)
- Session را پاک کنید و دوباره setup کنید:
```bash
rm -rf ./mtproto_session/*
```

---

## 📞 پشتیبانی

در صورت مشکل:
1. لاگ‌ها را بررسی کنید
2. GitHub Issues را چک کنید
3. از بخش Discussions در GitHub استفاده کنید

---

## 🔒 نکات امنیتی

1. **هرگز** Token ها و API Keys را در کد منبع قرار ندهید
2. از **Environment Variables** برای تنظیمات حساس استفاده کنید
3. **SSL/HTTPS** را حتماً فعال کنید
4. **Firewall** را به درستی تنظیم کنید
5. **Backup منظم** از دیتابیس بگیرید
6. رمزهای عبور را **قوی** انتخاب کنید

---

## ✅ چک‌لیست استقرار نهایی

- [ ] دیتابیس ساخته شد و Migration اجرا شد
- [ ] فایل `appsettings.json` پیکربندی شد
- [ ] Bot Token تنظیم شد
- [ ] Admin Telegram ID تنظیم شد
- [ ] برنامه با موفقیت اجرا شد
- [ ] Webhook تنظیم و تست شد
- [ ] MTProto راه‌اندازی شد
- [ ] بات در تلگرام پاسخ می‌دهد
- [ ] SSL فعال است
- [ ] Monitoring راه‌اندازی شد
- [ ] Backup برنامه‌ریزی شد

---

**🎉 تبریک! بات شما آماده استفاده است!**

برای راهنمای استفاده از بات، فایل `README.md` را مطالعه کنید.
