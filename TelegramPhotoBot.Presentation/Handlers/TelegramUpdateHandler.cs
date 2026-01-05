using Telegram.Bot.Types; // Added for CallbackQuery
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Application.Services;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Presentation.DTOs;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Main handler for Telegram bot updates
/// </summary>
public partial class TelegramUpdateHandler
{
    private readonly IUserService _userService;
    private readonly IContentAuthorizationService _contentAuthorizationService;
    private readonly IContentDeliveryService _contentDeliveryService;
    private readonly IPhotoPurchaseService _photoPurchaseService;
    private readonly ITelegramBotService _telegramBotService;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    
    // Marketplace services
    private readonly IAuthorizationService _authorizationService;
    private readonly IModelService _modelService;
    private readonly IModelDiscoveryService _modelDiscoveryService;
    private readonly IModelSubscriptionService _modelSubscriptionService;
    private readonly IModelRepository _modelRepository;
    private readonly IUserStateRepository _userStateRepository;
    private readonly IDemoAccessRepository _demoAccessRepository;
    private readonly IViewHistoryRepository _viewHistoryRepository;
    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly IMtProtoService _mtProtoService;
    private readonly IMtProtoAccessTokenService _mtProtoAccessTokenService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISingleModelModeService _singleModelModeService;
    private readonly IRevenueAnalyticsService _revenueAnalyticsService;
    private readonly IModelTermsService _modelTermsService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;

