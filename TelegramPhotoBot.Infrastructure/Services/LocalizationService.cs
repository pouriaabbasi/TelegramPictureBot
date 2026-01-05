using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    private const string LanguageSettingKey = "platform:bot_language";
    private const string DefaultLanguage = "Persian";
    
    // Complete dictionary of all localized strings
    private static readonly Dictionary<string, Dictionary<BotLanguage, string>> _strings = new()
    {
        ["common.back_to_main"] = new()
        {
            { BotLanguage.Persian, "🏠 بازگشت به منوی اصلی" },
            { BotLanguage.English, "🏠 Back to Main Menu" }
        },
        
        #region Main Menu
        ["menu.welcome"] = new()
        {
            { BotLanguage.Persian, "👋 به بات خوش آمدید!" },
            { BotLanguage.English, "👋 Welcome to the bot!" }
        },
        ["menu.browse_models"] = new()
        {
            { BotLanguage.Persian, "🔍 مشاهده مدل‌ها" },
            { BotLanguage.English, "🔍 Browse Models" }
        },
        ["menu.my_subscriptions"] = new()
        {
            { BotLanguage.Persian, "💎 اشتراک‌های من" },
            { BotLanguage.English, "💎 My Subscriptions" }
        },
        ["menu.my_content"] = new()
        {
            { BotLanguage.Persian, "📁 محتوای من" },
            { BotLanguage.English, "📁 My Content" }
        },
        ["menu.become_model"] = new()
        {
            { BotLanguage.Persian, "🎭 ثبت‌نام به عنوان مدل" },
            { BotLanguage.English, "🎭 Become a Model" }
        },
        ["menu.model_dashboard"] = new()
        {
            { BotLanguage.Persian, "📊 داشبورد مدل" },
            { BotLanguage.English, "📊 Model Dashboard" }
        },
        ["menu.admin_panel"] = new()
        {
            { BotLanguage.Persian, "🛡️ پنل ادمین" },
            { BotLanguage.English, "🛡️ Admin Panel" }
        },
        ["menu.back"] = new()
        {
            { BotLanguage.Persian, "« بازگشت به منوی اصلی" },
            { BotLanguage.English, "« Back to Main Menu" }
        },
        ["menu.view_model_content"] = new()
        {
            { BotLanguage.Persian, "📸 مشاهده محتوای {0}" },
            { BotLanguage.English, "📸 View {0}'s Content" }
        },
        #endregion

        #region Common Messages
        ["common.error"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در پردازش درخواست شما. لطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Error processing your request. Please try again." }
        },
        ["common.not_found"] = new()
        {
            { BotLanguage.Persian, "❌ مورد یافت نشد." },
            { BotLanguage.English, "❌ Not found." }
        },
        ["common.success"] = new()
        {
            { BotLanguage.Persian, "✅ با موفقیت انجام شد." },
            { BotLanguage.English, "✅ Successfully completed." }
        },
        ["common.cancel"] = new()
        {
            { BotLanguage.Persian, "❌ لغو" },
            { BotLanguage.English, "❌ Cancel" }
        },
        ["common.confirm"] = new()
        {
            { BotLanguage.Persian, "✅ تأیید" },
            { BotLanguage.English, "✅ Confirm" }
        },
        ["common.loading"] = new()
        {
            { BotLanguage.Persian, "⏳ در حال بارگذاری..." },
            { BotLanguage.English, "⏳ Loading..." }
        },
        #endregion

        #region Model Registration
        ["model.register.not_model"] = new()
        {
            { BotLanguage.Persian, "شما هنوز به عنوان مدل ثبت‌نام نکرده‌اید. از منوی اصلی 'Become a Model' را انتخاب کنید!" },
            { BotLanguage.English, "You are not registered as a model yet. Use 'Become a Model' to register!" }
        },
        ["model.register.pending"] = new()
        {
            { BotLanguage.Persian, "⏳ درخواست شما در انتظار بررسی است.\n\nادمین‌ها به زودی درخواست شما را بررسی خواهند کرد." },
            { BotLanguage.English, "⏳ Your application is pending review.\n\nAdmins will review your request soon." }
        },
        ["model.register.rejected"] = new()
        {
            { BotLanguage.Persian, "❌ درخواست ثبت‌نام شما رد شده است.\n\nمی‌توانید درخواست جدیدی ارسال کنید." },
            { BotLanguage.English, "❌ Your registration request has been rejected.\n\nYou can submit a new application." }
        },
        ["model.register.reapply"] = new()
        {
            { BotLanguage.Persian, "✅ ارسال درخواست جدید" },
            { BotLanguage.English, "✅ Submit New Application" }
        },
        ["model.register.name_prompt"] = new()
        {
            { BotLanguage.Persian, "🎭 ثبت‌نام به عنوان مدل\n\nلطفاً نام نمایشی خود را وارد کنید:" },
            { BotLanguage.English, "🎭 Model Registration\n\nPlease enter your display name:" }
        },
        ["model.register.bio_prompt"] = new()
        {
            { BotLanguage.Persian, "✅ عالی! نام شما ذخیره شد: {0}\n\nحالا لطفاً بیوگرافی خود را وارد کنید (توضیح کوتاه درباره خودتان):" },
            { BotLanguage.English, "✅ Great! Your name has been saved: {0}\n\nNow please enter your bio (a short description about yourself):" }
        },
        ["model.register.submitted"] = new()
        {
            { BotLanguage.Persian, "✅ درخواست شما با موفقیت ارسال شد!\n\nنام نمایشی: {0}\nبیوگرافی: {1}\n\nادمین‌ها به زودی درخواست شما را بررسی خواهند کرد." },
            { BotLanguage.English, "✅ Your application has been submitted successfully!\n\nDisplay Name: {0}\nBio: {1}\n\nAdmins will review your request soon." }
        },
        ["model.status.not_approved"] = new()
        {
            { BotLanguage.Persian, "وضعیت حساب شما: {0}. فقط مدل‌های تأییدشده می‌توانند به داشبورد دسترسی داشته باشند." },
            { BotLanguage.English, "Your model account is {0}. Only approved models can access the dashboard." }
        },
        #endregion

        #region Model Dashboard
        ["dashboard.title"] = new()
        {
            { BotLanguage.Persian, "💰 **داشبورد درآمد: {0}**\n\n" },
            { BotLanguage.English, "💰 **Revenue Dashboard: {0}**\n\n" }
        },
        ["dashboard.revenue"] = new()
        {
            { BotLanguage.Persian, "📊 **نمای کلی درآمد:**\n   💵 کل درآمد: {0:N0} ⭐️\n   📅 این ماه: {1:N0} ⭐️\n   📆 امروز: {2:N0} ⭐️\n   💰 موجودی قابل برداشت: {3:N0} ⭐️\n\n" },
            { BotLanguage.English, "📊 **Revenue Overview:**\n   💵 Total Revenue: {0:N0} ⭐️\n   📅 This Month: {1:N0} ⭐️\n   📆 Today: {2:N0} ⭐️\n   💰 Available Balance: {3:N0} ⭐️\n\n" }
        },
        ["dashboard.metrics"] = new()
        {
            { BotLanguage.Persian, "📈 **معیارهای عملکرد:**\n   👥 کل مشترکین: {0}\n   🛒 کل فروش: {1}\n   💸 میانگین فروش: {2:N0} ⭐️\n   📊 نرخ تبدیل: {3:F2}%\n\n" },
            { BotLanguage.English, "📈 **Performance Metrics:**\n   👥 Total Subscribers: {0}\n   🛒 Total Sales: {1}\n   💸 Average Sale: {2:N0} ⭐️\n   📊 Conversion Rate: {3:F2}%\n\n" }
        },
        ["dashboard.top_content"] = new()
        {
            { BotLanguage.Persian, "🏆 **3 محتوای برتر:**\n" },
            { BotLanguage.English, "🏆 **Top 3 Content Items:**\n" }
        },
        ["dashboard.recent_payouts"] = new()
        {
            { BotLanguage.Persian, "💳 **آخرین تسویه‌حساب‌ها:**\n" },
            { BotLanguage.English, "💳 **Recent Payouts:**\n" }
        },
        ["dashboard.no_payouts"] = new()
        {
            { BotLanguage.Persian, "💳 **هنوز تسویه‌حسابی انجام نشده**\n\n" },
            { BotLanguage.English, "💳 **No payouts yet**\n\n" }
        },
        ["dashboard.upload_premium"] = new()
        {
            { BotLanguage.Persian, "📤 آپلود محتوای ویژه" },
            { BotLanguage.English, "📤 Upload Premium Content" }
        },
        ["dashboard.upload_demo"] = new()
        {
            { BotLanguage.Persian, "🎁 آپلود دمو" },
            { BotLanguage.English, "🎁 Upload Demo" }
        },
        ["dashboard.my_content"] = new()
        {
            { BotLanguage.Persian, "📋 محتوای من" },
            { BotLanguage.English, "📋 My Content" }
        },
        ["dashboard.content_stats"] = new()
        {
            { BotLanguage.Persian, "📊 آمار محتوا" },
            { BotLanguage.English, "📊 Content Stats" }
        },
        ["dashboard.top_content_btn"] = new()
        {
            { BotLanguage.Persian, "🏆 محتوای برتر" },
            { BotLanguage.English, "🏆 Top Content" }
        },
        ["dashboard.set_alias"] = new()
        {
            { BotLanguage.Persian, "🏷️ تنظیم نام مستعار" },
            { BotLanguage.English, "🏷️ Set Alias" }
        },
        ["dashboard.change_alias"] = new()
        {
            { BotLanguage.Persian, "🏷️ تغییر نام مستعار" },
            { BotLanguage.English, "🏷️ Change Alias" }
        },
        ["dashboard.manage_subscription"] = new()
        {
            { BotLanguage.Persian, "💳 مدیریت طرح اشتراک" },
            { BotLanguage.English, "💳 Manage Subscription Plan" }
        },
        #endregion

        #region Content Statistics
        ["stats.title"] = new()
        {
            { BotLanguage.Persian, "📊 **آمار محتوا**\n\n" },
            { BotLanguage.English, "📊 **Content Statistics**\n\n" }
        },
        ["stats.no_data"] = new()
        {
            { BotLanguage.Persian, "📊 هنوز آماری موجود نیست.\n\nمحتوایی آپلود کنید تا آمار دقیق را ببینید!" },
            { BotLanguage.English, "📊 No content statistics available yet.\n\nUpload some content to see detailed statistics!" }
        },
        ["stats.top_all_time"] = new()
        {
            { BotLanguage.Persian, "🌟 **برترین‌های تمام دوران:**\n" },
            { BotLanguage.English, "🌟 **All Time Top 10:**\n" }
        },
        ["stats.top_month"] = new()
        {
            { BotLanguage.Persian, "📅 **برترین‌های این ماه:**\n" },
            { BotLanguage.English, "📅 **This Month Top 10:**\n" }
        },
        ["stats.top_year"] = new()
        {
            { BotLanguage.Persian, "📆 **برترین‌های امسال:**\n" },
            { BotLanguage.English, "📆 **This Year Top 10:**\n" }
        },
        ["stats.no_top_content"] = new()
        {
            { BotLanguage.Persian, "🏆 هنوز محتوای برتری موجود نیست.\n\nمحتوا آپلود کنید و بفروشید تا محتوای برتر خود را ببینید!" },
            { BotLanguage.English, "🏆 No top content available yet.\n\nUpload and sell content to see your top performers!" }
        },
        ["stats.back_dashboard"] = new()
        {
            { BotLanguage.Persian, "<< بازگشت به داشبورد" },
            { BotLanguage.English, "<< Back to Dashboard" }
        },
        #endregion

        #region Content Upload
        ["upload.premium.prompt"] = new()
        {
            { BotLanguage.Persian, "📤 آپلود محتوای ویژه\n\nلطفاً عکس یا ویدیوی خود را ارسال کنید:" },
            { BotLanguage.English, "📤 Upload Premium Content\n\nPlease send your photo or video:" }
        },
        ["upload.demo.prompt"] = new()
        {
            { BotLanguage.Persian, "🎁 آپلود محتوای دمو\n\nلطفاً عکس یا ویدیوی دمو خود را ارسال کنید:" },
            { BotLanguage.English, "🎁 Upload Demo Content\n\nPlease send your demo photo or video:" }
        },
        ["upload.price.prompt"] = new()
        {
            { BotLanguage.Persian, "💰 قیمت محتوا\n\nلطفاً قیمت را به استارز وارد کنید (فقط عدد):" },
            { BotLanguage.English, "💰 Content Price\n\nPlease enter the price in stars (numbers only):" }
        },
        ["upload.caption.prompt"] = new()
        {
            { BotLanguage.Persian, "✏️ توضیحات محتوا\n\nلطفاً توضیحات محتوا را وارد کنید (اختیاری - برای رد شدن 'skip' بزنید):" },
            { BotLanguage.English, "✏️ Content Caption\n\nPlease enter a caption (optional - type 'skip' to skip):" }
        },
        ["upload.success"] = new()
        {
            { BotLanguage.Persian, "✅ محتوای شما با موفقیت آپلود شد!\n\nنوع: {0}\nقیمت: {1} ⭐️" },
            { BotLanguage.English, "✅ Your content has been uploaded successfully!\n\nType: {0}\nPrice: {1} ⭐️" }
        },
        ["upload.invalid_price"] = new()
        {
            { BotLanguage.Persian, "❌ قیمت نامعتبر است. لطفاً یک عدد معتبر وارد کنید." },
            { BotLanguage.English, "❌ Invalid price. Please enter a valid number." }
        },
        #endregion

        #region Subscriptions
        ["subscription.title"] = new()
        {
            { BotLanguage.Persian, "💎 **اشتراک‌های من**\n\n" },
            { BotLanguage.English, "💎 **My Subscriptions**\n\n" }
        },
        ["subscription.none"] = new()
        {
            { BotLanguage.Persian, "شما هنوز اشتراکی ندارید.\n\nاز منوی 'مشاهده مدل‌ها' برای خرید اشتراک استفاده کنید." },
            { BotLanguage.English, "You don't have any subscriptions yet.\n\nUse 'Browse Models' to purchase a subscription." }
        },
        ["subscription.active"] = new()
        {
            { BotLanguage.Persian, "✅ فعال تا {0}" },
            { BotLanguage.English, "✅ Active until {0}" }
        },
        ["subscription.expired"] = new()
        {
            { BotLanguage.Persian, "❌ منقضی شده ({0})" },
            { BotLanguage.English, "❌ Expired ({0})" }
        },
        ["subscription.view_content"] = new()
        {
            { BotLanguage.Persian, "📸 {0}" },
            { BotLanguage.English, "📸 {0}" }
        },
        #endregion

        #region Model Terms & Conditions
        ["terms.title"] = new()
        {
            { BotLanguage.Persian, "📜 **شرایط و قوانین ثبت‌نام به عنوان مدل**\n\n" },
            { BotLanguage.English, "📜 **Model Registration Terms & Conditions**\n\n" }
        },
        ["terms.content.persian"] = new()
        {
            { BotLanguage.Persian, 
@"با قبول این شرایط، شما موارد زیر را می‌پذیرید:

**💰 کارمزد پلتفرم:**
• پلتفرم 15% از کل درآمد شما را به عنوان کارمزد برمی‌دارد
• این کارمزد شامل هزینه‌های نگهداری، پشتیبانی و توسعه پلتفرم می‌شود

**💳 نحوه تسویه‌حساب:**
• تسویه‌حساب به صورت ماهانه انجام می‌شود
• پرداخت‌ها تا پایان هر ماه برای ماه قبل واریز می‌شوند
• حداقل مبلغ برای برداشت: 1000 استارز تلگرام

**🔄 هزینه انتقال:**
• هزینه کارمزد انتقال وجه (به هر روشی) نصف نصف بین مدل و پلتفرم تقسیم می‌شود

**📋 مسئولیت‌های شما:**
• محتوای شما نباید نقض قوانین تلگرام یا قوانین کشور باشد
• مسئولیت صحت و قانونی بودن محتوا بر عهده شماست
• پلتفرم حق حذف یا مسدود کردن محتوای نامناسب را دارد

**⚖️ اهمیت قانونی:**
• با پذیرش این شرایط، تاریخ و زمان دقیق پذیرش و محتوای کامل این توافق‌نامه در دیتابیس ذخیره می‌شود
• این اطلاعات برای مسائل حقوقی آینده قابل استفاده است

با زدن دکمه 'قبول می‌کنم' شما تأیید می‌کنید که این شرایط را خوانده و می‌پذیرید." },
            { BotLanguage.English, "" }
        },
        ["terms.content.english"] = new()
        {
            { BotLanguage.Persian, "" },
            { BotLanguage.English,
@"By accepting these terms, you agree to the following:

**💰 Platform Commission:**
• The platform takes a 15% commission from your total revenue
• This commission covers maintenance, support, and platform development costs

**💳 Settlement Method:**
• Settlements are made monthly
• Payments are transferred by the end of each month for the previous month
• Minimum withdrawal amount: 1000 Telegram Stars

**🔄 Transfer Fees:**
• Transfer fees (by any method) are split 50/50 between the model and the platform

**📋 Your Responsibilities:**
• Your content must not violate Telegram's rules or country laws
• You are responsible for the accuracy and legality of your content
• The platform reserves the right to remove or block inappropriate content

**⚖️ Legal Importance:**
• By accepting these terms, the exact date, time, and full content of this agreement will be stored in the database
• This information can be used for future legal matters

By clicking 'I Accept' you confirm that you have read and accept these terms." }
        },
        ["terms.accept"] = new()
        {
            { BotLanguage.Persian, "✅ قبول می‌کنم" },
            { BotLanguage.English, "✅ I Accept" }
        },
        ["terms.decline"] = new()
        {
            { BotLanguage.Persian, "❌ نمی‌پذیرم" },
            { BotLanguage.English, "❌ I Decline" }
        },
        ["terms.declined"] = new()
        {
            { BotLanguage.Persian, "شما شرایط را نپذیرفتید. بدون پذیرش شرایط نمی‌توانید به عنوان مدل ثبت‌نام کنید." },
            { BotLanguage.English, "You declined the terms. You cannot register as a model without accepting the terms." }
        },
        #endregion

        #region Admin Panel
        ["admin.no_permission"] = new()
        {
            { BotLanguage.Persian, "❌ شما دسترسی ادمین ندارید." },
            { BotLanguage.English, "❌ You don't have admin permissions." }
        },
        ["admin.panel.title"] = new()
        {
            { BotLanguage.Persian, "🛡️ **پنل ادمین**\n\nچه کاری می‌خواهید انجام دهید?" },
            { BotLanguage.English, "🛡️ **Admin Panel**\n\nWhat would you like to do?" }
        },
        ["admin.pending_models"] = new()
        {
            { BotLanguage.Persian, "👤 بررسی مدل‌های در انتظار" },
            { BotLanguage.English, "👤 Review Pending Models" }
        },
        ["admin.settings"] = new()
        {
            { BotLanguage.Persian, "⚙️ تنظیمات" },
            { BotLanguage.English, "⚙️ Settings" }
        },
        ["admin.settings.title"] = new()
        {
            { BotLanguage.Persian, "⚙️ **تنظیمات پلتفرم**" },
            { BotLanguage.English, "⚙️ **Platform Settings**" }
        },
        ["admin.language"] = new()
        {
            { BotLanguage.Persian, "🌐 زبان بات" },
            { BotLanguage.English, "🌐 Bot Language" }
        },
        ["admin.language.current"] = new()
        {
            { BotLanguage.Persian, "زبان فعلی بات: {0}\n\nزبان جدید را انتخاب کنید:" },
            { BotLanguage.English, "Current bot language: {0}\n\nSelect new language:" }
        },
        ["admin.language.persian"] = new()
        {
            { BotLanguage.Persian, "🇮🇷 فارسی" },
            { BotLanguage.English, "🇮🇷 Persian" }
        },
        ["admin.language.english"] = new()
        {
            { BotLanguage.Persian, "🇬🇧 انگلیسی" },
            { BotLanguage.English, "🇬🇧 English" }
        },
        ["admin.language.updated"] = new()
        {
            { BotLanguage.Persian, "✅ زبان بات با موفقیت تغییر کرد به: {0}" },
            { BotLanguage.English, "✅ Bot language updated successfully to: {0}" }
        },
        ["admin.single_model_settings"] = new()
        {
            { BotLanguage.Persian, "🎯 حالت تک مدل" },
            { BotLanguage.English, "🎯 Single Model Mode" }
        },
        #endregion

        #region Contact Verification
        ["contact.verification.required"] = new()
        {
            { BotLanguage.Persian, "📱 برای دریافت محتوا، لطفاً مراحل زیر را به ترتیب انجام دهید:\n\n۱. اکانت فرستنده را به لیست مخاطبین خود اضافه کنید\n   Username: @{0}\n\n۲. پس از اضافه کردن، یک پیام کوتاه برای ما ارسال کنید (مثلاً: \"سلام\" یا \"آماده‌ام\")\n\n⚠️ این مراحل برای امنیت شما و تضمین دریافت محتوا ضروری است.\n\n💡 تا زمانی که این مراحل انجام نشود، امکان ارسال محتوا وجود ندارد." },
            { BotLanguage.English, "📱 To receive content, please follow these steps:\n\n1. Add the sender account to your contacts\n   Username: @{0}\n\n2. After adding, send us a short message (e.g., \"Hello\" or \"Ready\")\n\n⚠️ These steps are necessary for your security and to ensure content delivery.\n\n💡 Content cannot be sent until these steps are completed." }
        },
        ["contact.add"] = new()
        {
            { BotLanguage.Persian, "➕ اضافه کردن مخاطب" },
            { BotLanguage.English, "➕ Add Contact" }
        },
        #endregion

        #region Purchase & Content
        ["purchase.buy_photo"] = new()
        {
            { BotLanguage.Persian, "💳 خرید ({0} ⭐️)" },
            { BotLanguage.English, "💳 Buy ({0} ⭐️)" }
        },
        ["purchase.view_demo"] = new()
        {
            { BotLanguage.Persian, "👁️ مشاهده دمو" },
            { BotLanguage.English, "👁️ View Demo" }
        },
        ["purchase.already_purchased"] = new()
        {
            { BotLanguage.Persian, "شما قبلاً این محتوا را خریداری کرده‌اید! از منوی 'محتوای من' می‌توانید آن را مشاهده کنید." },
            { BotLanguage.English, "You have already purchased this content! You can view it from the 'My Content' menu." }
        },
        ["purchase.success"] = new()
        {
            { BotLanguage.Persian, "✅ خرید موفق!\n\nمحتوای شما در حال ارسال است..." },
            { BotLanguage.English, "✅ Purchase successful!\n\nYour content is being sent..." }
        },
        ["purchase.failed"] = new()
        {
            { BotLanguage.Persian, "❌ خرید ناموفق بود. لطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Purchase failed. Please try again." }
        },
        ["content.no_content"] = new()
        {
            { BotLanguage.Persian, "این مدل هنوز محتوایی آپلود نکرده است." },
            { BotLanguage.English, "This model hasn't uploaded any content yet." }
        },
        ["content.my_content.empty"] = new()
        {
            { BotLanguage.Persian, "شما هنوز محتوایی خریداری نکرده‌اید.\n\nاز منوی 'مشاهده مدل‌ها' برای خرید محتوا استفاده کنید." },
            { BotLanguage.English, "You haven't purchased any content yet.\n\nUse 'Browse Models' to purchase content." }
        },
        #endregion

        #region Alias
        ["alias.prompt"] = new()
        {
            { BotLanguage.Persian, "🏷️ تنظیم نام مستعار\n\nنام مستعار شما به جای نام اصلی در تمام بخش‌های کاربری نمایش داده خواهد شد.\n\nلطفاً نام مستعار دلخواه خود را وارد کنید (یا 'clear' برای حذف نام مستعار فعلی):" },
            { BotLanguage.English, "🏷️ Set Your Alias\n\nYour alias will be displayed instead of your real name in all user-facing areas.\n\nPlease enter your desired alias (or 'clear' to remove current alias):" }
        },
        ["alias.current"] = new()
        {
            { BotLanguage.Persian, "نام مستعار فعلی: {0}" },
            { BotLanguage.English, "Current alias: {0}" }
        },
        ["alias.set_success"] = new()
        {
            { BotLanguage.Persian, "✅ نام مستعار شما با موفقیت تنظیم شد: {0}" },
            { BotLanguage.English, "✅ Your alias has been set successfully: {0}" }
        },
        ["alias.cleared"] = new()
        {
            { BotLanguage.Persian, "✅ نام مستعار شما حذف شد. اکنون نام اصلی شما نمایش داده می‌شود." },
            { BotLanguage.English, "✅ Your alias has been cleared. Your real name will now be displayed." }
        },
        #endregion

        #region Model Terms & Conditions - Full Legal Text
        ["terms.title"] = new()
        {
            { BotLanguage.Persian, "📜 **شرایط و قوانین ثبت‌نام به عنوان مدل**\n\n" },
            { BotLanguage.English, "📜 **Model Registration Terms & Conditions**\n\n" }
        },
        ["terms.content.persian"] = new()
        {
            { BotLanguage.Persian, 
@"با عضویت به عنوان مدل در پلتفرم، شما با شرایط زیر موافقت می‌کنید:

━━━━━━━━━━━━━━━━━━━━━━━━

💰 کارمزد و درآمد:

• پلتفرم 15% از فروش محتوای شما را به عنوان کارمزد دریافت می‌کند
• 85% از درآمد به شما تعلق می‌گیرد
• مثال: برای هر 100 Stars فروش، 85 Stars به حساب شما واریز می‌شود

💳 هزینه انتقال:

• هزینه کارمزد انتقال وجه (Transfer Fee) به صورت مساوی بین مدل و پلتفرم تقسیم می‌شود
• مثال: اگر کارمزد انتقال 50 Stars باشد، 25 Stars از موجودی شما و 25 Stars توسط پلتفرم پرداخت می‌شود
• این هزینه شامل کارمزد بانکی، تبدیل ارز، و سایر هزینه‌های انتقال است

━━━━━━━━━━━━━━━━━━━━━━━━

💰 تسویه‌حساب:

• تسویه به صورت ماهانه انجام می‌شود
• حداقل موجودی برای برداشت: 1,000 Stars
• روش پرداخت: انتقال بانکی، کارت به کارت، یا سایر روش‌های توافقی
• تسویه تا 7 روز کاری پس از درخواست انجام می‌شود
• مبلغ نهایی پرداختی = (موجودی شما) - (50% هزینه انتقال)

━━━━━━━━━━━━━━━━━━━━━━━━

📸 قوانین محتوا:

• محتوای غیرقانونی، تهدیدآمیز، یا توهین‌آمیز ممنوع است
• محتوای حق نشر دار متعلق به دیگران ممنوع است
• محتوای مغایر با قوانین تلگرام ممنوع است
• قیمت‌گذاری منصفانه و متناسب با محتوا الزامی است
• پلتفرم حق حذف یا تعلیق محتوای نامناسب را دارد

━━━━━━━━━━━━━━━━━━━━━━━━

🛡️ مسئولیت‌ها:

• شما مسئول صحت اطلاعات ارائه شده هستید
• شما مسئول محتوایی که منتشر می‌کنید هستید
• پلتفرم مسئولیتی در قبال مشکلات قانونی ناشی از محتوای شما ندارد
• حفاظت از اطلاعات حساب کاربری به عهده شما است

━━━━━━━━━━━━━━━━━━━━━━━━

⚖️ سایر شرایط:

• پلتفرم حق تغییر شرایط را با اطلاع قبلی دارد
• نقض قوانین می‌تواند منجر به تعلیق یا حذف حساب شود
• شما می‌توانید هر زمان درخواست حذف حساب دهید
• پس از حذف حساب، موجودی باقی‌مانده پرداخت می‌شود

━━━━━━━━━━━━━━━━━━━━━━━━

📝 ثبت قانونی:

• تاریخ و ساعت دقیق پذیرش این شرایط ثبت و نگهداری می‌شود
• محتوای دقیق شرایطی که شما پذیرفته‌اید در سیستم ذخیره می‌شود
• این اطلاعات برای مسائل حقوقی احتمالی قابل استناد است

━━━━━━━━━━━━━━━━━━━━━━━━

📞 پشتیبانی:

در صورت هرگونه سؤال یا مشکل، با پشتیبانی تماس بگیرید.

━━━━━━━━━━━━━━━━━━━━━━━━

✅ با انتخاب 'قبول می‌کنم'، تأیید می‌کنید که:
• تمام شرایط بالا را خوانده و فهمیده‌اید
• با تمام موارد از جمله کارمزد 15% و تقسیم هزینه انتقال موافق هستید
• متعهد به رعایت قوانین پلتفرم هستید
• از ثبت این توافق در سیستم آگاه و موافق هستید

نسخه شرایط: 1.0
تاریخ: 2025-01-01" },
            { BotLanguage.English, "" }
        },
        ["terms.content.english"] = new()
        {
            { BotLanguage.Persian, "" },
            { BotLanguage.English,
@"By joining as a model on the platform, you agree to the following terms:

━━━━━━━━━━━━━━━━━━━━━━━━

💰 Commission and Revenue:

• The platform receives 15% commission from your content sales
• 85% of revenue belongs to you
• Example: For every 100 Stars sale, 85 Stars will be deposited to your account

💳 Transfer Fees:

• Transfer fees are split equally between the model and the platform
• Example: If transfer fee is 50 Stars, 25 Stars from your balance and 25 Stars by the platform will be paid
• This fee includes bank charges, currency conversion, and other transfer costs

━━━━━━━━━━━━━━━━━━━━━━━━

💰 Settlement:

• Settlement is done monthly
• Minimum balance for withdrawal: 1,000 Stars
• Payment method: Bank transfer, card to card, or other agreed methods
• Settlement is completed within 7 business days after request
• Final payment amount = (Your balance) - (50% transfer fee)

━━━━━━━━━━━━━━━━━━━━━━━━

📸 Content Rules:

• Illegal, threatening, or offensive content is prohibited
• Copyrighted content belonging to others is prohibited
• Content violating Telegram rules is prohibited
• Fair and appropriate pricing is mandatory
• The platform reserves the right to remove or suspend inappropriate content

━━━━━━━━━━━━━━━━━━━━━━━━

🛡️ Responsibilities:

• You are responsible for the accuracy of provided information
• You are responsible for the content you publish
• The platform has no liability for legal issues arising from your content
• Protecting your account information is your responsibility

━━━━━━━━━━━━━━━━━━━━━━━━

⚖️ Other Terms:

• The platform reserves the right to change terms with prior notice
• Violation of rules may result in account suspension or deletion
• You can request account deletion at any time
• After account deletion, remaining balance will be paid

━━━━━━━━━━━━━━━━━━━━━━━━

📝 Legal Registration:

• The exact date and time of accepting these terms will be recorded and maintained
• The exact content of the terms you accepted will be stored in the system
• This information can be referenced for potential legal matters

━━━━━━━━━━━━━━━━━━━━━━━━

📞 Support:

For any questions or issues, contact support.

━━━━━━━━━━━━━━━━━━━━━━━━

✅ By selecting 'I Accept', you confirm that:
• You have read and understood all the above terms
• You agree to all terms including 15% commission and transfer fee split
• You are committed to following the platform rules
• You are aware of and agree to this agreement being recorded in the system

Terms version: 1.0
Date: 2025-01-01" }
        },
        ["terms.accept"] = new()
        {
            { BotLanguage.Persian, "✅ قبول می‌کنم" },
            { BotLanguage.English, "✅ I Accept" }
        },
        ["terms.decline"] = new()
        {
            { BotLanguage.Persian, "❌ نمی‌پذیرم" },
            { BotLanguage.English, "❌ I Decline" }
        },
        ["terms.declined"] = new()
        {
            { BotLanguage.Persian, "شما شرایط را نپذیرفتید. بدون پذیرش شرایط نمی‌توانید به عنوان مدل ثبت‌نام کنید." },
            { BotLanguage.English, "You declined the terms. You cannot register as a model without accepting the terms." }
        },
        #endregion
        
        #region Content & Purchase
        ["content.not_found"] = new()
        {
            { BotLanguage.Persian, "📸 محتوا یافت نشد یا برای خرید در دسترس نیست." },
            { BotLanguage.English, "📸 Content not found or not available for purchase." }
        },
        ["content.photo_not_found"] = new()
        {
            { BotLanguage.Persian, "📸 عکس یافت نشد." },
            { BotLanguage.English, "📸 Photo not found." }
        },
        ["content.not_for_sale"] = new()
        {
            { BotLanguage.Persian, "❌ این محتوا برای فروش در دسترس نیست." },
            { BotLanguage.English, "❌ This content is not available for sale." }
        },
        ["purchase.success"] = new()
        {
            { BotLanguage.Persian, "✅ خرید با موفقیت انجام شد!" },
            { BotLanguage.English, "✅ Purchase successful!" }
        },
        ["purchase.test_success"] = new()
        {
            { BotLanguage.Persian, "✅ خرید آزمایشی عکس موفق بود!\n\n🔍 اطلاعات خرید:\n• کاربر: {0}\n• عکس: {1}\n• قیمت: {2} ستاره\n• تاریخ: {3}" },
            { BotLanguage.English, "✅ Test photo purchase successful!\n\n🔍 Purchase Details:\n• User: {0}\n• Photo: {1}\n• Price: {2} Stars\n• Date: {3}" }
        },
        ["purchase.failed"] = new()
        {
            { BotLanguage.Persian, "❌ خرید ناموفق بود: {0}" },
            { BotLanguage.English, "❌ Purchase failed: {0}" }
        },
        ["purchase.invoice_failed"] = new()
        {
            { BotLanguage.Persian, "❌ ایجاد صورتحساب ناموفق بود. لطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Failed to create invoice. Please try again later." }
        },
        ["purchase.test_failed"] = new()
        {
            { BotLanguage.Persian, "❌ ایجاد خرید آزمایشی ناموفق بود." },
            { BotLanguage.English, "❌ Failed to create test purchase." }
        },
        #endregion
        
        #region Upload & Content Management
        ["upload.prompt.photo"] = new()
        {
            { BotLanguage.Persian, "📸 لطفاً عکس خود را ارسال کنید:" },
            { BotLanguage.English, "📸 Please send your photo:" }
        },
        ["upload.prompt.caption"] = new()
        {
            { BotLanguage.Persian, "✍️ لطفاً توضیحات این محتوا را وارد کنید:\n\n💡 این متن برای کاربران نمایش داده می‌شود." },
            { BotLanguage.English, "✍️ Please enter the caption for this content:\n\n💡 This text will be displayed to users." }
        },
        ["upload.prompt.price"] = new()
        {
            { BotLanguage.Persian, "💰 لطفاً قیمت را به ستاره تلگرام وارد کنید:\n\n💡 مثال: 100" },
            { BotLanguage.English, "💰 Please enter the price in Telegram Stars:\n\n💡 Example: 100" }
        },
        ["upload.success"] = new()
        {
            { BotLanguage.Persian, "✅ محتوا با موفقیت آپلود شد!\n\n📊 آماده برای فروش است." },
            { BotLanguage.English, "✅ Content uploaded successfully!\n\n📊 Ready for sale." }
        },
        ["content.delete_success"] = new()
        {
            { BotLanguage.Persian, "✅ محتوا با موفقیت حذف شد!\n\n🗑️ دیگر برای کاربران قابل مشاهده نیست." },
            { BotLanguage.English, "✅ Content deleted successfully!\n\n🗑️ No longer visible to users." }
        },
        ["content.edit_caption_prompt"] = new()
        {
            { BotLanguage.Persian, "✍️ لطفاً توضیحات جدید این محتوا را ارسال کنید:\n\n📝 توضیحات فعلی:\n{0}" },
            { BotLanguage.English, "✍️ Please reply with the new caption for this content:\n\n📝 Current caption:\n{0}" }
        },
        ["content.edit_price_prompt"] = new()
        {
            { BotLanguage.Persian, "💰 لطفاً قیمت جدید را به ستاره تلگرام ارسال کنید:\n\n💵 قیمت فعلی: {0} ستاره\n\n💡 مثال: 150" },
            { BotLanguage.English, "💰 Please reply with the new price in Telegram Stars:\n\n💵 Current price: {0} Stars\n\n💡 Example: 150" }
        },
        #endregion
        
        #region Model Registration
        ["model.registration_success"] = new()
        {
            { BotLanguage.Persian, "✅ درخواست ثبت‌نام مدل با موفقیت ارسال شد!\n\n⏳ لطفاً منتظر بررسی و تایید ادمین باشید.\n\n📧 پس از تایید، اطلاع‌رسانی خواهید شد." },
            { BotLanguage.English, "✅ Model registration submitted successfully!\n\n⏳ Please wait for admin review and approval.\n\n📧 You will be notified after approval." }
        },
        ["model.reapplication_success"] = new()
        {
            { BotLanguage.Persian, "✅ درخواست جدید با موفقیت ارسال شد!\n\n⏳ لطفاً منتظر بررسی ادمین باشید." },
            { BotLanguage.English, "✅ New application submitted successfully!\n\n⏳ Please wait for admin review." }
        },
        #endregion
        
        #region User & Validation
        ["user.not_found"] = new()
        {
            { BotLanguage.Persian, "❌ کاربر یافت نشد. لطفاً ابتدا /start را ارسال کنید." },
            { BotLanguage.English, "❌ User not found. Please send /start first." }
        },
        ["common.invalid_id"] = new()
        {
            { BotLanguage.Persian, "❌ فرمت شناسه نامعتبر است." },
            { BotLanguage.English, "❌ Invalid ID format." }
        },
        ["common.invalid_photo_id"] = new()
        {
            { BotLanguage.Persian, "❌ فرمت شناسه عکس نامعتبر است. لطفاً از /photos یک شناسه معتبر استفاده کنید." },
            { BotLanguage.English, "❌ Invalid photo ID format. Please use a valid photo ID from /photos" }
        },
        #endregion
        
        #region Content Delivery
        ["delivery.error.general"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در ارسال عکس.\n\n{0}" },
            { BotLanguage.English, "❌ Error sending photo.\n\n{0}" }
        },
        ["delivery.error.mtproto"] = new()
        {
            { BotLanguage.Persian, "⚠️ سرویس MTProto به درستی پیکربندی یا احراز هویت نشده است.\n\nلطفاً با ادمین تماس بگیرید تا MTProto را با `/mtproto_setup` پیکربندی کند." },
            { BotLanguage.English, "⚠️ MTProto service is not properly configured or authenticated.\n\nPlease contact the admin to configure MTProto using `/mtproto_setup`." }
        },
        ["delivery.failed"] = new()
        {
            { BotLanguage.Persian, "❌ ارسال محتوا ناموفق بود. لطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Failed to send content. Please try again later." }
        },
        ["delivery.contact_error"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در ارسال اطلاعات تماس: {0}" },
            { BotLanguage.English, "❌ Error sending contact: {0}" }
        },
        #endregion
        
        #region Admin Setup
        ["admin.setup.api_id"] = new()
        {
            { BotLanguage.Persian, "🚀 بیایید شروع کنیم! لطفاً **API ID** خود را ارسال کنید:" },
            { BotLanguage.English, "🚀 Let's start! Please send your **API ID**:" }
        },
        ["admin.setup.setting_prompt"] = new()
        {
            { BotLanguage.Persian, "📝 لطفاً مقدار جدید این تنظیمات را ارسال کنید:\n\n📌 تنظیم: {0}\n📖 توضیحات: {1}" },
            { BotLanguage.English, "📝 Please send the new value for this setting:\n\n📌 Setting: {0}\n📖 Description: {1}" }
        },
        ["admin.setup.subscription_prompt"] = new()
        {
            { BotLanguage.Persian, "📝 لطفاً جزئیات اشتراک را به این فرمت ارسال کنید:\n\n**قالب:** نام - مدت (روز) - قیمت (ستاره)\n**مثال:** Premium - 30 - 500" },
            { BotLanguage.English, "📝 Please reply with the subscription details in this format:\n\n**Format:** Name - Duration (days) - Price (Stars)\n**Example:** Premium - 30 - 500" }
        },
        #endregion
        
        #region Subscribe
        ["subscribe.success"] = new()
        {
            { BotLanguage.Persian, "✅ با موفقیت مشترک {0} شدید!\n\n🎉 اکنون می‌توانید تمام محتوای این مدل را مشاهده کنید." },
            { BotLanguage.English, "✅ Successfully subscribed to {0}!\n\n🎉 You can now view all content from this model." }
        },
        #endregion
        
        #region Generic Errors
        ["error.loading_photos"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری عکس‌ها: {0}" },
            { BotLanguage.English, "❌ Error loading photos: {0}" }
        },
        ["error.loading_content"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری محتوا: {0}" },
            { BotLanguage.English, "❌ Error loading content: {0}" }
        },
        ["error.loading_your_content"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری محتوای شما: {0}" },
            { BotLanguage.English, "❌ Error loading your content: {0}" }
        },
        ["error.loading_models"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری مدل‌ها: {0}" },
            { BotLanguage.English, "❌ Error loading models: {0}" }
        },
        ["error.loading_dashboard"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری داشبورد: {0}" },
            { BotLanguage.English, "❌ Error loading dashboard: {0}" }
        },
        ["error.loading_subscriptions"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری اشتراک‌ها: {0}" },
            { BotLanguage.English, "❌ Error loading subscriptions: {0}" }
        },
        ["error.loading_admin_panel"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری پنل ادمین: {0}" },
            { BotLanguage.English, "❌ Error loading admin panel: {0}" }
        },
        ["error.loading_settings"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری تنظیمات: {0}" },
            { BotLanguage.English, "❌ Error loading settings: {0}" }
        },
        ["error.loading_demo"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در بارگذاری محتوای نمایشی: {0}" },
            { BotLanguage.English, "❌ Error loading demo content: {0}" }
        },
        ["error.viewing_demo"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در مشاهده محتوای نمایشی: {0}" },
            { BotLanguage.English, "❌ Error viewing demo content: {0}" }
        },
        ["error.viewing_model"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در مشاهده مدل: {0}" },
            { BotLanguage.English, "❌ Error viewing model: {0}" }
        },
        ["error.viewing_model_content"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در مشاهده محتوای مدل: {0}" },
            { BotLanguage.English, "❌ Error viewing model content: {0}" }
        },
        ["error.subscribing"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در اشتراک: {0}" },
            { BotLanguage.English, "❌ Error subscribing: {0}" }
        },
        ["error.approving_model"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در تایید مدل: {0}" },
            { BotLanguage.English, "❌ Error approving model: {0}" }
        },
        ["error.rejecting_model"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در رد مدل: {0}" },
            { BotLanguage.English, "❌ Error rejecting model: {0}" }
        },
        ["error.reapplication"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در ارسال درخواست جدید: {0}" },
            { BotLanguage.English, "❌ Error submitting new application: {0}" }
        },
        ["error.deleting_content"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در حذف محتوا: {0}" },
            { BotLanguage.English, "❌ Error deleting content: {0}" }
        },
        ["error.become_model"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در ثبت‌نام مدل: {0}" },
            { BotLanguage.English, "❌ Error registering model: {0}" }
        },
        ["error.become_model_flow"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در فرآیند ثبت‌نام مدل: {0}" },
            { BotLanguage.English, "❌ Error in become model flow: {0}" }
        },
        ["error.single_model_enable"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در فعال‌سازی حالت تک مدل: {0}" },
            { BotLanguage.English, "❌ Error enabling Single Model Mode: {0}" }
        },
        ["error.single_model_disable"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در غیرفعال‌سازی حالت تک مدل: {0}" },
            { BotLanguage.English, "❌ Error disabling Single Model Mode: {0}" }
        },
        ["error.mtproto_setup"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در راه‌اندازی MTProto: {0}" },
            { BotLanguage.English, "❌ Error in MTProto setup: {0}" }
        },
        ["error.setting_language"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در تنظیم زبان: {0}" },
            { BotLanguage.English, "❌ Error setting language: {0}" }
        },
        ["error.generic"] = new()
        {
            { BotLanguage.Persian, "❌ خطا: {0}" },
            { BotLanguage.English, "❌ Error: {0}" }
        },
        ["error.processing_input"] = new()
        {
            { BotLanguage.Persian, "❌ خطا در پردازش ورودی شما: {0}\n\nلطفاً دوباره تلاش کنید." },
            { BotLanguage.English, "❌ Error processing your input: {0}\n\nPlease try again." }
        },
        #endregion
        
        #region Models Browse
        ["models.none_available"] = new()
        {
            { BotLanguage.Persian, "📭 هنوز مدلی موجود نیست.\n\n💡 می‌خواهید سازنده محتوا شوید؟" },
            { BotLanguage.English, "📭 No models available yet.\n\n💡 Want to become a content creator?" }
        },
        ["models.available_count"] = new()
        {
            { BotLanguage.Persian, "👥 مدل‌های موجود ({0}):\n\n" },
            { BotLanguage.English, "👥 Available Models ({0}):\n\n" }
        },
        ["models.subscribers"] = new()
        {
            { BotLanguage.Persian, "   👥 مشترکین: {0}" },
            { BotLanguage.English, "   👥 Subscribers: {0}" }
        },
        ["models.content_count"] = new()
        {
            { BotLanguage.Persian, "   📸 محتوا: {0} عکس پریمیوم" },
            { BotLanguage.English, "   📸 Content: {0} premium photos" }
        },
        ["models.subscription_info"] = new()
        {
            { BotLanguage.Persian, "   💳 اشتراک: {0} ستاره / {1} روز" },
            { BotLanguage.English, "   💳 Subscription: {0} stars / {1} days" }
        },
        ["models.view_button"] = new()
        {
            { BotLanguage.Persian, "👁️ مشاهده {0}" },
            { BotLanguage.English, "👁️ View {0}" }
        },
        ["models.become_model_button"] = new()
        {
            { BotLanguage.Persian, "⭐ مدل شوید" },
            { BotLanguage.English, "⭐ Become a Model" }
        },
        #endregion
        
        #region Model Status & Info
        ["model.status.new_content_creator"] = new()
        {
            { BotLanguage.Persian, "🆕 سازنده محتوای جدید" },
            { BotLanguage.English, "🆕 New content creator" }
        },
        #endregion
        
        #region Admin Panel
        ["admin.pending_approvals.title"] = new()
        {
            { BotLanguage.Persian, "📋 درخواست‌های مدل در انتظار تایید: {0}" },
            { BotLanguage.English, "📋 Pending Model Approvals: {0}" }
        },
        ["admin.pending_approvals.none"] = new()
        {
            { BotLanguage.Persian, "✅ هیچ درخواستی در انتظار تایید نیست." },
            { BotLanguage.English, "✅ No pending approvals at this time." }
        },
        ["admin.button.refresh"] = new()
        {
            { BotLanguage.Persian, "🔄 بروزرسانی" },
            { BotLanguage.English, "🔄 Refresh" }
        },
        ["admin.settings.title"] = new()
        {
            { BotLanguage.Persian, "⚙️ **تنظیمات پلتفرم**" },
            { BotLanguage.English, "⚙️ **Platform Settings**" }
        },
        ["admin.settings.description"] = new()
        {
            { BotLanguage.Persian, "پیکربندی اعتبارنامه‌های MTProto و تنظیمات پلتفرم.\n\n⚠️ توجه: Bot Token باید در appsettings.json پیکربندی شود\n\nبرای ویرایش روی یک تنظیم کلیک کنید:" },
            { BotLanguage.English, "Configure MTProto credentials and platform settings.\n\n⚠️ Note: Bot token must be configured in appsettings.json\n\nClick on a setting to edit it:" }
        },
        #endregion
        
        #region Upload Content
        ["upload.title"] = new()
        {
            { BotLanguage.Persian, "📤 آپلود محتوای پریمیوم" },
            { BotLanguage.English, "📤 Upload Premium Content" }
        },
        ["upload.instructions"] = new()
        {
            { BotLanguage.Persian, "یک عکس یا ویدیو که می‌خواهید بفروشید برای من ارسال کنید.\n\nبعد از آپلود، از شما خواسته می‌شود:\n• قیمت (به ستاره تلگرام)\n• توضیحات (توضیحات اختیاری)\n\nاین محتوا برای خرید یا برای مشترکین در دسترس خواهد بود.\n\n📸 اکنون رسانه خود را ارسال کنید:" },
            { BotLanguage.English, "Send me a photo or video that you want to sell.\n\nAfter uploading, I'll ask you to set:\n• Price (in Telegram Stars)\n• Caption (optional description)\n\nThis content will be available for purchase or to subscribers.\n\n📸 Send your media now:" }
        },
        #endregion
        
        #region My Content
        ["content.my_content.title"] = new()
        {
            { BotLanguage.Persian, "📂 محتوای شما:" },
            { BotLanguage.English, "📂 Your Available Content:" }
        },
        ["content.view_button"] = new()
        {
            { BotLanguage.Persian, "👁️ مشاهده" },
            { BotLanguage.English, "👁️ View" }
        },
        ["content.subscription_label"] = new()
        {
            { BotLanguage.Persian, "    💳 اشتراک" },
            { BotLanguage.English, "    💳 Subscription" }
        },
        ["content.demo_label"] = new()
        {
            { BotLanguage.Persian, " 🎁 محتوای دمو" },
            { BotLanguage.English, " 🎁 Demo Content" }
        },
        ["content.view_instruction"] = new()
        {
            { BotLanguage.Persian, "💡 برای دریافت عکس با تایمر خودکار حذف روی 'مشاهده' کلیک کنید." },
            { BotLanguage.English, "💡 Click 'View' to receive the photo with self-destruct timer." }
        },
        #endregion
        
        #region Model Profile View
        ["model.profile.not_found"] = new()
        {
            { BotLanguage.Persian, "❌ مدل یافت نشد یا در دسترس نیست." },
            { BotLanguage.English, "❌ Model not found or not available." }
        },
        ["model.profile.statistics"] = new()
        {
            { BotLanguage.Persian, "📈 آمار:" },
            { BotLanguage.English, "📈 Statistics:" }
        },
        ["model.profile.subscribers"] = new()
        {
            { BotLanguage.Persian, "👥 مشترکین: {0}" },
            { BotLanguage.English, "👥 Subscribers: {0}" }
        },
        ["model.profile.content"] = new()
        {
            { BotLanguage.Persian, "📸 محتوا: {0} عکس پریمیوم" },
            { BotLanguage.English, "📸 Content: {0} premium photos" }
        },
        ["model.profile.demo_content"] = new()
        {
            { BotLanguage.Persian, "🎁 محتوای دمو: {0} پیش‌نمایش رایگان" },
            { BotLanguage.English, "🎁 Demo Content: {0} free preview(s)" }
        },
        ["model.profile.view_demo"] = new()
        {
            { BotLanguage.Persian, "🎁 مشاهده دمو رایگان" },
            { BotLanguage.English, "🎁 View Free Demo" }
        },
        ["model.profile.subscribed"] = new()
        {
            { BotLanguage.Persian, "✅ شما مشترک هستید!" },
            { BotLanguage.English, "✅ You are subscribed!" }
        },
        ["model.profile.view_my_content"] = new()
        {
            { BotLanguage.Persian, "📂 مشاهده محتوای من" },
            { BotLanguage.English, "📂 View My Content" }
        },
        ["model.profile.subscribe_offer"] = new()
        {
            { BotLanguage.Persian, "💰 اشتراک {0} ستاره / {1} روز\nدسترسی به تمام محتوا!\n" },
            { BotLanguage.English, "💰 Subscribe for {0} stars/{1} days\nGet access to all content!\n" }
        },
        ["model.profile.subscribe_button"] = new()
        {
            { BotLanguage.Persian, "💳 اشتراک ({0} ستاره)" },
            { BotLanguage.English, "💳 Subscribe ({0} stars)" }
        },
        ["model.profile.available_photos"] = new()
        {
            { BotLanguage.Persian, "📸 عکس‌های موجود:" },
            { BotLanguage.English, "📸 Available Photos:" }
        },
        ["model.profile.buy_button"] = new()
        {
            { BotLanguage.Persian, "🛒 خرید: {0}" },
            { BotLanguage.English, "🛒 Buy: {0}" }
        },
        ["model.profile.back_to_models"] = new()
        {
            { BotLanguage.Persian, "⬅️ بازگشت به لیست مدل‌ها" },
            { BotLanguage.English, "⬅️ Back to Models" }
        },
        #endregion
        
        #region Content Statistics
        ["content_stats.title"] = new()
        {
            { BotLanguage.Persian, "📊 آمار محتوا\n\n" },
            { BotLanguage.English, "📊 Content Statistics\n\n" }
        },
        ["content_stats.no_content"] = new()
        {
            { BotLanguage.Persian, "📊 هنوز آماری موجود نیست.\n\nبرای مشاهده آمار دقیق، محتوا آپلود کنید!" },
            { BotLanguage.English, "📊 No content statistics available yet.\n\nUpload some content to see detailed statistics!" }
        },
        ["content_stats.views"] = new()
        {
            { BotLanguage.Persian, "   👁️ بازدید: {0}" },
            { BotLanguage.English, "   👁️ Views: {0}" }
        },
        ["content_stats.purchases"] = new()
        {
            { BotLanguage.Persian, "   🛒 خرید: {0}" },
            { BotLanguage.English, "   🛒 Purchases: {0}" }
        },
        ["content_stats.revenue"] = new()
        {
            { BotLanguage.Persian, "   💰 درآمد: {0:N0} ⭐️" },
            { BotLanguage.English, "   💰 Revenue: {0:N0} ⭐️" }
        },
        ["content_stats.conversion"] = new()
        {
            { BotLanguage.Persian, "   📈 نرخ تبدیل: {0:F2}%" },
            { BotLanguage.English, "   📈 Conversion: {0:F2}%" }
        },
        ["content_stats.more_items"] = new()
        {
            { BotLanguage.Persian, "_... و {0} مورد دیگر_\n" },
            { BotLanguage.English, "_... and {0} more items_\n" }
        },
        ["content_stats.not_model"] = new()
        {
            { BotLanguage.Persian, "❌ شما به عنوان مدل ثبت‌نام نکرده‌اید." },
            { BotLanguage.English, "❌ You are not registered as a model." }
        },
        ["top_content.title"] = new()
        {
            { BotLanguage.Persian, "🏆 محتوای برتر\n\n" },
            { BotLanguage.English, "🏆 Top Performing Content\n\n" }
        },
        ["top_content.all_time"] = new()
        {
            { BotLanguage.Persian, "🌟 برترین‌های همیشه:" },
            { BotLanguage.English, "🌟 All Time Top 10:" }
        },
        ["top_content.this_year"] = new()
        {
            { BotLanguage.Persian, "📆 برترین‌های امسال:" },
            { BotLanguage.English, "📆 This Year Top 10:" }
        },
        ["top_content.this_month"] = new()
        {
            { BotLanguage.Persian, "📅 برترین‌های این ماه:" },
            { BotLanguage.English, "📅 This Month Top 10:" }
        },
        ["top_content.no_data"] = new()
        {
            { BotLanguage.Persian, "📊 هنوز داده‌ای موجود نیست.\n\nبعد از فروش محتوا، آمار برترین‌ها اینجا نمایش داده می‌شود!" },
            { BotLanguage.English, "📊 No data available yet.\n\nOnce you start selling content, top performers will appear here!" }
        }
        #endregion
    };
    
    public LocalizationService(
        IPlatformSettingsRepository platformSettingsRepository,
        IUnitOfWork unitOfWork)
    {
        _platformSettingsRepository = platformSettingsRepository ?? throw new ArgumentNullException(nameof(platformSettingsRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }
    
    public async Task<BotLanguage> GetBotLanguageAsync(CancellationToken cancellationToken = default)
    {
        var languageValue = await _platformSettingsRepository.GetValueAsync(LanguageSettingKey, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(languageValue))
        {
            return BotLanguage.Persian;
        }
        
        return Enum.TryParse<BotLanguage>(languageValue, out var language) 
            ? language 
            : BotLanguage.Persian;
    }
    
    public async Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var language = await GetBotLanguageAsync(cancellationToken);
        return GetString(key, language);
    }
    
    public async Task<string> GetStringAsync(string key, params object[] args)
    {
        var language = await GetBotLanguageAsync();
        var template = GetString(key, language);
        return args.Length > 0 ? string.Format(template, args) : template;
    }
    
    private string GetString(string key, BotLanguage language)
    {
        if (_strings.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(language, out var text))
            {
                return text;
            }
            
            // Fallback to Persian if translation not found
            if (translations.TryGetValue(BotLanguage.Persian, out var fallback))
            {
                return fallback;
            }
        }
        
        // Return key if translation not found
        return key;
    }
    
    public async Task SetBotLanguageAsync(BotLanguage language, CancellationToken cancellationToken = default)
    {
        await _platformSettingsRepository.SetValueAsync(
            LanguageSettingKey,
            language.ToString(),
            "Bot language (Persian or English)",
            isSecret: false,
            cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
