# راهنمای تنظیمات Plesk برای Telegram Photo Bot

## مشکل دسترسی (Access Denied)

اگر بعد از وارد کردن اطلاعات MTProto خطای **Access Denied** دریافت می‌کنید، باید دسترسی‌های فایل و پوشه را در Plesk تنظیم کنید.

## مراحل تنظیم در Plesk

### 1. ایجاد پوشه Data

پوشه `Data` باید در مسیر اصلی برنامه (Base Directory) ایجاد شود. این پوشه برای ذخیره فایل session استفاده می‌شود.

**مسیر پیش‌فرض:**
```
{BaseDirectory}/Data/mtproto_session.dat
```

### 2. تنظیم دسترسی‌های پوشه Data

#### روش 1: از طریق File Manager در Plesk

1. وارد Plesk شوید
2. به **File Manager** بروید
3. به مسیر اصلی برنامه (معمولاً `httpdocs` یا `vhost`) بروید
4. پوشه `Data` را ایجاد کنید (اگر وجود ندارد)
5. روی پوشه `Data` راست کلیک کنید و **Change Permissions** را انتخاب کنید
6. دسترسی‌ها را به این صورت تنظیم کنید:
   - **Owner (Owner)**: Read, Write, Execute (755 یا 777)
   - **Group**: Read, Execute (755)
   - **Others**: Read, Execute (755)

#### روش 2: از طریق SSH/Terminal

```bash
# ایجاد پوشه Data
mkdir -p /path/to/your/app/Data

# تنظیم دسترسی‌ها
chmod 755 /path/to/your/app/Data
chown your_user:your_group /path/to/your/app/Data
```

### 3. تنظیم دسترسی‌های Application Pool

در Plesk، باید Application Pool را به گونه‌ای تنظیم کنید که دسترسی نوشتن به پوشه `Data` داشته باشد:

1. به **Websites & Domains** بروید
2. دامنه خود را انتخاب کنید
3. به **ASP.NET Settings** یا **Application Pool** بروید
4. **Application Pool Identity** را بررسی کنید
5. مطمئن شوید که این Identity دسترسی نوشتن به پوشه `Data` دارد

### 4. بررسی مسیر Base Directory

برنامه از `AppDomain.CurrentDomain.BaseDirectory` برای تعیین مسیر استفاده می‌کند. این مسیر معمولاً:
- در IIS/Plesk: مسیر فیزیکی برنامه است
- باید دسترسی نوشتن داشته باشد

### 5. تنظیمات IIS (اگر در دسترس است)

اگر به تنظیمات IIS دسترسی دارید:

1. **IIS Manager** را باز کنید
2. Application Pool مربوط به برنامه را پیدا کنید
3. **Advanced Settings** را باز کنید
4. **Identity** را بررسی کنید
5. مطمئن شوید که Identity دسترسی نوشتن به پوشه `Data` دارد

### 6. تست دسترسی

برای تست دسترسی، می‌توانید از Health Check استفاده کنید:

```
GET /health
```

یا مستقیماً بررسی کنید که آیا فایل session ایجاد می‌شود یا نه.

## نکات مهم

1. **امنیت**: پوشه `Data` نباید از طریق وب قابل دسترسی باشد. مطمئن شوید که:
   - فایل `web.config` یا `.htaccess` برای جلوگیری از دسترسی مستقیم تنظیم شده است
   - یا پوشه `Data` خارج از `httpdocs` قرار دارد

2. **Backup**: فایل `mtproto_session.dat` مهم است و باید backup شود.

3. **Logs**: اگر مشکل ادامه داشت، لاگ‌های برنامه را بررسی کنید:
   - مسیر session در لاگ‌ها نمایش داده می‌شود
   - خطاهای دسترسی در لاگ‌ها ثبت می‌شوند

## مثال تنظیم web.config برای جلوگیری از دسترسی مستقیم

اگر پوشه `Data` در `httpdocs` قرار دارد، این تنظیمات را به `web.config` اضافه کنید:

```xml
<configuration>
  <location path="Data">
    <system.webServer>
      <httpHandlers>
        <clear />
      </httpHandlers>
      <handlers>
        <clear />
      </handlers>
      <authorization>
        <deny users="*" />
      </authorization>
    </system.webServer>
  </location>
</configuration>
```

## پشتیبانی

اگر مشکل ادامه داشت:
1. لاگ‌های برنامه را بررسی کنید
2. مسیر session را در لاگ‌ها پیدا کنید
3. دسترسی‌های آن مسیر را بررسی کنید

