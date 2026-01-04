using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Application.Services;

public class ModelTermsService : IModelTermsService
{
    private readonly IModelTermsAcceptanceRepository _termsAcceptanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocalizationService _localizationService;

    // Current terms version
    private const string CURRENT_TERMS_VERSION = "1.0";

    public ModelTermsService(
        IModelTermsAcceptanceRepository termsAcceptanceRepository,
        IUnitOfWork unitOfWork,
        ILocalizationService localizationService)
    {
        _termsAcceptanceRepository = termsAcceptanceRepository;
        _unitOfWork = unitOfWork;
        _localizationService = localizationService;
    }

    public string GetCurrentTermsVersion()
    {
        return CURRENT_TERMS_VERSION;
    }

    public async Task<string> GetTermsContentAsync(CancellationToken cancellationToken = default)
    {
        var language = await _localizationService.GetBotLanguageAsync(cancellationToken);
        var key = language == BotLanguage.Persian 
            ? "terms.content.persian" 
            : "terms.content.english";
        
        var title = await _localizationService.GetStringAsync("terms.title", cancellationToken);
        var content = await _localizationService.GetStringAsync(key, cancellationToken);
        
        return title + content;
    }

    public string GetTermsContent()
    {
        // Legacy method for backward compatibility - returns Persian by default
        return GetTermsContentAsync().GetAwaiter().GetResult();
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

        // Get the current terms content in the current language
        var termsContent = await GetTermsContentAsync(cancellationToken);

        // Create new acceptance record
        var acceptance = new ModelTermsAcceptance(
            modelId,
            CURRENT_TERMS_VERSION,
            termsContent);

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