    public TelegramUpdateHandler(
        IUserService userService,
        IContentAuthorizationService contentAuthorizationService,
        IContentDeliveryService contentDeliveryService,
        IPhotoPurchaseService photoPurchaseService,
        ITelegramBotService telegramBotService,
        IPhotoRepository photoRepository,
        IPurchaseRepository purchaseRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IAuthorizationService authorizationService,
        IModelService modelService,
        IModelDiscoveryService modelDiscoveryService,
        IModelSubscriptionService modelSubscriptionService,
        IModelRepository modelRepository,
        IUserStateRepository userStateRepository,
        IDemoAccessRepository demoAccessRepository,
        IViewHistoryRepository viewHistoryRepository,
        IPlatformSettingsRepository platformSettingsRepository,
        IMtProtoService mtProtoService,
        IMtProtoAccessTokenService mtProtoAccessTokenService,
        IServiceProvider serviceProvider,
        ISingleModelModeService singleModelModeService,
        IModelTermsService modelTermsService,
        IRevenueAnalyticsService revenueAnalyticsService,
        ILocalizationService localizationService,
        INotificationService notificationService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _contentAuthorizationService = contentAuthorizationService ?? throw new ArgumentNullException(nameof(contentAuthorizationService));
        _contentDeliveryService = contentDeliveryService ?? throw new ArgumentNullException(nameof(contentDeliveryService));
        _photoPurchaseService = photoPurchaseService ?? throw new ArgumentNullException(nameof(photoPurchaseService));
        _telegramBotService = telegramBotService ?? throw new ArgumentNullException(nameof(telegramBotService));
        _photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
        _purchaseRepository = purchaseRepository ?? throw new ArgumentNullException(nameof(purchaseRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _modelDiscoveryService = modelDiscoveryService ?? throw new ArgumentNullException(nameof(modelDiscoveryService));
        _modelSubscriptionService = modelSubscriptionService ?? throw new ArgumentNullException(nameof(modelSubscriptionService));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _userStateRepository = userStateRepository ?? throw new ArgumentNullException(nameof(userStateRepository));
        _demoAccessRepository = demoAccessRepository ?? throw new ArgumentNullException(nameof(demoAccessRepository));
        _viewHistoryRepository = viewHistoryRepository ?? throw new ArgumentNullException(nameof(viewHistoryRepository));
        _platformSettingsRepository = platformSettingsRepository ?? throw new ArgumentNullException(nameof(platformSettingsRepository));
        _mtProtoService = mtProtoService ?? throw new ArgumentNullException(nameof(mtProtoService));
        _mtProtoAccessTokenService = mtProtoAccessTokenService ?? throw new ArgumentNullException(nameof(mtProtoAccessTokenService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _singleModelModeService = singleModelModeService ?? throw new ArgumentNullException(nameof(singleModelModeService));
        _modelTermsService = modelTermsService ?? throw new ArgumentNullException(nameof(modelTermsService));
        _revenueAnalyticsService = revenueAnalyticsService ?? throw new ArgumentNullException(nameof(revenueAnalyticsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    /// <summary>
    /// Handles a message update from Telegram
    /// </summary>
    public async Task HandleMessageAsync(TelegramMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📨 HandleMessageAsync called. Text: {message.Text ?? "(no text)"}, From: {message.From.Id}, ChatId: {message.ChatId}");
            
            // Get or create user
            var userInfo = new TelegramUserInfo
            {
                Id = message.From.Id,
                IsBot = message.From.IsBot,
                FirstName = message.From.FirstName,
                LastName = message.From.LastName,
                Username = message.From.Username,
                LanguageCode = message.From.LanguageCode
            };

            Console.WriteLine($"👤 Getting or creating user: {userInfo.Id}");
            var user = await _userService.GetOrCreateUserAsync(userInfo, cancellationToken);
            Console.WriteLine($"✅ User retrieved/created: {user.Id}");

        // Get full user entity for role checking
        var userEntity = await _userRepository.GetByIdAsync(user.Id, cancellationToken);

        // Check if user has an active state (ongoing workflow)
        Console.WriteLine($"🔍 Checking for active user state for user {user.Id}...");
        var userState = await _userStateRepository.GetActiveStateAsync(user.Id, cancellationToken);
        
        if (userState != null)
        {
            Console.WriteLine($"📋 User state found: {userState.StateType}, Expired: {userState.IsExpired()}, Data: {userState.StateData ?? "(null)"}");
            
            if (!userState.IsExpired())
            {
                Console.WriteLine($"✅ User has active state: {userState.StateType}, calling HandleStateBasedInputAsync...");
                // Handle state-based input
                await HandleStateBasedInputAsync(user.Id, message.ChatId, userState, message, cancellationToken);
                Console.WriteLine($"✅ HandleStateBasedInputAsync completed");
                return;
            }
            else
            {
                Console.WriteLine($"⏰ User state expired: {userState.StateType}");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ No active user state found");
        }

        // Handle admin authentication commands (for MTProto setup)
        // Check if user is admin (check both userEntity and via authorization service)
        var isAdmin = userEntity?.Role == Domain.Enums.UserRole.Admin;
        if (!isAdmin && userEntity != null)
        {
            // Double-check using authorization service
            isAdmin = await _authorizationService.IsAdminAsync(user.Id, cancellationToken);
        }
        
        if (isAdmin)
        {
            // Handle /mtproto_setup command (check before other commands to ensure it's processed)
            if (message.Text != null && 
                (message.Text.Equals("/mtproto_setup", StringComparison.OrdinalIgnoreCase) ||
                 message.Text.StartsWith("/mtproto_setup@", StringComparison.OrdinalIgnoreCase) ||
                 message.Text.StartsWith("/mtproto_setup ", StringComparison.OrdinalIgnoreCase)))
            {
                await HandleMtProtoSetupStartAsync(user.Id, message.ChatId, cancellationToken);
                return;
            }
            
            if (message.Text?.StartsWith("/auth_code ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var code = message.Text.Substring("/auth_code ".Length).Trim();
                
                if (string.IsNullOrWhiteSpace(code))
                {
                    await _telegramBotService.SendMessageAsync(
                        message.ChatId,
                        "❌ Please provide a verification code.\n\n" +
                        "Usage: `/auth_code <your_code>`\n\n" +
                        "Example: `/auth_code 12345`",
                        cancellationToken);
                    return;
                }
                
                // Call LoginAsync with the code - just like the working example
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"📝 Calling LoginAsync with code: {code}");
                        var result = await _mtProtoService.LoginAsync(code, cancellationToken);
                        
                        if (result == null)
                        {
                            // Success!
                            await _telegramBotService.SendMessageAsync(
                                message.ChatId,
                                "✅ MTProto authentication successful!\n\n" +
                                "The service is now ready!",
                                cancellationToken);
                        }
                        else if (result == "password")
                        {
                            // Need 2FA
                            await _telegramBotService.SendMessageAsync(
                                message.ChatId,
                                "🔐 2FA password required.\n\n" +
                                "Send: `/auth_password <your_password>`",
                                cancellationToken);
                        }
                        else
                        {
                            await _telegramBotService.SendMessageAsync(
                                message.ChatId,
                                $"ℹ️ Needs: {result}",
                                cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Auth error: {ex.Message}");
                        await _telegramBotService.SendMessageAsync(
                            message.ChatId,
                            $"❌ Error: {ex.Message}",
                            cancellationToken);
                    }
                });
                
                await _telegramBotService.SendMessageAsync(
                    message.ChatId,
                    "⏳ Processing code...",
                    cancellationToken);
                
                return;
            }
            
            if (message.Text?.StartsWith("/auth_password ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var password = message.Text.Substring("/auth_password ".Length).Trim();
                
                if (string.IsNullOrWhiteSpace(password))
                {
                    await _telegramBotService.SendMessageAsync(
                        message.ChatId,
                        "❌ Please provide your 2FA password.\n\n" +
                        "Usage: `/auth_password <your_password>`",
                        cancellationToken);
                    return;
                }
                
                // Call LoginAsync with password
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"📝 Calling LoginAsync with password");
                        var result = await _mtProtoService.LoginAsync(password, cancellationToken);
                        
                        if (result == null)
                        {
                            await _telegramBotService.SendMessageAsync(
                                message.ChatId,
                                "✅ MTProto authentication successful!",
                                cancellationToken);
                        }
                        else
                        {
                            await _telegramBotService.SendMessageAsync(
                                message.ChatId,
                                $"ℹ️ Needs: {result}",
                                cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Auth error: {ex.Message}");
                        await _telegramBotService.SendMessageAsync(
                            message.ChatId,
                            $"❌ Error: {ex.Message}",
                            cancellationToken);
                    }
                });
                
                await _telegramBotService.SendMessageAsync(
                    message.ChatId,
                    "⏳ Processing password...",
                    cancellationToken);
                
                return;
            }
            
            if (message.Text?.Equals("/auth_clear", StringComparison.OrdinalIgnoreCase) == true)
            {
                Infrastructure.Services.MtProtoAuthStore.Clear();
                await _telegramBotService.SendMessageAsync(
                    message.ChatId,
                    "🧹 All stored authentication credentials have been cleared.",
                    cancellationToken);
                return;
            }
        }

        // Only handle /start command, everything else is buttons
        if (message.Text?.Equals("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine($"🚀 Handling /start command for user {user.Id}");
            await ShowMainMenuAsync(user.Id, message.ChatId, cancellationToken);
            Console.WriteLine($"✅ /start command completed");
            return;
        }

        // Track that user sent a message (important for contact verification)
        // This indicates user is responsive and likely added the contact
        try
        {
            var contactVerificationService = _serviceProvider.GetRequiredService<IContactVerificationService>();
            await contactVerificationService.MarkUserSentMessageAsync(user.Id, cancellationToken);
            Console.WriteLine($"✅ Marked user {user.Id} as having sent a message");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to mark user message: {ex.Message}");
            // Non-critical, continue processing
        }

        // For any other message, show the main menu
        Console.WriteLine($"📋 Showing main menu for user {user.Id}, text: {message.Text ?? "(no text)"}");
        await ShowMainMenuAsync(user.Id, message.ChatId, cancellationToken);
        Console.WriteLine($"✅ Main menu shown");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in HandleMessageAsync: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message ?? "None"}");
            
            // Try to send error message to user
            try
            {
                var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    message.ChatId,
                    errorMsg,
                    cancellationToken);
            }
            catch (Exception sendEx)
            {
                Console.WriteLine($"❌ Failed to send error message: {sendEx.Message}");
            }
            
            throw; // Re-throw to be caught by TelegramBotPollingService
        }
    }

    /// <summary>
    /// Handles callback queries from inline keyboard buttons
    /// </summary>
    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Data == null) return;

        Console.WriteLine($"🔔 Received callback query: {callbackQuery.Data}");
        
        var parts = callbackQuery.Data.Split('_');
        var action = parts[0];

        Console.WriteLine($"📋 Action: {action}, Parts: {string.Join(", ", parts)}");

        // Get user
        var userInfo = new TelegramUserInfo
        {
            Id = callbackQuery.From.Id,
            FirstName = callbackQuery.From.FirstName,
            LastName = callbackQuery.From.LastName,
            Username = callbackQuery.From.Username
        };
        var user = await _userService.GetOrCreateUserAsync(userInfo, cancellationToken);
        var chatId = callbackQuery.Message!.Chat.Id;

        // Handle menu navigation
        if (action == "menu")
        {
            if (parts.Length < 2) return;
            
            var menuAction = string.Join("_", parts.Skip(1));
            Console.WriteLine($"🎯 Menu action: {menuAction}");
            
            switch (menuAction)
            {
                case "browse_models":
                    await HandleModelsCommandAsync(chatId, cancellationToken);
                    break;
                case "my_subscriptions":
                    await HandleMySubscriptionsCommandAsync(user.Id, chatId, cancellationToken);
                    break;
                case "my_content":
                    await HandleMyContentCommandAsync(user.Id, chatId, cancellationToken);
                    break;
                case "register_model":
                    Console.WriteLine($"🚀 Calling HandleRegisterModelCommandAsync for user {user.Id}");
                    await HandleRegisterModelCommandAsync(user.Id, chatId, cancellationToken);
                    break;
                case "model_dashboard":
                    await HandleModelDashboardAsync(user.Id, chatId, cancellationToken);
                    break;
                case "admin_panel":
                    await HandleAdminCommandAsync(user.Id, chatId, cancellationToken);
                    break;
                case "add_contact":
                    await _telegramBotService.SendMessageAsync(
                        chatId,
                        "Here's the sender account contact card.\n\n" +
                        "Tap on it and click 'Add to Contacts' to save it.\n\n" +
                        "After adding, you'll be able to receive your purchased content!",
                        cancellationToken);
                    await SendSenderContactAsync(chatId, cancellationToken);
                    break;
                case "back_main":
                    await ShowMainMenuAsync(user.Id, chatId, cancellationToken);
                    break;
            }
            return;
        }

        // Handle MTProto setup
        if (action == "mtproto")
        {
            if (parts.Length >= 2)
            {
                var mtprotoAction = string.Join("_", parts.Skip(1));
                if (mtprotoAction == "setup_start")
                {
                    await HandleMtProtoSetupStartAsync(user.Id, chatId, cancellationToken);
                    return;
                }
            }
        }

        // Handle specific actions (existing logic)
        if (action == "admin")
        {
            if (parts.Length >= 2)
            {
                var adminAction = parts[1];
                switch (adminAction)
                {
                    case "settings":
                        // Clear any active state when returning to settings menu
                        await _userStateRepository.ClearStateAsync(user.Id, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await HandleAdminSettingsAsync(chatId, cancellationToken);
                        return;
                    case "setting":
                        if (parts.Length >= 3)
                        {
                            var settingAction = parts[2];
                            if (settingAction == "edit" && parts.Length >= 4)
                            {
                                var key = string.Join(":", parts.Skip(3));
                                await HandleSettingEditPromptAsync(user.Id, chatId, key, cancellationToken);
                                return;
                            }
                        }
                        break;
                    case "approve":
                    case "reject":
                        // Existing admin approve/reject logic handled below
                        break;
                }
            }
        }

        // Handle callbacks that don't require 3 parts (like reapply_model)
        if (parts.Length >= 2)
        {
            var callbackSecondPart = parts[1];
            
            // Handle reapply_model callback (only needs 2 parts)
            if (action == "reapply" && callbackSecondPart == "model")
            {
                await HandleReapplyModelAsync(user.Id, chatId, cancellationToken);
                return;
            }
        }
        
        if (parts.Length < 3) return;
        
        // Handle MTProto web setup
        if (action == "mtproto" && parts.Length >= 3 && parts[1] == "web" && parts[2] == "setup")
        {
            await HandleMtProtoWebSetupAsync(user.Id, chatId, cancellationToken);
            return;
        }
        
        var secondPart = parts[1];
        var thirdPart = parts[2];

        switch (action)
        {
            case "buy":
                if (secondPart == "photo")
                {
                    await HandleBuyPhotoCommandAsync(user.Id, thirdPart, chatId, cancellationToken);
                }
                else
                {
                    await _telegramBotService.SendMessageAsync(
                        chatId,
                        "❌ Platform subscriptions are no longer supported. Please subscribe to individual models instead.",
                        cancellationToken);
                }
                break;

            case "test":
                if (secondPart == "sub")
                {
                    await HandleTestBuySubscriptionAsync(user.Id, thirdPart, chatId, cancellationToken);
                }
                else if (secondPart == "photo")
                {
                    await HandleTestBuyPhotoAsync(user.Id, thirdPart, chatId, cancellationToken);
                }
                break;

            case "view":
                if (secondPart == "photo" && Guid.TryParse(thirdPart, out var viewPhotoId))
                {
                    await HandleViewPhotoCommandAsync(user.Id, chatId, viewPhotoId, cancellationToken);
                }
                else if (secondPart == "model" && Guid.TryParse(thirdPart, out var viewModelId))
                {
                    await HandleViewModelCommandAsync(thirdPart, chatId, user.Id, cancellationToken);
                }
                else if (secondPart == "content" && Guid.TryParse(thirdPart, out var viewContentModelId))
                {
                    await HandleViewModelContentAsync(user.Id, viewContentModelId, chatId, cancellationToken);
                }
                else if (secondPart == "model" && parts.Length > 3 && parts[2] == "content" && Guid.TryParse(parts[3], out var viewModelContentId))
                {
                    // Handle view_model_content_{modelId} callback from subscription list
                    await HandleViewModelContentAsync(user.Id, viewModelContentId, chatId, cancellationToken);
                }
                else if (secondPart == "demo" && Guid.TryParse(thirdPart, out var viewDemoModelId))
                {
                    await HandleViewDemoContentAsync(user.Id, viewDemoModelId, chatId, cancellationToken);
                }
                break;

            case "sub":
                if (secondPart == "model" && Guid.TryParse(thirdPart, out var subModelId))
                {
                    await HandleSubscribeToModelAsync(user.Id, subModelId, chatId, cancellationToken);
                }
                break;

            case "admin":
                if (secondPart == "approve" && Guid.TryParse(thirdPart, out var approveModelId))
                {
                    await HandleAdminApproveModelAsync(user.Id, approveModelId, chatId, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (secondPart == "reject" && Guid.TryParse(thirdPart, out var rejectModelId))
                {
                    await HandleAdminRejectModelAsync(user.Id, rejectModelId, chatId, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (secondPart == "settings")
                {
                    await HandleAdminSettingsAsync(chatId, cancellationToken);
                }
                else if (secondPart == "language")
                {
                    if (thirdPart == "settings")
                    {
                        await HandleAdminLanguageSettingsAsync(user.Id, chatId, cancellationToken);
                    }
                    else if (thirdPart == "set" && parts.Length > 3 && Enum.TryParse<Domain.Enums.BotLanguage>(parts[3], out var selectedLanguage))
                    {
                        await HandleAdminSetLanguageAsync(user.Id, chatId, selectedLanguage, cancellationToken);
                    }
                }
                else if (secondPart == "single" && thirdPart == "model" && parts.Length > 3)
                {
                    var fourthPart = parts[3];
                    if (fourthPart == "settings")
                    {
                        await HandleAdminSingleModelSettingsAsync(user.Id, chatId, cancellationToken);
                    }
                    else if (fourthPart == "enable" && parts.Length > 4 && Guid.TryParse(parts[4], out var enableModelId))
                    {
                        await HandleAdminEnableSingleModelModeAsync(user.Id, enableModelId, chatId, cancellationToken);
                    }
                    else if (fourthPart == "disable")
                    {
                        await HandleAdminDisableSingleModelModeAsync(user.Id, chatId, cancellationToken);
                    }
                }
                break;

            case "terms":
                if (secondPart == "accept" && parts.Length > 2 && Guid.TryParse(parts[2], out var termsUserId))
                {
                    await HandleTermsAcceptanceAsync(termsUserId, chatId, cancellationToken);
                }
                break;

            case "model":
                // Model dashboard actions
                switch (string.Join("_", parts.Skip(1)))
                {
                    case "upload_premium":
                        await HandleModelUploadPremiumAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "upload_demo":
                        await HandleModelUploadDemoAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "view_content":
                        await HandleModelViewContentAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "edit_content":
                        await HandleModelEditContentMenuAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "manage_subscription":
                        await HandleModelManageSubscriptionAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "set_subscription":
                        await HandleModelSetSubscriptionPromptAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "set_alias":
                        await HandleModelSetAliasPromptAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "content_statistics":
                        await HandleModelContentStatisticsAsync(user.Id, chatId, cancellationToken);
                        break;
                    case "top_content":
                        await HandleModelTopContentAsync(user.Id, chatId, cancellationToken);
                        break;
                }
                break;

            case "edit":
                if (secondPart == "photo" && Guid.TryParse(thirdPart, out var editPhotoId))
                {
                    await HandleEditPhotoOptionsAsync(user.Id, editPhotoId, chatId, cancellationToken);
                }
                else if (secondPart == "caption" && Guid.TryParse(thirdPart, out var captionPhotoId))
                {
                    await HandleEditCaptionPromptAsync(user.Id, captionPhotoId, chatId, cancellationToken);
                }
                else if (secondPart == "price" && Guid.TryParse(thirdPart, out var pricePhotoId))
                {
                    await HandleEditPricePromptAsync(user.Id, pricePhotoId, chatId, cancellationToken);
                }
                break;

            case "delete":
                if (secondPart == "photo" && Guid.TryParse(thirdPart, out var deletePhotoId))
                {
                    await HandleDeletePhotoAsync(user.Id, deletePhotoId, chatId, cancellationToken);
                }
                break;
        }
    }

    #region State-Based Input Handling

    /// <summary>
    /// Routes state-based input to the appropriate handler
    /// </summary>
    private async Task HandleStateBasedInputAsync(Guid userId, long chatId, UserState userState, TelegramMessage message, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"🔄 HandleStateBasedInputAsync called. StateType: {userState.StateType}, Text: {message.Text ?? "(no text)"}");
            
            switch (userState.StateType)
            {
                case Domain.Enums.UserStateType.UploadingPremiumMedia:
                    await HandlePremiumMediaUploadAsync(userId, chatId, message, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.UploadingDemoMedia:
                    await HandleDemoMediaUploadAsync(userId, chatId, message, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.SettingPremiumMediaPrice:
                    await HandlePremiumMediaPriceInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.SettingPremiumMediaCaption:
                    await HandlePremiumMediaCaptionInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.EditingCaption:
                    await HandleCaptionEditInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.EditingPrice:
                    await HandlePriceEditInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.SettingSubscriptionPlan:
                    await HandleSubscriptionPlanInputAsync(userId, chatId, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.EditingPlatformSetting:
                    await HandlePlatformSettingInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.MtProtoSetupApiId:
                    await HandleMtProtoSetupApiIdInputAsync(userId, chatId, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.MtProtoSetupApiHash:
                    await HandleMtProtoSetupApiHashInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.MtProtoSetupPhoneNumber:
                    await HandleMtProtoSetupPhoneNumberInputAsync(userId, chatId, userState.StateData, message.Text, cancellationToken);
                    break;

                case Domain.Enums.UserStateType.SettingModelAlias:
                    await HandleModelAliasInputAsync(userId, chatId, message.Text, cancellationToken);
                    break;

                default:
                    await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _telegramBotService.SendMessageAsync(chatId, "Unknown state. Please try again.", cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling state-based input: {ex.Message}");
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var errorMsg = await _localizationService.GetStringAsync("error.processing_input", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    #endregion

    /// <summary>
    /// Handles bot commands
    /// </summary>
    /// <summary>
    /// Shows the main menu with buttons
    /// </summary>
    private async Task ShowMainMenuAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"📋 ShowMainMenuAsync called for userId: {userId}, chatId: {chatId}");
            
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            Console.WriteLine($"👤 User retrieved: {user?.Id}, Role: {user?.Role}");
            
            var isAdmin = user?.Role == Domain.Enums.UserRole.Admin;
            var isModel = user?.Role == Domain.Enums.UserRole.Model;
            
            // Check if single model mode is enabled
            var isSingleModelMode = await _singleModelModeService.IsSingleModelModeAsync(cancellationToken);
            var defaultModel = isSingleModelMode ? await _singleModelModeService.GetDefaultModelAsync(cancellationToken) : null;

            var message = await _localizationService.GetStringAsync("menu.welcome", cancellationToken);

            Console.WriteLine($"📝 Message prepared, building buttons...");
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

        // Row 1: Browse Models OR View Model Content (Single Model Mode)
        if (isSingleModelMode && defaultModel != null)
        {
            // Use alias if available, otherwise use DisplayName
            var modelDisplayText = !string.IsNullOrWhiteSpace(defaultModel.Alias) 
                ? defaultModel.Alias 
                : defaultModel.DisplayName;
            
            // In single model mode, show direct link to model content
            var buttonText = await _localizationService.GetStringAsync("menu.view_model_content", modelDisplayText);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    buttonText,
                    $"view_model_{defaultModel.Id}")
            });
        }
        else
        {
            // Normal mode: Show Browse Models
            var browseText = await _localizationService.GetStringAsync("menu.browse_models", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    browseText,
                    "menu_browse_models")
            });
        }

        // Row 2: My Subscriptions & My Content
        var subscriptionsText = await _localizationService.GetStringAsync("menu.my_subscriptions", cancellationToken);
        var myContentText = await _localizationService.GetStringAsync("menu.my_content", cancellationToken);
        buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
        {
            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                subscriptionsText,
                "menu_my_subscriptions"),
            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                myContentText,
                "menu_my_content")
        });

        // Row 4: Become a Model (if not already a model and not in single model mode)
        if (!isModel && !isAdmin && !isSingleModelMode)
        {
            var becomeModelText = await _localizationService.GetStringAsync("menu.become_model", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    becomeModelText,
                    "menu_register_model")
            });
        }

        // Row 5: Model Dashboard (if model)
        if (isModel)
        {
            var dashboardText = await _localizationService.GetStringAsync("menu.model_dashboard", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    dashboardText,
                    "menu_model_dashboard")
            });
        }

        // Row 6: Admin Panel (if admin) - Show pending count
        if (isAdmin)
        {
            var pendingModels = await _modelService.GetPendingApprovalModelsAsync(cancellationToken);
            var pendingCount = pendingModels.Count();
            var adminPanelText = await _localizationService.GetStringAsync("menu.admin_panel", cancellationToken);
            var adminButtonText = pendingCount > 0 
                ? $"{adminPanelText} ({pendingCount} pending)" 
                : adminPanelText;
            
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    adminButtonText,
                    "menu_admin_panel")
            });
        }

            Console.WriteLine($"⌨️ Keyboard created with {buttons.Count} rows");
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            
            Console.WriteLine($"📤 Sending message to chatId: {chatId}");
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
            Console.WriteLine($"✅ Message sent successfully to chatId: {chatId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in ShowMainMenuAsync: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            Console.WriteLine($"❌ Inner exception: {ex.InnerException?.Message ?? "None"}");
            throw; // Re-throw to be caught by HandleMessageAsync
        }
    }

    private async Task HandleSubscriptionsCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        // Platform subscriptions are no longer supported
        await _telegramBotService.SendMessageAsync(
            chatId,
            "❌ Platform subscriptions are no longer supported. Please subscribe to individual models instead.",
            cancellationToken);
    }

    private async Task HandlePhotosCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Get all photos that are for sale
            var photos = await _photoRepository.GetAllAsync(cancellationToken);
            var availablePhotos = photos.Where(p => p.IsForSale && !p.IsDeleted).Take(10).ToList();

            if (!availablePhotos.Any())
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "No photos available at the moment.",
                    cancellationToken);
                return;
            }

            var message = " Available Photos:\n\n";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            foreach (var photo in availablePhotos)
            {
                message += $" {photo.Caption ?? "Untitled"}\n";
                message += $"   Price: {photo.Price.Amount} stars\n\n";

                // Add buttons for this photo
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        $" Buy: {photo.Caption ?? "Photo"}",
                        $"buy_photo_{photo.Id}"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        $" Test Buy",
                        $"test_photo_{photo.Id}")
                });
            }

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_photos", ex.Message);
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMsg,
                cancellationToken);
        }
    }

    private async Task HandleBuyPhotoCommandAsync(Guid userId, string photoIdStr, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(photoIdStr, out var photoId))
            {
                var invalidPhotoIdMsg = await _localizationService.GetStringAsync("common.invalid_photo_id", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    invalidPhotoIdMsg,
                    cancellationToken);
                return;
            }

            // Get the photo
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null || !photo.IsForSale)
            {
                var notFoundMsg = await _localizationService.GetStringAsync("content.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    notFoundMsg,
                    cancellationToken);
                return;
            }

            // Create invoice for payment
            var invoiceRequest = new Application.DTOs.CreateInvoiceRequest
            {
                ChatId = chatId,
                Title = $"Photo: {photo.Caption ?? "Premium Photo"}",
                Description = photo.Caption ?? "Premium content photo",
                Payload = $"photo_{photo.Id}_{userId}",
                ProviderToken = "", // Empty for Telegram Stars
                Currency = "XTR", // Telegram Stars currency
                Amount = photo.Price.Amount,
                Prices = new Dictionary<string, string>
                {
                    { "Photo", photo.Price.Amount.ToString() }
                }
            };

            var invoiceId = await _telegramBotService.CreateInvoiceAsync(invoiceRequest, cancellationToken);

            if (invoiceId != null)
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    " Invoice created! Please complete the payment.",
                    cancellationToken);
            }
            else
            {
                var invoiceFailedMsg = await _localizationService.GetStringAsync("purchase.invoice_failed", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    invoiceFailedMsg,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.generic", ex.Message);
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMsg,
                cancellationToken);
        }
    }

    private async Task HandleMyContentCommandAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Get all photos
            var allPhotos = await _photoRepository.GetAllAsync(cancellationToken);
            var availablePhotos = allPhotos.Where(p => !p.IsDeleted).ToList();

            if (!availablePhotos.Any())
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "No content available at the moment.",
                    cancellationToken);
                return;
            }

            var message = await _localizationService.GetStringAsync("content.my_content.title", cancellationToken);
            message += "\n";
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();
            var hasAccess = false;

            foreach (var photo in availablePhotos)
            {
                // Check if user has access
                var accessResult = await _contentAuthorizationService.CheckPhotoAccessAsync(userId, photo.Id, cancellationToken);

                Console.WriteLine($"Photo {photo.Caption}: HasAccess={accessResult.HasAccess}, Type={accessResult.AccessType}");

                if (accessResult.HasAccess)
                {
                    hasAccess = true;
                    var subscriptionLabel = await _localizationService.GetStringAsync("content.subscription_label", cancellationToken);
                    
                    message += $"{photo.Caption ?? "Untitled"}\n";
                    message += $"{(accessResult.AccessType == ContentAccessType.Subscription ? subscriptionLabel : "    ✅ Purchased")}\n\n";

                    // Add view button
                    var viewBtnText = await _localizationService.GetStringAsync("content.view_button", cancellationToken);
                    buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                $"{viewBtnText}: {photo.Caption ?? "Photo"}",
                                $"view_photo_{photo.Id}")
                        });
                }
            }

            if (!hasAccess)
            {
                var noAccessMessage = "You don't have access to any content yet.\n\n" +
                                     "Browse models and subscribe to access their exclusive content!";
                var browseText = await _localizationService.GetStringAsync("menu.browse_models", cancellationToken);
                var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
                
                var noAccessButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            browseText,
                            "menu_browse_models")
                    },
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            backText,
                            "menu_back_main")
                    }
                };
                
                var noAccessKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(noAccessButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noAccessMessage, noAccessKeyboard, cancellationToken);
                return;
            }

            var viewInstruction = await _localizationService.GetStringAsync("content.view_instruction", cancellationToken);
            message += $"\n{viewInstruction}";

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_your_content", ex.Message);
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMsg,
                cancellationToken);
        }
    }

    private async Task HandleViewPhotoCommandAsync(Guid userId, long chatId, Guid photoId, CancellationToken cancellationToken)
    {
        try
        {
            // Check authorization
            var accessResult = await _contentAuthorizationService.CheckPhotoAccessAsync(userId, photoId, cancellationToken);

            if (!accessResult.HasAccess)
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    " Access denied. " + (accessResult.Reason ?? "You don't have permission to view this content."),
                    cancellationToken);
                return;
            }

            // Get photo details
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                var photoNotFoundMsg = await _localizationService.GetStringAsync("content.photo_not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, photoNotFoundMsg, cancellationToken);
                return;
            }

            // Get user's Telegram ID
            var user = await _userService.GetUserByTelegramIdAsync(chatId, cancellationToken);
            if (user == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
                return;
            }

            // Determine what to send: FilePath (local file) or FileId (Telegram file ID)
            string filePathOrId = !string.IsNullOrWhiteSpace(photo.FileInfo.FilePath) 
                ? photo.FileInfo.FilePath 
                : photo.FileInfo.FileId;

            if (string.IsNullOrWhiteSpace(filePathOrId))
            {
                Console.WriteLine($"❌ ERROR: Photo {photo.Id} has no file path or file ID stored!");
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "❌ Photo data is corrupted. Please contact the model to re-upload this content.",
                    cancellationToken);
                return;
            }

            // Send photo via ContentDeliveryService (uses MTProto with contact check and self-destruct timer)
            Console.WriteLine($"📤 Attempting to send photo {photo.Id}. Using: {filePathOrId.Substring(0, Math.Min(40, filePathOrId.Length))}...");
            
            var sendRequest = new SendPhotoRequest
            {
                RecipientTelegramUserId = chatId,
                FilePath = filePathOrId,
                Caption = photo.Caption,
                PhotoId = photo.Id,
                UserId = user.Id,
                ViewerUsername = user.Username,
                SelfDestructSeconds = 60 // Default 60 seconds self-destruct timer
            };

            var deliveryResult = await _contentDeliveryService.SendPhotoAsync(sendRequest, cancellationToken);

            if (!deliveryResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to send photo to chat {chatId}: {deliveryResult.ErrorMessage}");
                
                // اگر به خاطر مشکل contact بود، contact card هم بفرست
                if (deliveryResult.ErrorMessage?.Contains("کانتکت") == true || 
                    deliveryResult.ErrorMessage?.Contains("contact") == true)
                {
                    await _telegramBotService.SendMessageAsync(
                        chatId,
                        "📱 برای دریافت محتوای پرمیوم، لطفاً ابتدا حساب فرستنده را به کانتکت‌های خود اضافه کنید:\n\n" +
                        "1️⃣ روی کارت زیر کلیک کنید\n" +
                        "2️⃣ گزینه 'Add to Contacts' را انتخاب کنید\n" +
                        "3️⃣ سپس دوباره دکمه 'View' را بزنید",
                        cancellationToken);
                    
                    // ارسال contact card
                    await SendSenderContactAsync(chatId, cancellationToken);
                }
                else
                {
                    var failedMsg = await _localizationService.GetStringAsync("delivery.failed", cancellationToken);
                    await _telegramBotService.SendMessageAsync(
                        chatId,
                        deliveryResult.ErrorMessage ?? failedMsg,
                        cancellationToken);
                }
            }
            else
            {
                // Track the view: increment view count
                photo.IncrementViewCount();
                await _photoRepository.UpdateAsync(photo, cancellationToken);
                
                // Log view history for premium content
                await _viewHistoryRepository.LogViewAsync(
                    userId: userId,
                    photoId: photo.Id,
                    modelId: photo.ModelId,
                    photoType: photo.Type,
                    viewerUsername: user.Username,
                    photoCaption: photo.Caption,
                    cancellationToken: cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                Console.WriteLine($"✅ Photo sent successfully to chat {chatId} with self-destruct timer. ViewCount: {photo.ViewCount}");
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "✅ عکس با موفقیت ارسال شد! این عکس پس از مشاهده خودکار حذف می‌شود.",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in HandleViewPhotoCommandAsync: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            
            var errorMessage = await _localizationService.GetStringAsync("delivery.error.general", "");
            
            if (ex.Message.Contains("PHONE_MIGRATE") || ex.Message.Contains("not configured") || ex.Message.Contains("authentication"))
            {
                errorMessage = await _localizationService.GetStringAsync("delivery.error.mtproto", cancellationToken);
            }
            else
            {
                errorMessage = await _localizationService.GetStringAsync("delivery.error.general", ex.Message);
            }
            
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMessage,
                cancellationToken);
        }
    }

    private async Task HandleTestPhotoCommandAsync(Guid userId, long chatId, Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
        if (photo == null || !photo.IsForSale)
        {
            var notForSaleMsg = await _localizationService.GetStringAsync("content.not_for_sale", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, notForSaleMsg, cancellationToken);
            return;
        }

        // Create photo purchase request (bypassing payment)
        var request = new Application.DTOs.CreatePhotoPurchaseRequest
        {
            UserId = userId,
            PhotoId = photoId
        };

        var result = await _photoPurchaseService.CreatePhotoPurchaseAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            // Get the purchase and mark as completed (for testing)
            var purchaseRepo = _photoPurchaseService as dynamic; // This is a workaround - ideally expose through interface

            var successMessage = "Test purchase completed!\n\n" +
                                $"Photo: {photo.Caption ?? "Untitled"}\n\n" +
                                "The photo is now available in your content!";
            
            var successButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "View My Content",
                        "menu_my_content")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Main Menu",
                        "menu_back_main")
                }
            };
            
            var successKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(successButtons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, successKeyboard, cancellationToken);
        }
        else
        {
            var failedMsg = await _localizationService.GetStringAsync("purchase.test_failed", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, result.ErrorMessage ?? failedMsg, cancellationToken);
        }
    }

    // Public methods for callback handling
    public async Task HandleBuySubscriptionCallbackAsync(long telegramUserId, string planIdStr, long chatId, CancellationToken cancellationToken)
    {
        await _telegramBotService.SendMessageAsync(
            chatId,
            "❌ Platform subscriptions are no longer supported. Please subscribe to individual models instead.",
            cancellationToken);
    }

    public async Task HandleBuyPhotoCallbackAsync(long telegramUserId, string photoIdStr, long chatId, CancellationToken cancellationToken)
    {
        // Get user from Telegram ID
        var user = await _userService.GetUserByTelegramIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            var userNotFoundMsg = await _localizationService.GetStringAsync("user.not_found", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, userNotFoundMsg, cancellationToken);
            return;
        }

        await HandleBuyPhotoCommandAsync(user.Id, photoIdStr, chatId, cancellationToken);
    }

    public async Task HandleTestBuySubscriptionCallbackAsync(long telegramUserId, string planIdStr, long chatId, CancellationToken cancellationToken)
    {
        await _telegramBotService.SendMessageAsync(
            chatId,
            "❌ Platform subscriptions are no longer supported. Please subscribe to individual models instead.",
            cancellationToken);
    }

    public async Task HandleTestBuyPhotoCallbackAsync(long telegramUserId, string photoIdStr, long chatId, CancellationToken cancellationToken)
    {
        // Get user from Telegram ID
        var user = await _userService.GetUserByTelegramIdAsync(telegramUserId, cancellationToken);
        if (user == null)
        {
            var userNotFoundMsg = await _localizationService.GetStringAsync("user.not_found", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, userNotFoundMsg, cancellationToken);
            return;
        }

        await HandleTestBuyPhotoAsync(user.Id, photoIdStr, chatId, cancellationToken);
    }

    // Test methods for development (bypass payment)
    private async Task HandleTestBuySubscriptionAsync(Guid userId, string planIdStr, long chatId, CancellationToken cancellationToken)
    {
        await _telegramBotService.SendMessageAsync(
            chatId,
            "❌ Platform subscriptions are no longer supported. Please subscribe to individual models instead.",
            cancellationToken);
    }

    private async Task HandleTestBuyPhotoAsync(Guid userId, string photoIdStr, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                var userNotFoundMsg = await _localizationService.GetStringAsync("user.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, userNotFoundMsg, cancellationToken);
                return;
            }
            
            if (!Guid.TryParse(photoIdStr, out var photoId))
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "Invalid photo ID format.",
                    cancellationToken);
                return;
            }

            // Get the photo
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null || !photo.IsForSale)
            {
                var notFoundMsg = await _localizationService.GetStringAsync("content.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    notFoundMsg,
                    cancellationToken);
                return;
            }

            // Create photo purchase request (bypassing payment)
            var request = new Application.DTOs.CreatePhotoPurchaseRequest
            {
                UserId = userId,
                PhotoId = photoId
            };

            var result = await _photoPurchaseService.CreatePhotoPurchaseAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                // Mark the purchase as completed for testing (bypass payment)
                var purchase = await _purchaseRepository.GetByIdAsync(result.PurchaseId, cancellationToken);
                if (purchase != null)
                {
                    purchase.MarkPaymentCompleted($"TEST_PAYMENT_{Guid.NewGuid()}", $"TEST_PRECHECK_{Guid.NewGuid()}");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var successMessage = await _localizationService.GetStringAsync(
                    "purchase.test_success",
                    user.FirstName,
                    photo.Caption ?? "Untitled",
                    photo.Price.Amount.ToString(),
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                
                var successButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "View My Content",
                            "menu_my_content")
                    },
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Main Menu",
                            "menu_back_main")
                    }
                };
                
                var successKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(successButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, successKeyboard, cancellationToken);
            }
            else
            {
                var failedMsg = await _localizationService.GetStringAsync("purchase.failed", result.ErrorMessage ?? "");
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    failedMsg,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.generic", ex.Message);
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMsg,
                cancellationToken);
        }
    }

    /// <summary>
    /// Notifies all admin users about a new model registration
    /// </summary>
    private async Task NotifyAdminsAboutNewModelAsync(Domain.Entities.Model model, Domain.Entities.User applicant, CancellationToken cancellationToken)
    {
        try
        {
            // Get all users with Admin role
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            var adminUsers = allUsers.Where(u => u.Role == Domain.Enums.UserRole.Admin && u.IsActive).ToList();

            if (!adminUsers.Any())
            {
                Console.WriteLine("⚠️ No admin users found to notify about new model registration");
                return;
            }

            var applicantName = $"{applicant.FirstName} {applicant.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(applicantName))
            {
                applicantName = "Unknown User";
            }
            
            // Create username link if available
            var usernameLink = !string.IsNullOrWhiteSpace(applicant.Username) 
                ? $"@{applicant.Username}" 
                : "No username";
            
            var notificationMessage = "🔔 New Model Registration\n\n" +
                                     $"👤 Applicant: {applicantName}\n" +
                                     $"💬 Telegram: {usernameLink}\n" +
                                     $"📝 Display Name: {model.DisplayName}\n" +
                                     $"📅 Registered: {model.CreatedAt:g}\n" +
                                     $"🆔 Application ID: {model.Id.ToString().Substring(0, 8)}...\n\n" +
                                     "⏳ This application is awaiting your review.";

            var notificationButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "✅ Approve",
                        $"admin_approve_{model.Id}"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Reject",
                        $"admin_reject_{model.Id}")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "🛡️ Admin Panel",
                        "menu_admin_panel")
                }
            };

            var notificationKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(notificationButtons);

            // Send notification to each admin
            foreach (var admin in adminUsers)
            {
                try
                {
                    var adminChatId = admin.TelegramUserId.Value;
                    await _telegramBotService.SendMessageWithButtonsAsync(
                        adminChatId,
                        notificationMessage,
                        notificationKeyboard,
                        cancellationToken);
                    
                    Console.WriteLine($"✅ Notified admin {admin.Username ?? admin.FirstName} about new model registration");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to notify admin {admin.Username}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying admins: {ex.Message}");
        }
    }

    /// <summary>
    /// Notifies all admin users about new media upload (premium or demo)
    /// Sends media WITHOUT secure mode so admin can keep it
    /// </summary>
    private async Task NotifyAdminsAboutNewMediaAsync(
        Photo photo, 
        Domain.Entities.Model model, 
        Domain.Entities.User uploader,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all users with Admin role
            var allUsers = await _userRepository.GetAllAsync(cancellationToken);
            var adminUsers = allUsers.Where(u => u.Role == Domain.Enums.UserRole.Admin && u.IsActive).ToList();

            if (!adminUsers.Any())
            {
                Console.WriteLine("⚠️ No admin users found to notify about new media upload");
                return;
            }

            var uploaderName = $"{uploader.FirstName} {uploader.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(uploaderName))
            {
                uploaderName = uploader.Username ?? "Unknown User";
            }

            var usernameLink = !string.IsNullOrWhiteSpace(uploader.Username) 
                ? $"@{uploader.Username}" 
                : "No username";

            var mediaType = photo.Type == Domain.Enums.PhotoType.Demo ? "Demo" : "Premium";
            var priceInfo = photo.Type == Domain.Enums.PhotoType.Demo 
                ? "Free Demo" 
                : $"{photo.Price.Amount} Stars";

            var notificationMessage = $"🔔 New {mediaType} Media Upload\n\n" +
                                     $"👤 Uploader: {uploaderName}\n" +
                                     $"💬 Telegram: {usernameLink}\n" +
                                     $"📝 Model: {model.DisplayName}\n" +
                                     $"💰 Price: {priceInfo}\n" +
                                     $"📅 Uploaded: {DateTime.UtcNow:g}\n" +
                                     $"🆔 Media ID: {photo.Id.ToString().Substring(0, 8)}...\n";

            if (!string.IsNullOrWhiteSpace(photo.Caption))
            {
                notificationMessage += $"\n📄 Caption: {photo.Caption}";
            }

            // Determine what to send: FilePath (local file) or FileId (Telegram file ID)
            string filePathOrId = !string.IsNullOrWhiteSpace(photo.FileInfo.FilePath) 
                ? photo.FileInfo.FilePath 
                : photo.FileInfo.FileId;

            if (string.IsNullOrWhiteSpace(filePathOrId))
            {
                Console.WriteLine($"⚠️ Cannot notify admins: Photo {photo.Id} has no file path or file ID");
                return;
            }

            // Send notification to each admin (WITHOUT secure mode - using Bot API)
            foreach (var admin in adminUsers)
            {
                try
                {
                    var adminChatId = admin.TelegramUserId.Value;
                    
                    // Send media using Bot API (no secure mode, admin can keep it)
                    // Determine if it's photo or video from MimeType or file extension
                    bool mediaSent = false;
                    var isVideo = photo.FileInfo.MimeType?.StartsWith("video/") == true ||
                                  filePathOrId.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                  filePathOrId.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                  filePathOrId.EndsWith(".avi", StringComparison.OrdinalIgnoreCase);
                    
                    if (isVideo)
                    {
                        mediaSent = await _telegramBotService.SendVideoAsync(
                            adminChatId,
                            filePathOrId,
                            notificationMessage,
                            cancellationToken);
                    }
                    else
                    {
                        // Default to photo
                        mediaSent = await _telegramBotService.SendPhotoAsync(
                            adminChatId,
                            filePathOrId,
                            notificationMessage,
                            cancellationToken);
                    }

                    if (mediaSent)
                    {
                        Console.WriteLine($"✅ Notified admin {admin.Username ?? admin.FirstName} about new {mediaType} media upload");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Failed to send media to admin {admin.Username ?? admin.FirstName}");
                        // Still send text notification
                        await _telegramBotService.SendMessageAsync(
                            adminChatId,
                            notificationMessage + $"\n\n⚠️ Media file could not be sent. File ID: {filePathOrId.Substring(0, Math.Min(20, filePathOrId.Length))}...",
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to notify admin {admin.Username}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying admins about new media: {ex.Message}");
        }
    }

    private async Task SendSenderContactAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Read phone number from database
            var senderPhoneNumber = await _platformSettingsRepository.GetValueAsync("telegram:mtproto:phone_number", cancellationToken);
            if (string.IsNullOrEmpty(senderPhoneNumber))
            {
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    "❌ Sender account configuration is missing.",
                    cancellationToken);
                return;
            }

            // Just send the contact card without additional message
            // The calling method already sent an explanation
            await _telegramBotService.SendContactAsync(
                chatId,
                senderPhoneNumber,
                "Premium Content Delivery",
                "Bot",
                cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(
                chatId,
                $"❌ Error sending contact: {ex.Message}",
                cancellationToken);
        }
    }
    
    /// <summary>
    /// Handles MTProto web setup - generates one-time token and sends secure link
    /// </summary>
    private async Task HandleMtProtoWebSetupAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can configure MTProto.", cancellationToken);
                return;
            }

            // Generate one-time access token
            var tokenString = await _mtProtoAccessTokenService.GenerateTokenAsync(userId, cancellationToken);
            
            // Get server URL from configuration
            var serverUrl = _configuration["ServerUrl"] ?? "http://localhost:5000";
            var setupUrl = $"{serverUrl}/mtproto/auth?token={tokenString}";
            
            var message = "🔐 MTProto Web Setup\n\n" +
                         "یک لینک امن برای شما ساخته شد:\n\n" +
                         $"🔗 {setupUrl}\n\n" +
                         "⏱️ این لینک فقط 5 دقیقه اعتبار دارد\n" +
                         "🔒 فقط یکبار قابل استفاده است\n" +
                         "🌐 بعد از کلیک، session شما ذخیره می‌شود\n\n" +
                         "⚠️ این لینک را با کسی به اشتراک نگذارید!";
            
            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            Console.WriteLine($"✅ Generated MTProto web setup link for admin {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in HandleMtProtoWebSetupAsync: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"❌ خطا در ساخت لینک: {ex.Message}", cancellationToken);
        }
    }

    #region Marketplace Commands

    /// <summary>
    /// Handles the /models command - Browse all approved models
    /// </summary>
    private async Task HandleModelsCommandAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var models = await _modelDiscoveryService.BrowseModelsAsync(cancellationToken);
            var modelsList = models.ToList();

            if (!modelsList.Any())
            {
                var noModelsMessage = await _localizationService.GetStringAsync("models.none_available", cancellationToken);
                var becomeModelText = await _localizationService.GetStringAsync("models.become_model_button", cancellationToken);
                var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
                
                var noModelsButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            becomeModelText,
                            "menu_register_model")
                    },
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            backText,
                            "menu_back_main")
                    }
                };
                
                var noModelsKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(noModelsButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noModelsMessage, noModelsKeyboard, cancellationToken);
                return;
            }

            var message = await _localizationService.GetStringAsync("models.available_count", modelsList.Count.ToString());
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            foreach (var model in modelsList.Take(10)) // Limit to 10 for UI
            {
                var stats = await _modelDiscoveryService.GetModelStatisticsAsync(model.Id, cancellationToken);
                
                // Use alias if available, otherwise use DisplayName
                var displayText = !string.IsNullOrWhiteSpace(model.Alias) ? model.Alias : model.DisplayName;
                
                message += $"{displayText}\n";
                if (!string.IsNullOrWhiteSpace(model.Bio))
                {
                    message += $"   {model.Bio.Substring(0, Math.Min(80, model.Bio.Length))}\n";
                }
                
                var subscribersText = await _localizationService.GetStringAsync("models.subscribers", stats.TotalSubscribers.ToString());
                message += $"{subscribersText}\n";
                
                var contentText = await _localizationService.GetStringAsync("models.content_count", stats.PremiumPhotos.ToString());
                message += $"{contentText}\n";
                
                if (stats.HasSubscriptionAvailable && stats.SubscriptionPrice.HasValue)
                {
                    var subscriptionText = await _localizationService.GetStringAsync("models.subscription_info", 
                        stats.SubscriptionPrice.Value.ToString(), 
                        stats.SubscriptionDurationDays.ToString());
                    message += $"{subscriptionText}\n";
                }
                message += "\n";

                var viewButtonText = await _localizationService.GetStringAsync("models.view_button", displayText);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        viewButtonText,
                        $"view_model_{model.Id}")
                });
            }

            // Add back button
            var backBtnText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backBtnText,
                    "menu_back_main")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_models", ex.Message);
            await _telegramBotService.SendMessageAsync(
                chatId,
                errorMsg,
                cancellationToken);
        }
    }

    /// <summary>
    /// Handles viewing a specific model's profile
    /// </summary>
    private async Task HandleViewModelCommandAsync(string modelIdStr, long chatId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(modelIdStr, out var modelId))
            {
                var invalidIdMsg = await _localizationService.GetStringAsync("common.invalid_id", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, invalidIdMsg, cancellationToken);
                return;
            }

            var model = await _modelDiscoveryService.GetModelProfileAsync(modelId, cancellationToken);
            if (model == null)
            {
                var notFoundMsg = await _localizationService.GetStringAsync("model.profile.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, notFoundMsg, cancellationToken);
                return;
            }

            var stats = await _modelDiscoveryService.GetModelStatisticsAsync(modelId, cancellationToken);
            var premiumPhotos = await _modelDiscoveryService.GetModelPremiumPhotosAsync(modelId, cancellationToken);
            var photosList = premiumPhotos.ToList();
            var demoPhotos = await _modelDiscoveryService.GetModelDemoPhotosAsync(modelId, cancellationToken);
            var demoList = demoPhotos.ToList();

            // Check if user has active subscription
            var hasSubscription = await _modelSubscriptionService.HasActiveSubscriptionAsync(userId, modelId, cancellationToken);

            // Use alias if available, otherwise use DisplayName
            var displayText = !string.IsNullOrWhiteSpace(model.Alias) ? model.Alias : model.DisplayName;
            
            var message = $"📊 {displayText}\n\n";
            
            if (!string.IsNullOrWhiteSpace(model.Bio))
            {
                message += $"{model.Bio}\n\n";
            }
            
            var statisticsLabel = await _localizationService.GetStringAsync("model.profile.statistics", cancellationToken);
            message += $"{statisticsLabel}\n";
            
            var subscribersText = await _localizationService.GetStringAsync("model.profile.subscribers", stats.TotalSubscribers.ToString());
            message += $"{subscribersText}\n";
            
            var contentText = await _localizationService.GetStringAsync("model.profile.content", stats.PremiumPhotos.ToString());
            message += $"{contentText}\n";
            
            if (demoList.Any())
            {
                var demoText = await _localizationService.GetStringAsync("model.profile.demo_content", demoList.Count.ToString());
                message += $"{demoText}\n";
            }
            message += "\n";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            // Demo content button (always visible if demo exists)
            if (demoList.Any())
            {
                var viewDemoText = await _localizationService.GetStringAsync("model.profile.view_demo", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        viewDemoText,
                        $"view_demo_{model.Id}")
                });
            }

            // If user already has subscription, show access to content
            if (hasSubscription)
            {
                var subscribedMsg = await _localizationService.GetStringAsync("model.profile.subscribed", cancellationToken);
                message += $"{subscribedMsg}\n\n";
                
                var viewMyContentText = await _localizationService.GetStringAsync("model.profile.view_my_content", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        viewMyContentText,
                        $"view_content_{model.Id}")
                });
            }
            else
            {
                // Subscription button if available
                if (stats.HasSubscriptionAvailable && stats.SubscriptionPrice.HasValue)
                {
                    var subscribeOffer = await _localizationService.GetStringAsync("model.profile.subscribe_offer", 
                        stats.SubscriptionPrice.Value.ToString(), 
                        stats.SubscriptionDurationDays.ToString());
                    message += subscribeOffer;
                    
                    var subscribeButtonText = await _localizationService.GetStringAsync("model.profile.subscribe_button", 
                        stats.SubscriptionPrice.Value.ToString());
                    buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            subscribeButtonText,
                            $"sub_model_{model.Id}")
                    });
                }

                // Photo buttons for individual purchase
                if (photosList.Any())
                {
                    var availablePhotosText = await _localizationService.GetStringAsync("model.profile.available_photos", cancellationToken);
                    message += $"{availablePhotosText}\n";
                    foreach (var photo in photosList.Take(5))
                    {
                        message += $"  • {photo.Caption ?? "Photo"} ({photo.Price.Amount} stars)\n";
                        
                        var buyButtonText = await _localizationService.GetStringAsync("model.profile.buy_button", photo.Caption ?? "Photo");
                        buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                buyButtonText,
                                $"buy_photo_{photo.Id}")
                        });
                    }
                }
            }

            // Back button
            var backToModelsText = await _localizationService.GetStringAsync("model.profile.back_to_models", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backToModelsText,
                    "menu_browse_models")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.viewing_model", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows all premium content for a model (for subscribers)
    /// </summary>
    private async Task HandleViewModelContentAsync(Guid userId, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user has active subscription
            var hasSubscription = await _modelSubscriptionService.HasActiveSubscriptionAsync(userId, modelId, cancellationToken);
            if (!hasSubscription)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have an active subscription to this model. Subscribe first to access content!", cancellationToken);
                return;
            }

            var model = await _modelDiscoveryService.GetModelProfileAsync(modelId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var premiumPhotos = await _modelDiscoveryService.GetModelPremiumPhotosAsync(modelId, cancellationToken);
            var photosList = premiumPhotos.ToList();

            if (!photosList.Any())
            {
                // Use alias if available, otherwise use DisplayName
                var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                    ? model.Alias 
                    : model.DisplayName;
                
                var noContentMsg = $"📭 {modelDisplayText} hasn't uploaded any content yet.\n\n" +
                                  "Check back later!";
                var backButton = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Model",
                            $"view_model_{modelId}")
                    }
                };
                var backKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(backButton);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noContentMsg, backKeyboard, cancellationToken);
                return;
            }

            // Use alias if available, otherwise use DisplayName
            var displayText = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;
            
            var message = $"📂 {displayText}'s Content\n\n";
            message += $"✅ You have access to all {photosList.Count} premium photos!\n\n";
            message += "Select a photo to view:\n\n";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            foreach (var photo in photosList.Take(15)) // Show up to 15
            {
                var buttonText = photo.Caption ?? "Untitled Photo";
                if (buttonText.Length > 60)
                {
                    buttonText = buttonText.Substring(0, 57) + "...";
                }
                
                // Add view count if available
                if (photo.ViewCount > 0)
                {
                    buttonText += $" (👁️ {photo.ViewCount})";
                }

                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        $"📸 {buttonText}",
                        $"view_photo_{photo.Id}")
                });
            }

            if (photosList.Count > 15)
            {
                message += $"\n... and {photosList.Count - 15} more photos!\n";
            }

            // Back button
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "<< Back to Model",
                    $"view_model_{modelId}")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error viewing model content: {ex.Message}");
            var errorMsg = await _localizationService.GetStringAsync("error.loading_content", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows demo content for a model (free preview, available to everyone)
    /// </summary>
    private async Task HandleViewDemoContentAsync(Guid userId, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Get user for tracking
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
                return;
            }

            var model = await _modelDiscoveryService.GetModelProfileAsync(modelId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var demoPhotos = await _modelDiscoveryService.GetModelDemoPhotosAsync(modelId, cancellationToken);
            var demoList = demoPhotos.ToList();

            if (!demoList.Any())
            {
                // Use alias if available, otherwise use DisplayName
                var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                    ? model.Alias 
                    : model.DisplayName;
                
                var noContentMsg = $"📭 {modelDisplayText} hasn't uploaded any demo content yet.";
                var backButton = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Model",
                            $"view_model_{modelId}")
                    }
                };
                var backKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(backButton);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noContentMsg, backKeyboard, cancellationToken);
                return;
            }

            // Check if user has already viewed demo
            var demoAccess = await _demoAccessRepository.GetDemoAccessAsync(userId, modelId, cancellationToken);
            
            if (demoAccess != null)
            {
                // Use alias if available, otherwise use DisplayName
                var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                    ? model.Alias 
                    : model.DisplayName;
                
                var alreadyViewedMsg = $"🎁 {modelDisplayText}'s Free Demo\n\n" +
                                      "❌ You've already viewed the free demo content for this model.\n\n" +
                                      "To see more content, subscribe or purchase individual photos!";
                
                var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Model",
                            $"view_model_{modelId}")
                    }
                };
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, alreadyViewedMsg, keyboard, cancellationToken);
                return;
            }

            // Send the demo content (first demo photo) - using secure mode for subscribers
            var demoPhoto = demoList.First();
            
            // Determine what to send: FilePath (local file) or FileId (Telegram file ID)
            string filePathOrId = !string.IsNullOrWhiteSpace(demoPhoto.FileInfo.FilePath) 
                ? demoPhoto.FileInfo.FilePath 
                : demoPhoto.FileInfo.FileId;

            // Use alias if available, otherwise use DisplayName
            var displayTextForCaption = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;
            
            var caption = $"🎁 Free Demo from {displayTextForCaption}\n\n";
            if (!string.IsNullOrWhiteSpace(demoPhoto.Caption))
            {
                caption += $"{demoPhoto.Caption}\n\n";
            }
            caption += "Like what you see? Subscribe for full access to all premium content!";

            Console.WriteLine($"📤 Sending demo photo {demoPhoto.Id} to user {userId} (chatId: {chatId}) with secure mode");
            
            // Send via ContentDeliveryService with secure mode (60 seconds self-destruct)
            var sendRequest = new SendPhotoRequest
            {
                RecipientTelegramUserId = chatId,
                FilePath = filePathOrId,
                Caption = caption,
                PhotoId = demoPhoto.Id,
                UserId = user.Id,
                ViewerUsername = user.Username,
                SelfDestructSeconds = 60 // Secure mode: 60 seconds self-destruct timer
            };

            var deliveryResult = await _contentDeliveryService.SendPhotoAsync(sendRequest, cancellationToken);

            if (deliveryResult.IsSuccess)
            {
                // Track the view: increment view count and log in view history
                demoPhoto.IncrementViewCount();
                await _photoRepository.UpdateAsync(demoPhoto, cancellationToken);
                
                // Log view history for demo content
                await _viewHistoryRepository.LogViewAsync(
                    userId: userId,
                    photoId: demoPhoto.Id,
                    modelId: modelId,
                    photoType: Domain.Enums.PhotoType.Demo,
                    viewerUsername: user.Username,
                    photoCaption: demoPhoto.Caption,
                    cancellationToken: cancellationToken);
                
                // Track that user has viewed this demo (for one-time access)
                var newDemoAccess = new DemoAccess(userId, modelId, demoPhoto.FileInfo.FileId);
                await _demoAccessRepository.AddAsync(newDemoAccess, cancellationToken);
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                Console.WriteLine($"✅ Demo photo sent with secure mode and tracked: Photo {demoPhoto.Id}, User {userId}, Model {modelId}, ViewCount: {demoPhoto.ViewCount}");
                
                // Send follow-up message with action buttons
                var followUpMsg = "Want more content from this model?";
                var followUpButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Model",
                            $"view_model_{modelId}")
                    }
                };
                var followUpKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(followUpButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, followUpMsg, followUpKeyboard, cancellationToken);
            }
            else
            {
                Console.WriteLine($"❌ Failed to send demo photo {demoPhoto.Id} to user {userId}: {deliveryResult.ErrorMessage}");
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    deliveryResult.ErrorMessage ?? "❌ Failed to send demo content. Please try again later.",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error viewing demo content: {ex.Message}");
            var errorMsg = await _localizationService.GetStringAsync("error.loading_demo", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles model registration
    /// </summary>
    private async Task HandleRegisterModelCommandAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user already has a model
            var existingModel = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (existingModel != null)
            {
                // Allow re-application if rejected
                if (existingModel.Status == Domain.Entities.ModelStatus.Rejected)
                {
                    var reapplyMsg = $"Your previous model registration was rejected.\n\n" +
                                    $"Reason: {existingModel.RejectionReason}\n\n" +
                                    $"Would you like to submit a new application?";
                    
                    var reapplyButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                    {
                        new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                "✅ Submit New Application",
                                "reapply_model"), // Don't pass model ID, we'll use userId
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                "<< Back to Main Menu",
                                "menu_back_main")
                        }
                    };
                    
                    var reapplyKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(reapplyButtons);
                    await _telegramBotService.SendMessageWithButtonsAsync(chatId, reapplyMsg, reapplyKeyboard, cancellationToken);
                    return;
                }
                
                var statusMsg = existingModel.Status switch
                {
                    Domain.Entities.ModelStatus.PendingApproval => 
                        "Your model registration is awaiting admin approval.\n\n" +
                        "You'll be notified once an admin reviews your application.",
                    Domain.Entities.ModelStatus.Approved => 
                        "You are already an approved content creator!\n\n" +
                        "Use the Model Dashboard to manage your content.",
                    Domain.Entities.ModelStatus.Suspended => 
                        $"Your model account is currently suspended.\n\n" +
                        $"Reason: {existingModel.RejectionReason}",
                    _ => "You already have a model profile."
                };
                
                var statusButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "<< Back to Main Menu",
                            "menu_back_main")
                    }
                };
                
                var statusKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(statusButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, statusMsg, statusKeyboard, cancellationToken);
                return;
            }

            // Get user info
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
                return;
            }

            // Show Terms & Conditions before proceeding with registration
            await ShowModelTermsAndConditionsAsync(userId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.become_model_flow", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows Terms and Conditions for model registration
    /// </summary>
    private async Task ShowModelTermsAndConditionsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        var termsContent = await _modelTermsService.GetTermsContentAsync(cancellationToken);
        
        var acceptText = await _localizationService.GetStringAsync("terms.accept", cancellationToken);
        var declineText = await _localizationService.GetStringAsync("terms.decline", cancellationToken);
        
        var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
        {
            new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    acceptText,
                    $"terms_accept_{userId}"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    declineText,
                    "menu_back_main")
            }
        };

        var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
        await _telegramBotService.SendMessageWithButtonsAsync(chatId, termsContent, keyboard, cancellationToken);
    }

    /// <summary>
    /// Handles terms acceptance and proceeds with model registration
    /// </summary>
    private async Task HandleTermsAcceptanceAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Get user info
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
                return;
            }

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = user.Username ?? $"Model_{userId.ToString().Substring(0, 8)}";
            }

            // Register the model
            var newCreatorBio = await _localizationService.GetStringAsync("model.status.new_content_creator", cancellationToken);
            var model = await _modelService.RegisterModelAsync(userId, displayName, newCreatorBio, cancellationToken);
            
            // Record terms acceptance
            await _modelTermsService.RecordAcceptanceAsync(model.Id, cancellationToken);

            var successMessage = "✅ Model registration submitted successfully!\n\n" +
                                $"Display Name: {model.DisplayName}\n\n" +
                                "⏳ Your application is now pending admin approval.\n\n" +
                                "An admin will review your application and you'll be notified once approved. " +
                                "After approval, you'll be able to:\n" +
                                "• Set subscription prices\n" +
                                "• Upload premium content\n" +
                                "• Manage your creator profile\n\n" +
                                $"Application ID: {model.Id.ToString().Substring(0, 8)}...";
            
            var successButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "« Back to Main Menu",
                        "menu_back_main")
                }
            };
            
            var successKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(successButtons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, successKeyboard, cancellationToken);
            
            // Notify all admins about the new model registration
            Console.WriteLine($"📢 About to notify admins about new model registration. Model ID: {model.Id}");
            try
            {
                await NotifyAdminsAboutNewModelAsync(model, user, cancellationToken);
                Console.WriteLine("✅ Admin notification method completed");
            }
            catch (Exception notifyEx)
            {
                Console.WriteLine($"❌ FAILED to notify admins: {notifyEx.Message}");
                Console.WriteLine($"Stack trace: {notifyEx.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error registering model: {ex.Message}");
            var errorMsg = await _localizationService.GetStringAsync("error.become_model", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles model dashboard - Shows model stats and management options
    /// </summary>
    private async Task HandleModelDashboardAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                var notModelMsg = await _localizationService.GetStringAsync("model.register.not_model", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    notModelMsg,
                    cancellationToken);
                return;
            }

            if (model.Status != Domain.Entities.ModelStatus.Approved)
            {
                var statusMsg = await _localizationService.GetStringAsync("model.status.not_approved", cancellationToken);
                await _telegramBotService.SendMessageAsync(
                    chatId,
                    string.Format(statusMsg, model.Status),
                    cancellationToken);
                return;
            }

            // Get comprehensive revenue analytics
            var analytics = await _revenueAnalyticsService.GetModelRevenueAnalyticsAsync(model.Id, cancellationToken);

            // Use alias if available, otherwise use DisplayName
            var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;
            
            var message = await _localizationService.GetStringAsync("dashboard.title", modelDisplayText);
            
            // Revenue Section
            message += await _localizationService.GetStringAsync("dashboard.revenue", 
                analytics.TotalRevenueStars,
                analytics.ThisMonthRevenueStars,
                analytics.TodayRevenueStars,
                analytics.AvailableBalanceStars);
            
            // Performance Metrics
            message += await _localizationService.GetStringAsync("dashboard.metrics",
                analytics.TotalSubscribers,
                analytics.TotalSales,
                analytics.AverageSalePriceStars,
                analytics.ConversionRate);
            
            // Top Content (if available)
            if (analytics.TopOverallContent.Any())
            {
                message += await _localizationService.GetStringAsync("dashboard.top_content", cancellationToken);
                var topItems = analytics.TopOverallContent.Take(3);
                var rank = 1;
                foreach (var item in topItems)
                {
                    message += $"   {rank}. {item.ContentName} - {item.MetricValue} sales\n";
                    rank++;
                }
                message += "\n";
            }

            // Payout History (last 3)
            if (analytics.PayoutHistory.Any())
            {
                message += await _localizationService.GetStringAsync("dashboard.recent_payouts", cancellationToken);
                var recentPayouts = analytics.PayoutHistory.Take(3);
                foreach (var payout in recentPayouts)
                {
                    var payoutDate = payout.PayoutDate.ToString("yyyy-MM-dd");
                    message += $"   • {payoutDate}: {payout.AmountStars:N0} ⭐️ ({payout.Status})\n";
                }
                message += "\n";
            }
            else
            {
                message += await _localizationService.GetStringAsync("dashboard.no_payouts", cancellationToken);
            }

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            // Row 1: Upload Content
            var uploadPremiumText = await _localizationService.GetStringAsync("dashboard.upload_premium", cancellationToken);
            var uploadDemoText = await _localizationService.GetStringAsync("dashboard.upload_demo", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    uploadPremiumText,
                    "model_upload_premium"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    uploadDemoText,
                    "model_upload_demo")
            });

            // Row 2: Manage Content & Statistics
            var myContentText = await _localizationService.GetStringAsync("dashboard.my_content", cancellationToken);
            var contentStatsText = await _localizationService.GetStringAsync("dashboard.content_stats", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    myContentText,
                    "model_view_content"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    contentStatsText,
                    "model_content_statistics")
            });

            // Row 3: Top Content Analytics
            var topContentText = await _localizationService.GetStringAsync("dashboard.top_content_btn", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    topContentText,
                    "model_top_content")
            });

            // Row 4: Profile Settings
            var aliasButtonText = string.IsNullOrWhiteSpace(model.Alias) 
                ? await _localizationService.GetStringAsync("dashboard.set_alias", cancellationToken)
                : await _localizationService.GetStringAsync("dashboard.change_alias", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    aliasButtonText,
                    "model_set_alias")
            });

            // Row 5: Subscription Management
            var manageSubText = await _localizationService.GetStringAsync("dashboard.manage_subscription", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    manageSubText,
                    "model_manage_subscription")
            });

            // Row 6: Back button
            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "menu_back_main")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_dashboard", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles /mysubscriptions command - View user's model subscriptions
    /// </summary>
    private async Task HandleMySubscriptionsCommandAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _modelSubscriptionService.GetUserSubscriptionsAsync(userId, cancellationToken);
            var subsList = subscriptions.ToList();

            if (!subsList.Any())
            {
                var noSubsMessage = await _localizationService.GetStringAsync("subscription.none", cancellationToken);
                var browseText = await _localizationService.GetStringAsync("menu.browse_models", cancellationToken);
                var noSubsBackText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
                
                var noSubsButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            browseText,
                            "menu_browse_models")
                    },
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            noSubsBackText,
                            "menu_back_main")
                    }
                };
                
                var noSubsKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(noSubsButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noSubsMessage, noSubsKeyboard, cancellationToken);
                return;
            }

            var titleText = await _localizationService.GetStringAsync("subscription.title", cancellationToken);
            var message = titleText + $" ({subsList.Count}):\n\n";

            // Build buttons for each subscribed model
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            foreach (var sub in subsList)
            {
                var model = await _modelService.GetModelByIdAsync(sub.ModelId, cancellationToken);
                if (model == null) continue;

                // Use alias if available, otherwise use DisplayName
                var modelDisplayName = !string.IsNullOrWhiteSpace(model.Alias) 
                    ? model.Alias 
                    : model.DisplayName;

                var statusEmoji = sub.IsValidNow() ? "✅" : "⚠️";
                message += $"{statusEmoji} {modelDisplayName}\n";
                
                var statusText = sub.IsActive 
                    ? await _localizationService.GetStringAsync("subscription.active", sub.SubscriptionPeriod.EndDate.ToString("yyyy-MM-dd"))
                    : await _localizationService.GetStringAsync("subscription.expired", sub.SubscriptionPeriod.EndDate.ToString("yyyy-MM-dd"));
                message += $"   {statusText}\n\n";

                // Add button to view model's content
                var viewContentText = await _localizationService.GetStringAsync("subscription.view_content", modelDisplayName);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        viewContentText,
                        $"view_model_content_{model.Id}")
                });
            }

            // Add back button
            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "menu_back_main")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_subscriptions", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles /admin command - Admin panel
    /// </summary>
    private async Task HandleAdminCommandAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                var noAccessMessage = await _localizationService.GetStringAsync("admin.no_permission", cancellationToken);
                var adminBackText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
                
                var noAccessButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            adminBackText,
                            "menu_back_main")
                    }
                };
                
                var noAccessKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(noAccessButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noAccessMessage, noAccessKeyboard, cancellationToken);
                return;
            }

            var pendingModels = await _modelService.GetPendingApprovalModelsAsync(cancellationToken);
            var pendingList = pendingModels.ToList();

            var message = await _localizationService.GetStringAsync("admin.panel.title", cancellationToken);
            var pendingTitle = await _localizationService.GetStringAsync("admin.pending_approvals.title", pendingList.Count.ToString());
            message += $"\n\n{pendingTitle}\n\n";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            if (pendingList.Any())
            {
                message += "⏳ Models awaiting approval:\n\n";
                foreach (var model in pendingList.Take(5))
                {
                    var user = model.User;
                    var userName = $"{user?.FirstName} {user?.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        userName = "Unknown User";
                    }
                    
                    var usernameLink = !string.IsNullOrWhiteSpace(user?.Username) 
                        ? $"@{user.Username}" 
                        : "No username";
                    
                    // Use alias if available, otherwise use DisplayName
                    var displayName = !string.IsNullOrWhiteSpace(model.Alias) ? model.Alias : model.DisplayName;
                    
                    message += $"👤 {displayName}\n";
                    message += $"   User: {userName}\n";
                    message += $"   Telegram: {usernameLink}\n";
                    message += $"   Registered: {model.CreatedAt:g}\n\n";
                    
                    buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            $"✅ Approve {displayName}",
                            $"admin_approve_{model.Id}"),
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            $"❌ Reject",
                            $"admin_reject_{model.Id}")
                    });
                }
                
                if (pendingList.Count > 5)
                {
                    message += $"... and {pendingList.Count - 5} more\n";
                }
            }
            
            // Add platform settings and refresh buttons
            var settingsText = await _localizationService.GetStringAsync("admin.settings", cancellationToken);
            var refreshText = await _localizationService.GetStringAsync("admin.button.refresh", cancellationToken);
            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    settingsText,
                    "admin_settings"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    refreshText,
                    "menu_admin_panel")
            });
            
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "menu_back_main")
            });

            if (pendingList.Any())
            {
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
            }
            else
            {
                var noPendingMsg = await _localizationService.GetStringAsync("admin.pending_approvals.none", cancellationToken);
                message += noPendingMsg;
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.loading_admin_panel", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles admin settings panel
    /// </summary>
    private async Task HandleAdminSettingsAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _localizationService.GetStringAsync("admin.settings.title", cancellationToken);
            var description = await _localizationService.GetStringAsync("admin.settings.description", cancellationToken);
            message += $"\n\n{description}";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            // MTProto Setup Wizard
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "🔧 Setup MTProto (Wizard)",
                    "mtproto_setup_start"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "🌐 Web Setup",
                    "mtproto_web_setup")
            });
            
            // Single Model Mode Settings
            var isSingleModelMode = await _singleModelModeService.IsSingleModelModeAsync(cancellationToken);
            var singleModeStatus = isSingleModelMode ? "✅ Enabled" : "❌ Disabled";
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    $"🎯 Single Model Mode ({singleModeStatus})",
                    "admin_single_model_settings")
            });

            // MTProto Settings (Individual)
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "📱 MTProto API ID",
                    $"admin_setting_edit_{PlatformSettings.Keys.MtProtoApiId.Replace(":", "_")}"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "🔑 MTProto API Hash",
                    $"admin_setting_edit_{PlatformSettings.Keys.MtProtoApiHash.Replace(":", "_")}")
            });

            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "📞 MTProto Phone",
                    $"admin_setting_edit_{PlatformSettings.Keys.MtProtoPhoneNumber.Replace(":", "_")}")
            });

            // Platform Info
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "🏷️ Platform Name",
                    $"admin_setting_edit_{PlatformSettings.Keys.PlatformName.Replace(":", "_")}"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "⏱️ Self-Destruct Timer",
                    $"admin_setting_edit_{PlatformSettings.Keys.DefaultSelfDestructSeconds.Replace(":", "_")}")
            });

            // Bot Language
            var currentLanguage = await _localizationService.GetBotLanguageAsync(cancellationToken);
            var languageText = currentLanguage == Domain.Enums.BotLanguage.Persian ? "🇮🇷 فارسی" : "🇬🇧 English";
            var languageButtonText = await _localizationService.GetStringAsync("admin.language", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    $"{languageButtonText} ({languageText})",
                    "admin_language_settings")
            });

            // Back button
            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText.Replace("Main Menu", "Admin Panel"),
                    "menu_admin_panel")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error loading settings: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles bot language settings
    /// </summary>
    private async Task HandleAdminLanguageSettingsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var currentLanguage = await _localizationService.GetBotLanguageAsync(cancellationToken);
            var currentLanguageText = currentLanguage == Domain.Enums.BotLanguage.Persian ? "فارسی" : "English";
            
            var message = await _localizationService.GetStringAsync("admin.settings.language.current", currentLanguageText);
            message += "\n\n";
            message += await _localizationService.GetStringAsync("admin.settings.language.select");
            
            // Create buttons in two rows (one button per row for better UX)
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "🇮🇷 فارسی" + (currentLanguage == Domain.Enums.BotLanguage.Persian ? " ✅" : ""),
                        "admin_language_set_Persian")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "🇬🇧 English" + (currentLanguage == Domain.Enums.BotLanguage.English ? " ✅" : ""),
                        "admin_language_set_English")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        await _localizationService.GetStringAsync("menu.back"),
                        "admin_settings")
                }
            };
            
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.setting_language", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Sets the bot language
    /// </summary>
    private async Task HandleAdminSetLanguageAsync(Guid userId, long chatId, Domain.Enums.BotLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            await _localizationService.SetBotLanguageAsync(language, cancellationToken);
            var message = await _localizationService.GetStringAsync("admin.settings.language.updated");
            
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Settings",
                        "admin_settings")
                }
            };
            
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error setting language: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Starts MTProto setup wizard
    /// </summary>
    private async Task HandleMtProtoSetupStartAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can configure MTProto.", cancellationToken);
                return;
            }

            // Clear all existing MTProto settings (including soft-deleted ones) to ensure clean setup
            // This prevents issues with multiple records or stale data
            await _platformSettingsRepository.ClearMtProtoSettingsAsync(cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            Console.WriteLine("✅ Cleared all existing MTProto settings before starting setup");

            var message = "🔧 MTProto Setup Wizard\n\n" +
                         "This wizard will guide you through setting up MTProto credentials.\n\n" +
                         "⚠️ **Note:** All previous MTProto settings have been cleared.\n\n" +
                         "You'll need:\n" +
                         "1️⃣ API ID (from https://my.telegram.org/apps)\n" +
                         "2️⃣ API Hash (from https://my.telegram.org/apps)\n" +
                         "3️⃣ Phone Number (with country code, e.g., +1234567890)\n\n" +
                         "⚠️ Important: After entering credentials, you may need to:\n" +
                         "• Enter verification code sent to your phone/app\n" +
                         "• Enter 2FA password (if enabled)\n\n" +
                         "Use /auth_code <code> and /auth_password <password> commands if needed.\n\n" +
                         "Let's start! Please send your **API ID**:";

            // Set user state to step 1: API ID
            await _userStateRepository.SetStateAsync(
                userId,
                Domain.Enums.UserStateType.MtProtoSetupApiId,
                null,
                10, // 10 minutes timeout
                cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Cancel",
                        "admin_settings")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Prompts admin to edit a specific setting
    /// </summary>
    private async Task HandleSettingEditPromptAsync(Guid userId, long chatId, string keyWithUnderscores, CancellationToken cancellationToken)
    {
        try
        {
            var key = keyWithUnderscores.Replace("_", ":");
            var currentValue = await _platformSettingsRepository.GetValueAsync(key, cancellationToken);
            var isSecret = PlatformSettings.Keys.IsSecretKey(key);

            var message = $"⚙️ Edit Setting\n\n" +
                         $"Key: `{key}`\n" +
                         $"Current Value: {(isSecret ? "***" : currentValue ?? "(not set)")}\n\n" +
                         "Please send the new value for this setting:\n\n" +
                         "💡 Tip: Send /cancel to return to settings menu";

            // Set user state to editing this setting
            await _userStateRepository.SetStateAsync(
                userId,
                Domain.Enums.UserStateType.EditingPlatformSetting,
                key,
                5, // 5 minutes
                cancellationToken);
            
            // Save the state to database immediately
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Cancel",
                        "admin_settings")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles model subscription purchase
    /// </summary>
    private async Task HandleSubscribeToModelAsync(Guid userId, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByIdAsync(modelId, cancellationToken);
            if (model == null || !model.CanAcceptSubscriptions())
            {
                await _telegramBotService.SendMessageAsync(chatId, " Model not available for subscriptions.", cancellationToken);
                return;
            }

            // Check if already subscribed
            var hasSubscription = await _modelSubscriptionService.HasActiveSubscriptionAsync(userId, modelId, cancellationToken);
            if (hasSubscription)
            {
                // Use alias if available, otherwise use DisplayName
                var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                    ? model.Alias 
                    : model.DisplayName;
                
                await _telegramBotService.SendMessageAsync(chatId, $" You already have an active subscription to {modelDisplayText}!", cancellationToken);
                return;
            }

            // For now, create a test subscription (in production, this would integrate with Telegram Stars payment)
            var subscription = await _modelSubscriptionService.CreateSubscriptionAsync(
                userId,
                modelId,
                model.SubscriptionPrice!,
                cancellationToken);

            // Use alias if available, otherwise use DisplayName
            var displayText = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;
            
            await _telegramBotService.SendMessageAsync(
                chatId,
                $" Successfully subscribed to {displayText}!\n\n" +
                $"Duration: {model.SubscriptionDurationDays} days\n" +
                $"Expires: {subscription.SubscriptionPeriod.EndDate:d}\n\n" +
                $" You now have access to all of {displayText}'s premium content!",
                cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.subscribing", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles admin model approval
    /// </summary>
    private async Task HandleAdminApproveModelAsync(Guid adminId, Guid modelId, long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByIdAsync(modelId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            await _modelService.ApproveModelAsync(modelId, adminId, cancellationToken);
            
            // Get applicant info for admin notification
            var applicantUser = await _userRepository.GetByIdAsync(model.UserId, cancellationToken);
            var applicantName = $"{applicantUser?.FirstName} {applicantUser?.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(applicantName))
            {
                applicantName = "Unknown User";
            }
            var usernameLink = !string.IsNullOrWhiteSpace(applicantUser?.Username) 
                ? $"@{applicantUser.Username}" 
                : "";

            // Edit the original notification message to show approval status
            var updatedMessage = "✅ APPROVED\n\n" +
                                $"Display Name: {model.DisplayName}\n" +
                                $"User: {applicantName}";
            if (!string.IsNullOrWhiteSpace(usernameLink))
            {
                updatedMessage += $"\nTelegram: {usernameLink}";
            }
            updatedMessage += $"\n\nApproved by: Admin\n" +
                            $"Date: {DateTime.UtcNow:g} UTC\n\n" +
                            "The model can now start selling content.";
            
            await _telegramBotService.EditMessageTextAndRemoveKeyboardAsync(chatId, messageId, updatedMessage, cancellationToken);

            // Notify the applicant
            if (applicantUser != null)
            {
                try
                {
                    var applicantMessage = "🎉 Congratulations! Your model registration has been approved!\n\n" +
                                          $"Display Name: {model.DisplayName}\n\n" +
                                          "You can now:\n" +
                                          "• Set your subscription prices\n" +
                                          "• Upload premium content\n" +
                                          "• Manage your creator profile\n\n" +
                                          "Access your Model Dashboard from the main menu!";

                    var applicantButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                    {
                        new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                "📊 Model Dashboard",
                                "menu_model_dashboard")
                        },
                        new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                "<< Back to Main Menu",
                                "menu_back_main")
                        }
                    };

                    var applicantKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(applicantButtons);
                    await _telegramBotService.SendMessageWithButtonsAsync(
                        applicantUser.TelegramUserId.Value,
                        applicantMessage,
                        applicantKeyboard,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to notify applicant: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            await _telegramBotService.SendMessageAsync(chatId, "❌ You don't have permission to approve models.", cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.approving_model", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles admin model rejection
    /// </summary>
    private async Task HandleAdminRejectModelAsync(Guid adminId, Guid modelId, long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByIdAsync(modelId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            await _modelService.RejectModelAsync(modelId, adminId, "Rejected by admin via bot", cancellationToken);
            
            // Get applicant info for admin notification
            var applicantUser = await _userRepository.GetByIdAsync(model.UserId, cancellationToken);
            var applicantName = $"{applicantUser?.FirstName} {applicantUser?.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(applicantName))
            {
                applicantName = "Unknown User";
            }
            var usernameLink = !string.IsNullOrWhiteSpace(applicantUser?.Username) 
                ? $"@{applicantUser.Username}" 
                : "";

            // Edit the original notification message to show rejection status
            var updatedMessage = "❌ REJECTED\n\n" +
                                $"Display Name: {model.DisplayName}\n" +
                                $"User: {applicantName}";
            if (!string.IsNullOrWhiteSpace(usernameLink))
            {
                updatedMessage += $"\nTelegram: {usernameLink}";
            }
            updatedMessage += $"\n\nRejected by: Admin\n" +
                            $"Date: {DateTime.UtcNow:g} UTC\n" +
                            "Reason: Rejected by admin";
            
            await _telegramBotService.EditMessageTextAndRemoveKeyboardAsync(chatId, messageId, updatedMessage, cancellationToken);

            // Notify the applicant
            if (applicantUser != null)
            {
                try
                {
                    var applicantMessage = "❌ Your model registration has been rejected.\n\n" +
                                          $"Display Name: {model.DisplayName}\n" +
                                          "Reason: Rejected by admin\n\n" +
                                          "If you believe this was a mistake, please contact support.";

                    var applicantButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                    {
                        new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                "<< Back to Main Menu",
                                "menu_back_main")
                        }
                    };

                    var applicantKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(applicantButtons);
                    await _telegramBotService.SendMessageWithButtonsAsync(
                        applicantUser.TelegramUserId.Value,
                        applicantMessage,
                        applicantKeyboard,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to notify applicant: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            await _telegramBotService.SendMessageAsync(chatId, "❌ You don't have permission to reject models.", cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.rejecting_model", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles model re-application after rejection
    /// </summary>
    private async Task HandleReapplyModelAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"🔄 User {userId} is reapplying for model registration.");
            
            // Get the user's existing model (should be rejected)
            var oldModel = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            
            if (oldModel == null)
            {
                Console.WriteLine($"❌ No existing model found for user {userId}. Creating new registration instead.");
                // If no model found, just create a new one directly
                await CreateNewModelRegistrationAsync(userId, chatId, cancellationToken);
                return;
            }
            
            Console.WriteLine($"✅ Found old model: ID={oldModel.Id}, UserId={oldModel.UserId}, Status={oldModel.Status}");
            
            if (oldModel.Status != Domain.Entities.ModelStatus.Rejected)
            {
                Console.WriteLine($"❌ Model status is {oldModel.Status}, not Rejected. Cannot reapply.");
                await _telegramBotService.SendMessageAsync(chatId, "Only rejected applications can be resubmitted.", cancellationToken);
                return;
            }

            var oldModelId = oldModel.Id;

            // Get user and prepare for cleanup
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
                return;
            }

            // Prepare changes but don't save yet - do it all in one transaction
            // 1. Soft delete the old rejected model first
            var modelEntity = await _modelRepository.GetByIdAsync(oldModelId, cancellationToken);
            if (modelEntity != null)
            {
                modelEntity.MarkAsDeleted();
                await _modelRepository.UpdateAsync(modelEntity, cancellationToken);
                Console.WriteLine($"🗑️ Marked old model {oldModelId} for deletion");
            }

            // 2. Clear the user's reference to the old model
            if (user.ModelId == oldModelId)
            {
                user.DemoteToUser();
                await _userRepository.UpdateAsync(user, cancellationToken);
                Console.WriteLine($"👤 Cleared user's model reference");
            }

            // 3. Save all changes in one transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"✅ Old model deleted and user reference cleared");

            // Create new model registration
            await CreateNewModelRegistrationAsync(userId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error handling model reapplication: {ex.Message}");
            var errorMsg = await _localizationService.GetStringAsync("error.reapplication", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a new model registration for the user
    /// </summary>
    private async Task CreateNewModelRegistrationAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            await _telegramBotService.SendMessageAsync(chatId, "User not found.", cancellationToken);
            return;
        }

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user.Username ?? $"Model_{userId.ToString().Substring(0, 8)}";
        }

        // Create new model registration
        var newModel = await _modelService.RegisterModelAsync(userId, displayName, "New content creator", cancellationToken);

        var successMessage = "✅ New application submitted successfully!\n\n" +
                            $"Display Name: {newModel.DisplayName}\n\n" +
                            "⏳ Your application is now pending admin approval.\n\n" +
                            "An admin will review your new application and you'll be notified once approved.\n\n" +
                            $"Application ID: {newModel.Id.ToString().Substring(0, 8)}...";
        
        var successButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
        {
            new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "<< Back to Main Menu",
                    "menu_back_main")
            }
        };
        
        var successKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(successButtons);
        await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, successKeyboard, cancellationToken);
        
        // Notify all admins about the new model registration
        Console.WriteLine($"📢 About to notify admins. Model ID: {newModel.Id}");
        try
        {
            await NotifyAdminsAboutNewModelAsync(newModel, user, cancellationToken);
            Console.WriteLine("✅ Admin notification method completed");
        }
        catch (Exception notifyEx)
        {
            Console.WriteLine($"❌ FAILED to notify admins: {notifyEx.Message}");
            Console.WriteLine($"Stack trace: {notifyEx.StackTrace}");
        }
    }

    #region Model Content Management

    /// <summary>
    /// Handles premium content upload
    /// </summary>
    private async Task HandleModelUploadPremiumAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null || model.Status != Domain.Entities.ModelStatus.Approved)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Only approved models can upload content.", cancellationToken);
                return;
            }

            var message = await _localizationService.GetStringAsync("upload.title", cancellationToken);
            message += "\n\n";
            message += await _localizationService.GetStringAsync("upload.instructions", cancellationToken);

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            // Set user state to expect media upload
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.UploadingPremiumMedia, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles demo content upload
    /// </summary>
    private async Task HandleModelUploadDemoAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null || model.Status != Domain.Entities.ModelStatus.Approved)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Only approved models can upload content.", cancellationToken);
                return;
            }

            var message = "🎁 Upload Demo Content\n\n" +
                         "Send me a photo or video to use as a FREE demo/preview.\n\n" +
                         "Demo content:\n" +
                         "• Visible to all users\n" +
                         "• One-time view only per user\n" +
                         "• Helps users decide to subscribe\n\n" +
                         "⚠️ Note: Each model can have only ONE demo. Uploading a new one will replace the old one.\n\n" +
                         "📸 Send your demo media now:";

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            // Set user state to expect demo upload
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.UploadingDemoMedia, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Shows model's content list
    /// </summary>
    private async Task HandleModelViewContentAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var premiumPhotos = await _modelDiscoveryService.GetModelPhotosAsync(model.Id, Domain.Enums.PhotoType.Premium, cancellationToken: cancellationToken);
            var demoPhotos = await _modelDiscoveryService.GetModelPhotosAsync(model.Id, Domain.Enums.PhotoType.Demo, cancellationToken: cancellationToken);
            
            var premiumList = premiumPhotos.ToList();
            var demoList = demoPhotos.ToList();

            var message = $"📋 Your Content\n\n";
            
            if (demoList.Any())
            {
                message += "🎁 Demo Content:\n";
                foreach (var photo in demoList)
                {
                    message += $"  • {photo.Caption ?? "No caption"} (Demo)\n";
                }
                message += "\n";
            }
            else
            {
                message += "🎁 No demo content yet\n\n";
            }

            message += $"📸 Premium Content: {premiumList.Count} items\n\n";
            
            if (premiumList.Any())
            {
                foreach (var photo in premiumList.Take(10))
                {
                    message += $"  • {photo.Caption ?? "No caption"} - {photo.Price} stars\n";
                }
                if (premiumList.Count > 10)
                {
                    message += $"\n... and {premiumList.Count - 10} more\n";
                }
            }

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "📊 Back to Dashboard",
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Shows edit content menu
    /// </summary>
    private async Task HandleModelEditContentMenuAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var photos = await _modelDiscoveryService.GetModelPhotosAsync(model.Id, Domain.Enums.PhotoType.Premium, cancellationToken: cancellationToken);
            var photosList = photos.Take(20).ToList();

            if (!photosList.Any())
            {
                var noContentMessage = "You don't have any premium content yet.\n\n" +
                                      "Upload some content first!";
                
                var noContentButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
                {
                    new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            "📊 Back to Dashboard",
                            "menu_model_dashboard")
                    }
                };
                
                var noContentKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(noContentButtons);
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, noContentMessage, noContentKeyboard, cancellationToken);
                return;
            }

            var message = "✏️ Edit Content\n\n" +
                         "Select content to edit or delete:\n\n";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            foreach (var photo in photosList.Take(10))
            {
                var buttonText = $"{photo.Caption ?? "No caption"} - {photo.Price} stars";
                if (buttonText.Length > 60)
                {
                    buttonText = buttonText.Substring(0, 57) + "...";
                }
                
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        buttonText,
                        $"edit_photo_{photo.Id}")
                });
            }

            // Back button
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "📊 Back to Dashboard",
                    "menu_model_dashboard")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Shows subscription management
    /// </summary>
    private async Task HandleModelManageSubscriptionAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var stats = await _modelDiscoveryService.GetModelStatisticsAsync(model.Id, cancellationToken);

            var message = "💳 Subscription Management\n\n";

            if (stats.HasSubscriptionAvailable && stats.SubscriptionPrice.HasValue)
            {
                message += $"Current Plan:\n" +
                          $"💰 Price: {stats.SubscriptionPrice} stars\n" +
                          $"⏱️ Duration: {stats.SubscriptionDurationDays} days\n" +
                          $"👥 Subscribers: {stats.TotalSubscribers}\n\n" +
                          "Click below to update your subscription plan:";
            }
            else
            {
                message += "⚠️ You haven't set up a subscription plan yet.\n\n" +
                          "A subscription allows users to access all your premium content for a monthly fee.\n\n" +
                          "Click below to create your subscription plan:";
            }

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        stats.HasSubscriptionAvailable ? "✏️ Update Plan" : "➕ Create Plan",
                        "model_set_subscription")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "📊 Back to Dashboard",
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Prompts user to set subscription price and duration
    /// </summary>
    private async Task HandleModelSetSubscriptionPromptAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                return;
            }

            var message = "💳 Set Subscription Plan\n\n" +
                         "Please reply with the subscription details in this format:\n\n" +
                         "`<price> <duration>`\n\n" +
                         "Examples:\n" +
                         "• `1000 30` - 1000 stars for 30 days\n" +
                         "• `500 7` - 500 stars for 7 days\n" +
                         "• `2000 90` - 2000 stars for 90 days\n\n" +
                         "Send your subscription plan now:";

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            // Set user state to expect subscription input
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.SettingSubscriptionPlan, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Shows edit options for a photo
    /// </summary>
    private async Task HandleEditPhotoOptionsAsync(Guid userId, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Verify ownership
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to edit this content.", cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                return;
            }

            var message = $"✏️ Edit Content\n\n" +
                         $"Caption: {photo.Caption ?? "No caption"}\n" +
                         $"Price: {photo.Price.Amount} stars\n" +
                         $"Type: {photo.Type}\n\n" +
                         "What would you like to do?";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "✏️ Edit Caption",
                        $"edit_caption_{photoId}"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "💰 Edit Price",
                        $"edit_price_{photoId}")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "🗑️ Delete Content",
                        $"delete_photo_{photoId}")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Edit Menu",
                        "model_edit_content")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles photo deletion
    /// </summary>
    private async Task HandleDeletePhotoAsync(Guid userId, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            // Verify ownership
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to delete this content.", cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                return;
            }

            // Soft delete the photo
            photo.MarkAsDeleted();
            await _photoRepository.UpdateAsync(photo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var message = "✅ Content deleted successfully!\n\n" +
                         "The content has been removed from your catalog.";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "📊 Back to Dashboard",
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.deleting_content", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Prompts user to edit caption
    /// </summary>
    private async Task HandleEditCaptionPromptAsync(Guid userId, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to edit this content.", cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                return;
            }

            var message = $"✏️ Edit Caption\n\n" +
                         $"Current caption: {photo.Caption ?? "No caption"}\n\n" +
                         "Please reply with the new caption for this content.\n\n" +
                         $"Photo ID (for reference): {photoId.ToString().Substring(0, 8)}...";

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            // Set user state to expect caption input with photoId
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.EditingCaption, photoId.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Prompts user to edit price
    /// </summary>
    private async Task HandleEditPricePromptAsync(Guid userId, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to edit this content.", cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                return;
            }

            var message = $"💰 Edit Price\n\n" +
                         $"Current price: {photo.Price.Amount} stars\n\n" +
                         "Please reply with the new price in Telegram Stars.\n\n" +
                         "Examples: `1000`, `500`, `2500`\n\n" +
                         $"Photo ID (for reference): {photoId.ToString().Substring(0, 8)}...";

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
            
            // Set user state to expect price input with photoId
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.EditingPrice, photoId.ToString(), cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    #endregion

    #region Contact Verification Helpers

    /// <summary>
    /// Sends admin notification for manual contact add requirement
    /// </summary>
    private async Task SendAdminNotificationAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Get admin IDs from platform settings
            var adminIdsString = await _platformSettingsRepository.GetValueAsync("admin:user_ids", cancellationToken);
            
            if (string.IsNullOrWhiteSpace(adminIdsString))
            {
                Console.WriteLine("⚠️ No admin IDs configured in platform settings");
                return;
            }

            // Parse comma-separated admin IDs
            var adminIds = adminIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => long.TryParse(id.Trim(), out var telegramId) ? telegramId : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (!adminIds.Any())
            {
                Console.WriteLine("⚠️ No valid admin IDs found");
                return;
            }

            // Send to all admins
            foreach (var adminId in adminIds)
            {
                try
                {
                    await _telegramBotService.SendMessageAsync(
                        adminId,
                        message,
                        cancellationToken);
                    
                    Console.WriteLine($"✅ Admin notification sent to {adminId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to send admin notification to {adminId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending admin notifications: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles contact verification result and notifies admin if needed
    /// </summary>
    private async Task HandleContactVerificationResultAsync(
        ContentDeliveryResult deliveryResult,
        long userChatId,
        CancellationToken cancellationToken)
    {
        if (deliveryResult.VerificationResult == null)
        {
            return;
        }

        var verificationResult = deliveryResult.VerificationResult;

        // Send instruction message to user
        if (!string.IsNullOrWhiteSpace(verificationResult.UserInstructionMessage))
        {
            await _telegramBotService.SendMessageAsync(
                userChatId,
                verificationResult.UserInstructionMessage,
                cancellationToken);
        }

        // Notify admin if needed
        if (verificationResult.ShouldNotifyAdmin && 
            !string.IsNullOrWhiteSpace(verificationResult.AdminNotificationMessage))
        {
            await SendAdminNotificationAsync(verificationResult.AdminNotificationMessage, cancellationToken);
        }
    }

    #endregion
    
    #region Single Model Mode Admin Methods

    /// <summary>
    /// Shows Single Model Mode settings menu
    /// </summary>
    private async Task HandleAdminSingleModelSettingsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ You don't have admin permissions.", cancellationToken);
                return;
            }

            var isSingleModelMode = await _singleModelModeService.IsSingleModelModeAsync(cancellationToken);
            var defaultModel = await _singleModelModeService.GetDefaultModelAsync(cancellationToken);

            var message = "🎯 Single Model Mode Settings\n\n";
            
            if (isSingleModelMode && defaultModel != null)
            {
                // Use alias if available, otherwise use DisplayName
                var modelDisplayText = !string.IsNullOrWhiteSpace(defaultModel.Alias) 
                    ? defaultModel.Alias 
                    : defaultModel.DisplayName;
                
                message += $"✅ Status: Enabled\n";
                message += $"📸 Active Model: {modelDisplayText}\n";
                message += $"👤 Bio: {defaultModel.Bio ?? "No bio"}\n\n";
                message += "In Single Model Mode:\n";
                message += "• Users see only this model's content\n";
                message += "• Browse Models option is hidden\n";
                message += "• New model registrations are hidden\n\n";
                message += "Choose an action:";
            }
            else
            {
                message += $"❌ Status: Disabled\n\n";
                message += "Single Model Mode allows your bot to operate exclusively for one content creator.\n\n";
                message += "Benefits:\n";
                message += "• Simplified user experience\n";
                message += "• Direct access to model's content\n";
                message += "• Perfect for individual creators\n\n";
                message += "Choose a model to enable:";
            }

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            if (isSingleModelMode && defaultModel != null)
            {
                // Disable button
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Disable Single Model Mode",
                        "admin_single_model_disable")
                });
            }
            else
            {
                // Show all approved models to choose from
                var allModels = await _modelService.GetApprovedModelsAsync(cancellationToken);
                var modelsList = allModels.ToList();

                if (modelsList.Any())
                {
                    foreach (var model in modelsList.Take(10))
                    {
                        // Use alias if available, otherwise use DisplayName
                        var modelDisplayText = !string.IsNullOrWhiteSpace(model.Alias) 
                            ? model.Alias 
                            : model.DisplayName;
                        
                        buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                        {
                            Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                                $"✅ Enable for: {modelDisplayText}",
                                $"admin_single_model_enable_{model.Id}")
                        });
                    }
                }
                else
                {
                    message += "\n⚠️ No approved models available.";
                }
            }

            // Back button
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    "<< Back to Settings",
                    "admin_settings")
            });

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Enables Single Model Mode for a specific model
    /// </summary>
    private async Task HandleAdminEnableSingleModelModeAsync(Guid userId, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ You don't have admin permissions.", cancellationToken);
                return;
            }

            await _singleModelModeService.EnableSingleModelModeAsync(modelId, cancellationToken);
            
            var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
            
            // Use alias if available, otherwise use DisplayName
            var modelDisplayText = model != null && !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model?.DisplayName ?? "Unknown";
            
            var successMessage = $"✅ Single Model Mode Enabled!\n\n" +
                                $"📸 Active Model: {modelDisplayText}\n\n" +
                                "The bot will now operate exclusively for this model.\n" +
                                "Users will see their content directly on the main menu.";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "⚙️ Single Model Settings",
                        "admin_single_model_settings")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Main Menu",
                        "menu_back_main")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error enabling Single Model Mode: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Disables Single Model Mode
    /// </summary>
    private async Task HandleAdminDisableSingleModelModeAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ You don't have admin permissions.", cancellationToken);
                return;
            }

            await _singleModelModeService.DisableSingleModelModeAsync(cancellationToken);
            
            var successMessage = "✅ Single Model Mode Disabled!\n\n" +
                                "The bot will now show all models.\n" +
                                "Users can browse and subscribe to multiple creators.";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "⚙️ Single Model Settings",
                        "admin_single_model_settings")
                },
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Main Menu",
                        "menu_back_main")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error disabling Single Model Mode: {ex.Message}", cancellationToken);
        }
    }

    #endregion

    #region Model Alias Methods

    /// <summary>
    /// Prompts model to set their alias
    /// </summary>
    private async Task HandleModelSetAliasPromptAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ You are not registered as a model.", cancellationToken);
                return;
            }

            var message = "🏷️ Set Your Alias\n\n";
            
            if (!string.IsNullOrWhiteSpace(model.Alias))
            {
                message += $"Current alias: {model.Alias}\n\n";
            }
            
            message += "An alias is a friendly nickname that will be displayed alongside your display name.\n\n" +
                      "✨ Examples: \"The Artist\", \"Photography Pro\", \"Creative Soul\"\n\n" +
                      "Send me your desired alias, or send /cancel to go back.\n" +
                      "💡 Tip: Send \"clear\" to remove your current alias.";

            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.SettingModelAlias, null, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    #endregion

    #region Model Content Statistics & Analytics Methods

    /// <summary>
    /// Shows detailed content statistics for the model
    /// </summary>
    private async Task HandleModelContentStatisticsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                var notModelMsg = await _localizationService.GetStringAsync("content_stats.not_model", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, notModelMsg, cancellationToken);
                return;
            }

            var analytics = await _revenueAnalyticsService.GetModelRevenueAnalyticsAsync(model.Id, cancellationToken);

            if (!analytics.ContentStatistics.Any())
            {
                var noContentMsg = await _localizationService.GetStringAsync("content_stats.no_content", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, noContentMsg, cancellationToken);
                return;
            }

            var message = await _localizationService.GetStringAsync("content_stats.title", cancellationToken);

            foreach (var content in analytics.ContentStatistics.OrderByDescending(c => c.RevenueStars).Take(20))
            {
                message += $"📸 **{content.ContentName}**\n";
                
                var viewsText = await _localizationService.GetStringAsync("content_stats.views", content.Views.ToString());
                message += $"{viewsText}\n";
                
                var purchasesText = await _localizationService.GetStringAsync("content_stats.purchases", content.Purchases.ToString());
                message += $"{purchasesText}\n";
                
                var revenueText = await _localizationService.GetStringAsync("content_stats.revenue", content.RevenueStars.ToString("N0"));
                message += $"{revenueText}\n";
                
                var conversionText = await _localizationService.GetStringAsync("content_stats.conversion", content.ConversionRate.ToString("F2"));
                message += $"{conversionText}\n\n";
            }

            if (analytics.ContentStatistics.Count > 20)
            {
                var moreItemsText = await _localizationService.GetStringAsync("content_stats.more_items", (analytics.ContentStatistics.Count - 20).ToString());
                message += moreItemsText;
            }

            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        backText,
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.generic", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows top performing content items
    /// </summary>
    private async Task HandleModelTopContentAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                var notModelMsg = await _localizationService.GetStringAsync("content_stats.not_model", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, notModelMsg, cancellationToken);
                return;
            }

            var analytics = await _revenueAnalyticsService.GetModelRevenueAnalyticsAsync(model.Id, cancellationToken);

            var message = await _localizationService.GetStringAsync("top_content.title", cancellationToken);

            // Top Overall
            if (analytics.TopOverallContent.Any())
            {
                var allTimeText = await _localizationService.GetStringAsync("top_content.all_time", cancellationToken);
                message += $"{allTimeText}\n";
                var rank = 1;
                foreach (var item in analytics.TopOverallContent.Take(10))
                {
                    message += $"{rank}. {item.ContentName} - {item.MetricValue} {item.MetricType.ToLower()}\n";
                    rank++;
                }
                message += "\n";
            }

            // Top Monthly
            if (analytics.TopMonthlyContent.Any())
            {
                var thisMonthText = await _localizationService.GetStringAsync("top_content.this_month", cancellationToken);
                message += $"{thisMonthText}\n";
                var rank = 1;
                foreach (var item in analytics.TopMonthlyContent.Take(10))
                {
                    message += $"{rank}. {item.ContentName} - {item.MetricValue} {item.MetricType.ToLower()}\n";
                    rank++;
                }
                message += "\n";
            }

            // Top Yearly
            if (analytics.TopYearlyContent.Any())
            {
                var thisYearText = await _localizationService.GetStringAsync("top_content.this_year", cancellationToken);
                message += $"{thisYearText}\n";
                var rank = 1;
                foreach (var item in analytics.TopYearlyContent.Take(10))
                {
                    message += $"{rank}. {item.ContentName} - {item.MetricValue} {item.MetricType.ToLower()}\n";
                    rank++;
                }
            }

            if (!analytics.TopOverallContent.Any() && !analytics.TopMonthlyContent.Any() && !analytics.TopYearlyContent.Any())
            {
                message = await _localizationService.GetStringAsync("top_content.no_data", cancellationToken);
            }

            var backText = await _localizationService.GetStringAsync("menu.back", cancellationToken);
            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        backText,
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("error.generic", ex.Message);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    #endregion

    #endregion
}
