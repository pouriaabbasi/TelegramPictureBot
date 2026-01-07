using Telegram.Bot.Types;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Coupon-related handlers for TelegramUpdateHandler
/// </summary>
public partial class TelegramUpdateHandler
{
    /// <summary>
    /// Shows coupon input prompt for photo purchase
    /// </summary>
    private async Task ShowCouponPromptForPhotoAsync(Guid userId, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null || !photo.IsForSale)
            {
                var notFoundMsg = await _localizationService.GetStringAsync("content.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, notFoundMsg, cancellationToken);
                return;
            }

            var enterCouponMsg = await _localizationService.GetStringAsync("coupon.enter_code", cancellationToken);
            var skipMsg = await _localizationService.GetStringAsync("coupon.skip", cancellationToken);
            
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        skipMsg,
                        $"skip_coupon_photo_{photoId}")
                }
            });

            // Set state to waiting for coupon input
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.EnteringCouponForPhoto,
                photoId.ToString(),
                5, // 5 minutes
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, enterCouponMsg, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows coupon input prompt for subscription purchase
    /// </summary>
    private async Task ShowCouponPromptForSubscriptionAsync(Guid userId, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
            if (model == null || !model.CanAcceptSubscriptions())
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Model not available for subscriptions.", cancellationToken);
                return;
            }

            var enterCouponMsg = await _localizationService.GetStringAsync("coupon.enter_code", cancellationToken);
            var skipMsg = await _localizationService.GetStringAsync("coupon.skip", cancellationToken);
            
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        skipMsg,
                        $"skip_coupon_sub_{modelId}")
                }
            });

            // Set state to waiting for coupon input
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.EnteringCouponForSubscription,
                modelId.ToString(),
                5, // 5 minutes
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, enterCouponMsg, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles coupon code input for photo purchase
    /// </summary>
    private async Task HandleCouponInputForPhotoAsync(Guid userId, string couponCode, Guid photoId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null || !photo.IsForSale)
            {
                var notFoundMsg = await _localizationService.GetStringAsync("content.not_found", cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, notFoundMsg, cancellationToken);
                return;
            }

            // Validate and apply coupon
            var couponRequest = new ApplyCouponRequest
            {
                CouponCode = couponCode,
                UserId = userId,
                OriginalPriceStars = (int)photo.Price.Amount,
                UsageType = CouponUsageType.ContentPurchase,
                PhotoId = photoId,
                ModelId = photo.ModelId
            };

            var couponResult = await _couponService.ValidateAndApplyCouponAsync(couponRequest, cancellationToken);

            if (!couponResult.IsValid)
            {
                // Show error message
                await _telegramBotService.SendMessageAsync(chatId, couponResult.ErrorMessage!, cancellationToken);
                
                // Ask if they want to try again or skip
                var tryAgainMsg = await _localizationService.GetStringAsync("coupon.enter_code", cancellationToken);
                var skipMsg = await _localizationService.GetStringAsync("coupon.skip", cancellationToken);
                
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            skipMsg,
                            $"skip_coupon_photo_{photoId}")
                    }
                });

                await _telegramBotService.SendMessageWithButtonsAsync(chatId, tryAgainMsg, keyboard, cancellationToken);
                return;
            }

            // Coupon valid! Store it temporarily and show payment method selection
            var appliedMsg = await _localizationService.GetStringAsync("coupon.applied", cancellationToken);
            var message = string.Format(appliedMsg, 
                couponResult.Coupon!.DiscountPercentage,
                couponResult.DiscountAmountStars,
                couponResult.FinalPriceStars);

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);

            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Store coupon ID in state for use in payment
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.None,
                $"coupon_{couponResult.Coupon.Id}_{photoId}",
                10, // 10 minutes
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Show payment method selection with discounted price
            await ShowPaymentMethodSelectionWithCouponAsync(userId, photoId, couponResult, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Handles coupon code input for subscription purchase
    /// </summary>
    private async Task HandleCouponInputForSubscriptionAsync(Guid userId, string couponCode, Guid modelId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
            if (model == null || !model.CanAcceptSubscriptions())
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Model not available for subscriptions.", cancellationToken);
                return;
            }

            // Validate and apply coupon
            var couponRequest = new ApplyCouponRequest
            {
                CouponCode = couponCode,
                UserId = userId,
                OriginalPriceStars = (int)(model.SubscriptionPrice?.Amount ?? 0),
                UsageType = CouponUsageType.SubscriptionPurchase,
                PhotoId = null,
                ModelId = modelId
            };

            var couponResult = await _couponService.ValidateAndApplyCouponAsync(couponRequest, cancellationToken);

            if (!couponResult.IsValid)
            {
                // Show error message
                await _telegramBotService.SendMessageAsync(chatId, couponResult.ErrorMessage!, cancellationToken);
                
                // Ask if they want to try again or skip
                var tryAgainMsg = await _localizationService.GetStringAsync("coupon.enter_code", cancellationToken);
                var skipMsg = await _localizationService.GetStringAsync("coupon.skip", cancellationToken);
                
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            skipMsg,
                            $"skip_coupon_sub_{modelId}")
                    }
                });

                await _telegramBotService.SendMessageWithButtonsAsync(chatId, tryAgainMsg, keyboard, cancellationToken);
                return;
            }

            // Coupon valid! Store it temporarily and show payment method selection
            var appliedMsg = await _localizationService.GetStringAsync("coupon.applied", cancellationToken);
            var message = string.Format(appliedMsg,
                couponResult.DiscountAmountStars,
                couponResult.Coupon!.DiscountPercentage,
                couponResult.FinalPriceStars);

            await _telegramBotService.SendMessageAsync(chatId, message, cancellationToken);

            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Store coupon ID in state for use in payment
            await _userStateRepository.SetStateAsync(
                userId,
                UserStateType.None,
                $"coupon_{couponResult.Coupon.Id}_{modelId}",
                10, // 10 minutes
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Show payment method selection with discounted price
            await ShowSubscriptionPaymentMethodWithCouponAsync(userId, modelId, couponResult, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows payment method selection with applied coupon for photo
    /// </summary>
    private async Task ShowPaymentMethodSelectionWithCouponAsync(Guid userId, Guid photoId, ApplyCouponResult couponResult, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
                return;

            // Build payment method selection message with discount info
            var selectMethodMsg = await _localizationService.GetStringAsync("payment.select_method", cancellationToken);
            var contentInfo = await _localizationService.GetStringAsync("payment.content_info", cancellationToken);
            
            var message = selectMethodMsg;
            message += string.Format(contentInfo, photo.Caption ?? "Premium Photo", couponResult.FinalPriceStars);
            message += $"\n🎫 Coupon ({couponResult.Coupon!.DiscountPercentage}% OFF): -{couponResult.DiscountAmountStars} ⭐️";

            // Build keyboard with payment options
            var invoiceMethodText = await _localizationService.GetStringAsync("payment.method_invoice", cancellationToken);
            var starMethodText = await _localizationService.GetStringAsync("payment.method_star", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        invoiceMethodText,
                        $"pay_invoice_photo_{photoId}"),
                },
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        starMethodText,
                        $"pay_star_photo_{photoId}")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Shows payment method selection with applied coupon for subscription
    /// </summary>
    private async Task ShowSubscriptionPaymentMethodWithCouponAsync(Guid userId, Guid modelId, ApplyCouponResult couponResult, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
            if (model == null)
                return;

            var displayText = !string.IsNullOrWhiteSpace(model.Alias) 
                ? model.Alias 
                : model.DisplayName;

            // Build payment method selection message with discount info
            var selectMethodMsg = await _localizationService.GetStringAsync("payment.select_method", cancellationToken);
            var subscriptionInfo = await _localizationService.GetStringAsync("payment.subscription_info", cancellationToken);
            
            var message = selectMethodMsg;
            message += string.Format(subscriptionInfo, displayText, couponResult.FinalPriceStars, model.SubscriptionDurationDays);
            message += $"\n🎫 Coupon ({couponResult.Coupon!.DiscountPercentage}% OFF): -{couponResult.DiscountAmountStars} ⭐️";

            // Build keyboard with payment options
            var invoiceMethodText = await _localizationService.GetStringAsync("payment.method_invoice", cancellationToken);
            var starMethodText = await _localizationService.GetStringAsync("payment.method_star", cancellationToken);

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        invoiceMethodText,
                        $"pay_invoice_sub_{modelId}"),
                },
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        starMethodText,
                        $"pay_star_sub_{modelId}")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = await _localizationService.GetStringAsync("common.error", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
        }
    }
}
