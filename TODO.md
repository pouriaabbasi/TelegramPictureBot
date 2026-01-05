# 📋 TODO List - Telegram Photo Bot

## 🎯 Overview
این لیست شامل تمام فیچرهای در انتظار پیاده‌سازی برای بات است. هر آیتم شامل توضیحات فنی، اولویت، و جزئیات پیاده‌سازی است.

---

## 📊 Status Summary
- **Total Tasks**: 13
- **Pending**: 11
- **In Progress**: 0
- **Completed**: 2

---

## 🚀 Feature List

### 1️⃣ **My Subscriptions - Model Navigation Buttons**
**Priority**: Medium  
**Status**: ✅ Completed  
**ID**: `my-subscription-buttons`

**Description**:
در صفحه "My Subscriptions" برای هر مدلی که کاربر subscribe کرده، یک دکمه اضافه بشه که مستقیم به لیست محتوای اون مدل بره.

**Implementation Details**:
- ✅ Updated `HandleMySubscriptionsCommandAsync` in `TelegramUpdateHandler.cs`
- ✅ Added inline buttons for each subscribed model
- ✅ Button callback: `view_content_{modelId}`
- ✅ Localized all messages and buttons

**Completed**: 2025-01-05

---

### 2️⃣ **Content Statistics in Model Dashboard**
**Priority**: High  
**Status**: ✅ Completed  
**ID**: `content-statistics`

**Description**:
در داشبورد مدل، برای هر محتوا آمار نمایش داده بشه:
- تعداد بازدید (Views)
- تعداد خرید (Purchases)
- درآمد کل (Total Revenue)
- نرخ تبدیل (Conversion Rate = Purchases / Views)

**Implementation Details**:
- ✅ `ViewCount` field already exists in `Photo` entity
- ✅ Added `GetContentStatisticsAsync` to `IPhotoRepository`
- ✅ Implemented analytics in `PhotoRepository`
- ✅ Added view tracking to:
  - `HandleViewPhotoAsync` (premium content)
  - `HandleViewDemoAsync` (demo content)
  - `PaymentCallbackHandler` (after purchase)
- ✅ Updated Model Dashboard with detailed statistics
- ✅ Added `HandleModelContentStatisticsAsync` handler
- ✅ Localized all content statistics messages
- ✅ Migration already exists (`AddViewHistoryAndViewCount`)

**Completed**: 2025-01-05

---

### 3️⃣ **Top 10 Most Popular Content**
**Priority**: Medium  
**Status**: Pending  
**ID**: `top-content-analytics`

**Description**:
مدل‌ها بتونن محبوب‌ترین محتوای خودشون رو ببینن:
- Top 10 Monthly (این ماه)
- Top 10 Yearly (این سال)
- Top 10 All Time (کل تاریخ)

**Technical Details**:
- Create `IContentAnalyticsService`
- Implement ranking by: Views, Purchases, Revenue
- Add time-range filtering
- Create new callback handlers in Model Dashboard
- Cache results (optional, using `IMemoryCache`)

**Button Flow**:
```
Model Dashboard → [📊 Analytics] → 
  - [📅 Top 10 This Month]
  - [📆 Top 10 This Year]
  - [🏆 Top 10 All Time]
```

**Queries Needed**:
```csharp
Task<List<ContentRankingDto>> GetTopContentAsync(
    Guid modelId,
    TimeRange range,
    int topCount = 10,
    RankingCriteria criteria = RankingCriteria.Purchases
);
```

---

### 4️⃣ **Batch Notifications for New Content**
**Priority**: High  
**Status**: Pending  
**ID**: `batch-notifications`

**Description**:
وقتی مدل محتوای جدید آپلود میکنه، به تمام Subscribers اون اعلان بره. برای جلوگیری از Rate Limit تلگرام:
- هر بار 50 نفر
- Delay بین هر batch: 1 ثانیه
- Background job برای ارسال

**Technical Details**:
- Create `INotificationService`
- Implement `SendBatchNotificationsAsync`
- Use `IBackgroundJobQueue` or Hangfire
- Track notification status (Sent, Failed, Pending)
- Add retry logic for failed notifications

**Database Changes**:
```csharp
public class ContentNotification : BaseEntity
{
    public Guid ContentId { get; set; }
    public Guid UserId { get; set; }
    public NotificationStatus Status { get; set; } // Pending, Sent, Failed
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
```

**Rate Limits**:
- 30 messages per second to different users
- 1 message per second to the same user
- Batch size: 50 users
- Delay: 1 second between batches

---

### 5️⃣ **New Payment System (Telegram Invoice + Stars)**
**Priority**: Critical  
**Status**: Pending  
**ID**: `payment-system`

