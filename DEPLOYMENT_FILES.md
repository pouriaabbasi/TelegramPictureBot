# فایل‌های مورد نیاز برای Deployment

## برای تغییرات Health Check (این تغییر)

### فایل‌های ضروری:
```
publish/TelegramPhotoBot.Presentation.dll
publish/TelegramPhotoBot.Presentation.exe
publish/TelegramPhotoBot.Presentation.deps.json
publish/TelegramPhotoBot.Presentation.runtimeconfig.json
```

### فایل‌های اختیاری (برای debugging):
```
publish/TelegramPhotoBot.Presentation.pdb
```

---

## برای تغییرات MTProto (تغییرات قبلی)

اگر تغییرات MTProto را هم deploy می‌کنید:
```
publish/TelegramPhotoBot.Infrastructure.dll
publish/TelegramPhotoBot.Infrastructure.pdb (اختیاری)
```

---

## برای تغییرات Application Layer

اگر تغییراتی در Application layer داشتید:
```
publish/TelegramPhotoBot.Application.dll
publish/TelegramPhotoBot.Application.pdb (اختیاری)
```

---

## برای تغییرات Domain Layer

اگر تغییراتی در Domain layer داشتید:
```
publish/TelegramPhotoBot.Domain.dll
publish/TelegramPhotoBot.Domain.pdb (اختیاری)
```

---

## نکات مهم

1. **همیشه فایل‌های .deps.json و .runtimeconfig.json را هم کپی کنید** - این فایل‌ها برای runtime ضروری هستند

2. **اگر dependency جدیدی اضافه شده** - باید DLL مربوط به آن dependency را هم کپی کنید

3. **اگر appsettings.json تغییر کرده** - باید آن را هم کپی کنید

4. **اگر web.config تغییر کرده** - باید آن را هم کپی کنید

5. **برای اطمینان بیشتر** - اگر مطمئن نیستید، کل پوشه publish را کپی کنید

---

## دستور PowerShell برای کپی فقط فایل‌های تغییر یافته

```powershell
# کپی فقط فایل‌های Presentation (برای تغییرات Health Check)
Copy-Item ".\publish\TelegramPhotoBot.Presentation.*" -Destination "\\server\path\" -Force
```

---

## دستور برای کپی همه فایل‌های پروژه (بدون dependencies)

```powershell
# کپی فقط DLL های پروژه خودتان
Copy-Item ".\publish\TelegramPhotoBot.*.dll" -Destination "\\server\path\" -Force
Copy-Item ".\publish\TelegramPhotoBot.*.exe" -Destination "\\server\path\" -Force
Copy-Item ".\publish\TelegramPhotoBot.*.deps.json" -Destination "\\server\path\" -Force
Copy-Item ".\publish\TelegramPhotoBot.*.runtimeconfig.json" -Destination "\\server\path\" -Force
```

---

## توصیه

**برای اولین بار**: کل پوشه publish را کپی کنید

**برای آپدیت‌های بعدی**: فقط فایل‌های DLL مربوط به لایه‌های تغییر یافته را کپی کنید

**اگر خطا گرفتید**: کل پوشه publish را کپی کنید

