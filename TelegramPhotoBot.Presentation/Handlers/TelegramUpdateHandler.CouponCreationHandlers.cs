using Telegram.Bot.Types;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Enums;
using System.Globalization;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Coupon creation workflow handlers for TelegramUpdateHandler
/// </summary>
public partial class TelegramUpdateHandler
{
    /// <summary>
    /// Handles coupon code input
    /// </summary>
    private async Task HandleCreatingCouponCodeInputAsync(Guid userId, long chatId, string? stateData, string? input, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.code_required", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                return;
            }

            var code = input.Trim().ToUpperInvariant();

            // Validate code format (alphanumeric, 3-20 characters)
            if (code.Length < 3 || code.Length > 20 || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9]+$"))
            {
                var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.invalid_code_format", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                return;
            }

            // Note: Duplicate check will be done in the service when creating the coupon

            // Ask for discount percentage
            var promptMsg = await _localizationService.GetStringAsync("coupon.create.enter_discount", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, promptMsg, cancellationToken);

            // Update state
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.CreatingCouponDiscount,
                $"{stateData}|{code}",
                10,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles discount percentage input
    /// </summary>
    private async Task HandleCreatingCouponDiscountInputAsync(Guid userId, long chatId, string? stateData, string? input, CancellationToken cancellationToken)
    {
        try
        {
            if (!int.TryParse(input, out var discount) || discount <= 0 || discount > 100)
            {
                var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.invalid_discount", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                return;
            }

            // Ask for usage type
            var promptMsg = await _localizationService.GetStringAsync("coupon.create.select_usage_type", cancellationToken);
            var contentText = await _localizationService.GetStringAsync("coupon.type.content", cancellationToken);
            var subscriptionText = await _localizationService.GetStringAsync("coupon.type.subscription", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        contentText,
                        "coupon_create_usage_content")
                },
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        subscriptionText,
                        "coupon_create_usage_subscription")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, promptMsg, keyboard, cancellationToken);

            // Update state
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.CreatingCouponUsageType,
                $"{stateData}|{discount}",
                10,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles usage type selection
    /// </summary>
    private async Task HandleCreatingCouponUsageTypeSelectionAsync(Guid userId, long chatId, string? stateData, CouponUsageType usageType, CancellationToken cancellationToken)
    {
        try
        {
            // Ask for valid from date (optional)
            var promptMsg = await _localizationService.GetStringAsync("coupon.create.enter_valid_from", cancellationToken);
            var skipText = await _localizationService.GetStringAsync("coupon.create.skip_date", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        skipText,
                        "coupon_create_skip_valid_from")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, promptMsg, keyboard, cancellationToken);

            // Update state
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.CreatingCouponValidFrom,
                $"{stateData}|{(int)usageType}",
                10,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles valid from date input
    /// </summary>
    private async Task HandleCreatingCouponValidFromInputAsync(Guid userId, long chatId, string? stateData, string? input, CancellationToken cancellationToken)
    {
        try
        {
            DateTime? validFrom = null;

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (!DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.invalid_date_format", cancellationToken);
                    await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                    return;
                }
                validFrom = parsedDate;
            }

            // Ask for valid to date (optional)
            var promptMsg = await _localizationService.GetStringAsync("coupon.create.enter_valid_to", cancellationToken);
            var skipText = await _localizationService.GetStringAsync("coupon.create.skip_date", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        skipText,
                        "coupon_create_skip_valid_to")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, promptMsg, keyboard, cancellationToken);

            // Update state
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.CreatingCouponValidTo,
                $"{stateData}|{validFrom?.ToString("yyyy-MM-dd") ?? "null"}",
                10,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles valid to date input
    /// </summary>
    private async Task HandleCreatingCouponValidToInputAsync(Guid userId, long chatId, string? stateData, string? input, CancellationToken cancellationToken)
    {
        try
        {
            DateTime? validTo = null;

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (!DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.invalid_date_format", cancellationToken);
                    await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                    return;
                }
                validTo = parsedDate;
            }

            // Ask for max uses (optional)
            var promptMsg = await _localizationService.GetStringAsync("coupon.create.enter_max_uses", cancellationToken);
            var unlimitedText = await _localizationService.GetStringAsync("coupon.create.unlimited_uses", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        unlimitedText,
                        "coupon_create_unlimited_uses")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, promptMsg, keyboard, cancellationToken);

            // Update state
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.CreatingCouponMaxUses,
                $"{stateData}|{validTo?.ToString("yyyy-MM-dd") ?? "null"}",
                10,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles max uses input and creates the coupon
    /// </summary>
    private async Task HandleCreatingCouponMaxUsesInputAsync(Guid userId, long chatId, string? stateData, string? input, CancellationToken cancellationToken)
    {
        try
        {
            int? maxUses = null;

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (!int.TryParse(input, out var parsedMaxUses) || parsedMaxUses <= 0)
                {
                    var errorMsg = await _localizationService.GetStringAsync("coupon.create.error.invalid_max_uses", cancellationToken);
                    await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                    return;
                }
                maxUses = parsedMaxUses;
            }

            // Parse state data
            var parts = stateData?.Split('|') ?? Array.Empty<string>();
            if (parts.Length < 5)
            {
                throw new InvalidOperationException("Invalid state data for coupon creation");
            }

            var ownerData = parts[0]; // "admin" or "model_{modelId}"
            var code = parts[1];
            var discount = int.Parse(parts[2]);
            var usageType = (CouponUsageType)int.Parse(parts[3]);
            var validFromStr = parts[4];
            var validToStr = parts.Length > 5 ? parts[5] : "null";

            DateTime? validFrom = validFromStr != "null" ? DateTime.Parse(validFromStr, CultureInfo.InvariantCulture) : null;
            DateTime? validTo = validToStr != "null" ? DateTime.Parse(validToStr, CultureInfo.InvariantCulture) : null;

            // Determine owner type and model ID
            var isAdmin = ownerData == "admin";
            var ownerType = isAdmin ? CouponOwnerType.Admin : CouponOwnerType.Model;
            Guid? modelId = null;

            if (!isAdmin && ownerData.StartsWith("model_"))
            {
                modelId = Guid.Parse(ownerData.Replace("model_", ""));
            }

            // Create coupon
            var request = new CreateCouponRequest
            {
                Code = code,
                DiscountPercentage = discount,
                UsageType = usageType,
                OwnerType = ownerType,
                ModelId = modelId,
                ValidFrom = validFrom,
                ValidTo = validTo,
                MaxUses = maxUses
            };

            var coupon = await _couponService.CreateCouponAsync(request, cancellationToken);

            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Show success message
            var successMsg = await _localizationService.GetStringAsync("coupon.create.success", code, cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);

            // Show the coupon details
            if (isAdmin)
            {
                await HandleAdminCouponViewAsync(userId, coupon.Id, chatId, cancellationToken);
            }
            else
            {
                await HandleCouponViewAsync(userId, coupon.Id, chatId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, $"{errorMsg}\n{ex.Message}", cancellationToken);
            
            // Clear state on error
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