**Description**:
پیاده‌سازی سیستم پرداخت با Telegram Invoice API:
- استفاده از Telegram Stars به جای Stars Provider
- پرداخت Invoice-based
- پشتیبانی از Star Reactions (اختیاری)
- تایید خودکار پرداخت

**Technical Details**:
- Replace custom Stars system with `SendInvoiceAsync`
- Handle `PreCheckoutQuery` and `SuccessfulPayment`
- Update `PaymentVerificationService`
- Add support for refunds (optional)

**API Methods Needed**:
```csharp
// Send invoice
await botClient.SendInvoiceAsync(
    chatId: chatId,
    title: "Premium Photo",
    description: "Access to premium content",
    payload: $"photo_{photoId}",
    providerToken: "", // Empty for Stars
    currency: "XTR", // Telegram Stars
    prices: new[] { new LabeledPrice("Price", amount) }
);

// Handle pre-checkout
async Task HandlePreCheckoutQueryAsync(PreCheckoutQuery query);

// Handle successful payment
async Task HandleSuccessfulPaymentAsync(Message message);
```

**Migration Notes**:
- Keep existing `TelegramStars` value object
- Add `PaymentMethod` enum: Manual, TelegramInvoice
- Update `Purchase` entity with invoice details

---

### 6️⃣ **Rating & Review System**
**Priority**: Medium  
**Status**: Pending  
**ID**: `review-rating-system`

**Description**:
کاربرها بتونن نظر و امتیاز بدن:
- **Model Rating**: به کل مدل (1-5 ستاره)
- **Content Rating**: به هر محتوای خریداری شده (1-5 ستاره)
- **Reviews**: کامنت متنی (با تایید مدل)
- **Moderation**: مدل باید کامنت رو تایید کنه

**Technical Details**:
- Create entities: `ModelReview`, `ContentReview`
- Rating calculation (average)
- Approval workflow for reviews
- Display ratings in model/content lists
- Spam/abuse detection (optional)

**Database Schema**:
```csharp
public class ModelReview : BaseEntity
{
    public Guid ModelId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; } // Pending, Approved, Rejected
    public DateTime? ReviewedAt { get; set; }
}

public class ContentReview : BaseEntity
{
    public Guid ContentId { get; set; } // PhotoId or VideoId
    public ContentType ContentType { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public ReviewStatus Status { get; set; }
}
```

**User Flow**:
```
After purchase → [⭐ Rate this content]
Input: Stars (1-5) + Optional comment
→ Model receives notification for approval
→ Model approves/rejects
→ Rating becomes public
```

---

### 7️⃣ **Wishlist System**
**Priority**: Low  
**Status**: Pending  
**ID**: `wishlist-system`

**Description**:
کاربرها بتونن محتوا رو به Wishlist اضافه کنن:
- Add to Wishlist
- Remove from Wishlist
- View Wishlist
- Buy from Wishlist (bulk purchase option)

**Technical Details**:
- Create `WishlistItem` entity
- Add button in content view: "💗 Add to Wishlist"
- Command: `/wishlist` to view saved items
- Callback handlers for add/remove

**Database Schema**:
```csharp
public class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ContentId { get; set; }
    public ContentType ContentType { get; set; } // Photo, Video
    public DateTime AddedAt { get; set; }
}
```

**UI Buttons**:
```
Content View:
  [💰 Buy] [💗 Add to Wishlist]

Wishlist:
  📋 Your Wishlist (3 items):
  1. Photo Title - 100 Stars [🛒 Buy] [🗑️ Remove]
  2. Video Title - 200 Stars [🛒 Buy] [🗑️ Remove]
  3. ...
```

---

### 8️⃣ **Model Revenue Dashboard**
**Priority**: High  
**Status**: Pending  
**ID**: `revenue-dashboard`

**Description**:
داشبورد کامل درآمد برای مدل‌ها:
- نمودار درآمد (Graphs) - روزانه، ماهانه
- آمار فروش (Sales Stats)
- Conversion Rate
- تعداد Subscribers
- تسویه‌حساب‌های قبلی
- مانده حساب فعلی

**Technical Details**:
- Create `IRevenueAnalyticsService`
- Generate charts (using external API or text-based)
- Calculate metrics:
  - Total Revenue
  - Revenue This Month
  - Revenue This Year
  - Avg Revenue Per Purchase
  - Subscriber Growth Rate
  - Content Performance

**Dashboard Layout**:
```
💰 Revenue Dashboard - [Model Name]

📊 Overview:
   Total Revenue: 50,000 Stars (4,250,000 تومان)
   This Month: 8,500 Stars
   Today: 320 Stars

📈 Stats:
   Total Subscribers: 245
   Total Sales: 1,234
   Avg Sale Value: 40.5 Stars
   Conversion Rate: 12.3%

💵 Payouts:
   Last Payout: 2024-12-15 (42,500 Stars)
   Pending Balance: 7,500 Stars
   Next Payout: 2025-01-15

[📊 Detailed Analytics]
[💳 Payout History]
[📥 Request Payout]
```

