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
