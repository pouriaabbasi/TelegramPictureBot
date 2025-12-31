using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Application.Services;

public class ModelTermsService : IModelTermsService
{
    private readonly IModelTermsAcceptanceRepository _termsAcceptanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Current terms version
    private const string CURRENT_TERMS_VERSION = "1.0";

    // Full terms content
    private const string TERMS_CONTENT = @"📜 شرایط و قوانین ثبت‌نام به عنوان مدل

با عضویت به عنوان مدل در پلتفرم، شما با شرایط زیر موافقت می‌کنید:

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

✅ با انتخاب ""قبول و ادامه""، تأیید می‌کنید که:
• تمام شرایط بالا را خوانده و فهمیده‌اید
• با تمام موارد از جمله کارمزد 15% و تقسیم هزینه انتقال موافق هستید
• متعهد به رعایت قوانین پلتفرم هستید
• از ثبت این توافق در سیستم آگاه و موافق هستید

نسخه شرایط: 1.0
تاریخ: 2025-01-01";

    public ModelTermsService(
        IModelTermsAcceptanceRepository termsAcceptanceRepository,
        IUnitOfWork unitOfWork)
    {
        _termsAcceptanceRepository = termsAcceptanceRepository;
        _unitOfWork = unitOfWork;
    }

    public string GetCurrentTermsVersion()
    {
        return CURRENT_TERMS_VERSION;
    }

    public string GetTermsContent()
    {
        return TERMS_CONTENT;
    }

    public async Task<bool> HasAcceptedTermsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var latestAcceptance = await _termsAcceptanceRepository.GetLatestAcceptanceAsync(modelId, cancellationToken);
        return latestAcceptance != null;
    }

    public async Task<bool> HasAcceptedLatestTermsAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _termsAcceptanceRepository.HasAcceptedLatestTermsAsync(
            modelId, 
            CURRENT_TERMS_VERSION, 
            cancellationToken);
    }

    public async Task<ModelTermsAcceptance> RecordAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        // Mark all previous acceptances as old
        await _termsAcceptanceRepository.MarkPreviousAsOldVersionAsync(modelId, cancellationToken);

        // Create new acceptance record
        var acceptance = new ModelTermsAcceptance(
            modelId,
            CURRENT_TERMS_VERSION,
            TERMS_CONTENT);

        await _termsAcceptanceRepository.AddAsync(acceptance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return acceptance;
    }

    public async Task<ModelTermsAcceptance?> GetLatestAcceptanceAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _termsAcceptanceRepository.GetLatestAcceptanceAsync(modelId, cancellationToken);
    }

    public async Task<IEnumerable<ModelTermsAcceptance>> GetAcceptanceHistoryAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _termsAcceptanceRepository.GetModelAcceptanceHistoryAsync(modelId, cancellationToken);
    }
}