---

### 9️⃣ **Discount & Coupon System**
**Priority**: Low  
**Status**: Pending  
**ID**: `coupon-system`

**Description**:
سیستم کوپن تخفیف:
- کد تخفیف (Coupon Code)
- درصد تخفیف یا مقدار ثابت
- Bundle Deals (خرید چند محتوا با تخفیف)
- محدودیت زمانی
- محدودیت تعداد استفاده
- فقط برای کاربران خاص

**Technical Details**:
- Create `Coupon` entity
- Validation logic
- Apply discount at checkout
- Track usage
- Admin panel for creating coupons

**Database Schema**:
```csharp
public class Coupon : BaseEntity
{
    public string Code { get; set; } // "SUMMER2024"
    public DiscountType Type { get; set; } // Percentage, FixedAmount
    public int Value { get; set; } // 20 (for 20% or 20 Stars)
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public Guid? ModelId { get; set; } // null = all models
    public bool IsActive { get; set; }
}

public class CouponUsage : BaseEntity
{
    public Guid CouponId { get; set; }
    public Guid UserId { get; set; }
    public Guid PurchaseId { get; set; }
    public int DiscountAmount { get; set; }
}
```

**User Flow**:
```
Purchase Flow:
  Price: 100 Stars
  [💳 Have a coupon?]
  → Input: SUMMER20
  → Applied! New Price: 80 Stars (-20%)
  [✅ Confirm Purchase]
```

---

### 🔟 **Content Reporting & Moderation**
**Priority**: High  
**Status**: Pending  
**ID**: `report-moderation-system`

**Description**:
سیستم گزارش تخلفات:
- کاربرها میتونن محتوا رو Report کنن
- دلایل: Spam, Inappropriate, Scam, etc.
- Admin Panel برای بررسی
- امکان Suspend/Ban کردن مدل
- امکان حذف محتوا

**Technical Details**:
- Create `ContentReport` entity
- Moderation queue for admins
- Auto-suspend after X reports (optional)
- Email/notification to model on report
- Appeal system (optional)

**Database Schema**:
```csharp
public class ContentReport : BaseEntity
{
    public Guid ContentId { get; set; }
    public ContentType ContentType { get; set; }
    public Guid ReportedByUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; } // Pending, Reviewed, Resolved
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
    public ModerationAction? Action { get; set; } // None, RemoveContent, WarnModel, SuspendModel
}

public enum ReportReason
{
    Spam,
    Inappropriate,
    Scam,
    Copyright,
    Other
}

public enum ModerationAction
{
    None,
    RemoveContent,
    WarnModel,
    SuspendModel,
    BanModel
}
```

**Admin Panel**:
```
🚨 Moderation Queue (5 reports):

1. 📸 Photo "xyz" by @model1
   Reported by: @user123
   Reason: Inappropriate
   Details: "..."
   [✅ Dismiss] [⚠️ Warn] [🗑️ Remove] [🚫 Ban Model]

2. ...
```

---

### 1️⃣1️⃣ **Terms & Conditions for Model Registration**
**Priority**: High  
**Status**: Pending  
**ID**: `model-terms-conditions`

**Description**:
قبل از ثبت‌نام به عنوان مدل، شرایط نمایش داده بشه:
- 15% کارمزد پلتفرم (85% برای مدل)
- تسویه ماهانه
- قوانین محتوا
- سیاست‌های Privacy و Copyright
- مدل باید Accept کنه

**Technical Details**:
- Add Terms & Conditions text to database or config
- Display before model registration
- Add checkbox/button for acceptance
- Store acceptance date in `Model` entity
- Block registration if not accepted

**Database Changes**:
```csharp
// Add to Model entity:
public DateTime? TermsAcceptedAt { get; private set; }

public void AcceptTerms()
{
    TermsAcceptedAt = DateTime.UtcNow;
    MarkAsUpdated();
}
```

**UI Flow**:
```
User: /become_model

Bot: 📋 شرایط و قوانین ثبت‌نام به عنوان مدل:

1️⃣ کارمزد پلتفرم:
   - 15% از درآمد شما به پلتفرم
   - 85% برای شما
   - مثال: برای هر 100 Stars، 85 Stars به شما میرسه

2️⃣ تسویه‌حساب:
   - تسویه به صورت ماهانه
   - حداقل موجودی برای برداشت: 1,000 Stars
   - روش پرداخت: انتقال بانکی / کارت به کارت

3️⃣ قوانین محتوا:
   - محتوای غیرقانونی ممنوع
   - محتوای حق نشر دار دیگران ممنوع
   - رعایت قوانین تلگرام الزامی

[✅ قبول دارم و ثبت‌نام میکنم]
[❌ انصراف]
```

---

### 1️⃣2️⃣ **Admin Payout Recording System**
**Priority**: High  
**Status**: Pending  
**ID**: `admin-payout-system`

**Description**:
سیستم ثبت تسویه توسط Admin:
- ادمین میتونه تسویه ثبت کنه
- تاریخ، مقدار، روش پرداخت
- شماره پیگیری
- یادداشت اختیاری
- وضعیت: Pending → Completed → Verified

**Technical Details**:
- Create `ModelPayout` entity
- Admin panel for recording payouts
- Validation (model balance >= payout amount)
- Update model balance after payout
- Notification to model

**Database Schema**:
```csharp
public class ModelPayout : BaseEntity
{
    public Guid ModelId { get; set; }
    public long AmountStars { get; set; } // Total Stars
    public decimal AmountFiat { get; set; } // In Toman/Dollar
    public string Currency { get; set; } // "IRR", "USD"
    public decimal ExchangeRate { get; set; } // Stars to Fiat
    public PayoutMethod Method { get; set; } // BankTransfer, CardToCard, Crypto
    public string? TrackingNumber { get; set; }
    public PayoutStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid ProcessedByAdminId { get; set; }
    public string? AdminNotes { get; set; }
}

public enum PayoutMethod
{
    BankTransfer,
    CardToCard,
    Crypto,
    Other
}

public enum PayoutStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
```

**Admin Flow**:
```
Admin Panel → [💰 Model Payouts]

Select Model: @model1
Current Balance: 5,000 Stars

Record Payout:
  Amount (Stars): [5000]
  Amount (Toman): [425000]
  Exchange Rate: [85 تومان/Star]
  Method: [Card to Card ▼]
  Card Number: [6037-****-****-1234]
  Tracking: [123456789]
  Notes: [_______________]
  
[✅ Record Payout] [❌ Cancel]
```

---

### 1️⃣3️⃣ **Payout History in Model Dashboard**
**Priority**: High  
**Status**: Pending  
**ID**: `payout-history-dashboard`

**Description**:
مدل‌ها بتونن تاریخچه تسویه‌ها رو ببینن:
- تاریخ هر تسویه
- مقدار (Stars و تومان)
- وضعیت (در انتظار / پرداخت شده)
- تسویه بعدی کی هست
- مانده حساب فعلی

**Technical Details**:
- Query payouts from database
- Display in Model Dashboard
- Pagination for long lists
- Export to PDF/CSV (optional)

**Display Format**:
```
💳 تاریخچه تسویه‌حساب

💰 موجودی فعلی: 2,450 Stars (208,250 تومان)
📅 تسویه بعدی: 15 اردیبهشت 1404

📋 تسویه‌های قبلی:

1️⃣ 2024-12-15
   💵 5,000 Stars → 425,000 تومان
   ✅ پرداخت شده
   شماره پیگیری: 123456789

2️⃣ 2024-11-15
   💵 3,200 Stars → 272,000 تومان
   ✅ پرداخت شده
   شماره پیگیری: 987654321

3️⃣ 2024-10-15
   💵 4,100 Stars → 348,500 تومان
   ✅ پرداخت شده

[◀️ Previous] [Next ▶️]
[📥 Request New Payout]
```

---

## 🔄 Implementation Priority

### Phase 1 (Critical - Revenue & Payment)
1. Terms & Conditions (#11) ⭐⭐⭐
2. Admin Payout System (#12) ⭐⭐⭐
3. Payout History Dashboard (#13) ⭐⭐⭐
4. New Payment System (#5) ⭐⭐⭐

### Phase 2 (High - User Experience)
5. Content Statistics (#2) ⭐⭐
6. Batch Notifications (#4) ⭐⭐
7. Content Reporting (#10) ⭐⭐
8. Revenue Dashboard (#8) ⭐⭐

### Phase 3 (Medium - Nice to Have)
9. My Subscriptions Buttons (#1) ⭐
10. Top Content Analytics (#3) ⭐
11. Rating & Review System (#6) ⭐

### Phase 4 (Low - Future Enhancement)
12. Coupon System (#9)
13. Wishlist System (#7)

---

## 📝 Notes

- All features should maintain RTL (Persian) support
- Performance considerations for large datasets
- Rate limiting for Telegram API calls
- Security: Input validation, SQL injection prevention
- Testing: Unit tests for each new service
- Documentation: Update README after each feature

---

## 🔗 Related Documents
- [Architecture.md](./Architecture.md) - System architecture
- [API_Documentation.md](./API_Documentation.md) - API reference
- [Database_Schema.md](./Database_Schema.md) - Database design

---

**Last Updated**: 2024-12-30  
**Version**: 1.0  
**Maintainer**: Development Team
