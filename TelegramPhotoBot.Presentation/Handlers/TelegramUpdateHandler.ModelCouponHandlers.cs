using Telegram.Bot.Types;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Model coupon management handlers for TelegramUpdateHandler
/// </summary>
public partial class TelegramUpdateHandler
{
    /// <summary>
    /// Shows model's coupon management menu
    /// </summary>
    private async Task HandleModelManageCouponsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Model not found.", cancellationToken);
                return;
            }

            var coupons = await _couponService.GetModelCouponsAsync(model.Id, cancellationToken);
            var couponList = coupons.ToList();

            var title = await _localizationService.GetStringAsync("coupon.my_coupons", cancellationToken);
            var message = $"{title}\n\n";

            if (!couponList.Any())
            {
                message += await _localizationService.GetStringAsync("coupon.list_empty", cancellationToken);
            }
            else
            {
                foreach (var coupon in couponList)
                {
                    var status = coupon.IsActive 
                        ? await _localizationService.GetStringAsync("coupon.status.active", cancellationToken)
                        : await _localizationService.GetStringAsync("coupon.status.inactive", cancellationToken);
                    
                    var usageType = coupon.UsageType == CouponUsageType.ContentPurchase
                        ? await _localizationService.GetStringAsync("coupon.type.content", cancellationToken)
                        : await _localizationService.GetStringAsync("coupon.type.subscription", cancellationToken);

                    message += $"🎫 **{coupon.Code}**\n";
                    message += $"   📊 {coupon.DiscountPercentage}% OFF | {usageType}\n";
                    message += $"   🔢 Used: {coupon.CurrentUses}/{(coupon.MaxUses?.ToString() ?? "∞")}\n";
                    message += $"   ✅ {status}\n\n";
                }
            }

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            // Show individual coupon buttons if less than 10
            if (couponList.Count <= 10)
            {
                foreach (var coupon in couponList)
                {
                    buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                    {
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                            $"🎫 {coupon.Code}",
                            $"coupon_view_{coupon.Id}")
                    });
                }
            }

            // Create new coupon button (check limit)
            var canCreate = await _couponService.CanModelCreateCouponAsync(model.Id, cancellationToken);
            if (canCreate)
            {
                var createText = await _localizationService.GetStringAsync("coupon.create", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        createText,
                        "coupon_create_start")
                });
            }
            else
            {
                message += "\n" + await _localizationService.GetStringAsync("coupon.error.max_limit", 5);
            }

            // Back button
            var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "menu_model_dashboard")
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
    /// Shows coupon details and management options
    /// </summary>
    private async Task HandleCouponViewAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Model not found.", cancellationToken);
                return;
            }

            var coupons = await _couponService.GetModelCouponsAsync(model.Id, cancellationToken);
            var coupon = coupons.FirstOrDefault(c => c.Id == couponId);

            if (coupon == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Coupon not found.", cancellationToken);
                return;
            }

            var status = coupon.IsActive 
                ? await _localizationService.GetStringAsync("coupon.status.active", cancellationToken)
                : await _localizationService.GetStringAsync("coupon.status.inactive", cancellationToken);
            
            var usageType = coupon.UsageType == CouponUsageType.ContentPurchase
                ? await _localizationService.GetStringAsync("coupon.type.content", cancellationToken)
                : await _localizationService.GetStringAsync("coupon.type.subscription", cancellationToken);

            var validFrom = coupon.ValidFrom?.ToString("yyyy-MM-dd") ?? "N/A";
            var validTo = coupon.ValidTo?.ToString("yyyy-MM-dd") ?? "N/A";
            var maxUses = coupon.MaxUses?.ToString() ?? "Unlimited";

            var message = await _localizationService.GetStringAsync("coupon.details", 
                coupon.Code,
                coupon.DiscountPercentage,
                usageType,
                validFrom,
                validTo,
                coupon.CurrentUses,
                maxUses,
                status);

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();

            // Toggle active/inactive
            if (coupon.IsActive)
            {
                var deactivateText = await _localizationService.GetStringAsync("coupon.deactivate", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        deactivateText,
                        $"coupon_deactivate_{couponId}")
                });
            }
            else
            {
                var activateText = await _localizationService.GetStringAsync("coupon.activate", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        activateText,
                        $"coupon_activate_{couponId}")
                });
            }

            // View statistics
            var statsText = await _localizationService.GetStringAsync("coupon.view_stats", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    statsText,
                    $"coupon_stats_{couponId}")
            });

            // Back button
            var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "model_manage_coupons")
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
    /// Shows coupon usage statistics
    /// </summary>
    private async Task HandleCouponStatsAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var coupons = await _couponService.GetModelCouponsAsync((await _modelService.GetModelByUserIdAsync(userId, cancellationToken))!.Id, cancellationToken);
            var coupon = coupons.FirstOrDefault(c => c.Id == couponId);

            if (coupon == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Coupon not found.", cancellationToken);
                return;
            }

            var usages = await _couponService.GetCouponUsageStatsAsync(couponId, cancellationToken);
            var usageList = usages.ToList();

            var title = await _localizationService.GetStringAsync("coupon.stats.title", coupon.Code);
            var message = title;

            if (!usageList.Any())
            {
                message += await _localizationService.GetStringAsync("coupon.stats.no_usage", cancellationToken);
            }
            else
            {
                foreach (var usage in usageList.Take(10))
                {
                    var usageText = await _localizationService.GetStringAsync("coupon.stats.usage_item",
                        usage.UserName,
                        usage.UsedAt.ToString("yyyy-MM-dd HH:mm"),
                        usage.OriginalPriceStars,
                        usage.DiscountAmountStars,
                        usage.FinalPriceStars);
                    message += usageText + "\n";
                }

                if (usageList.Count > 10)
                {
                    message += $"\n... and {usageList.Count - 10} more.";
                }
            }

            var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        backText,
                        $"coupon_view_{couponId}")
                }
            });

            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Activates a coupon
    /// </summary>
    private async Task HandleCouponActivateAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            await _couponService.ActivateCouponAsync(couponId, cancellationToken);
            
            var successMsg = await _localizationService.GetStringAsync("coupon.activated", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);
            
            await HandleCouponViewAsync(userId, couponId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Deactivates a coupon
    /// </summary>
    private async Task HandleCouponDeactivateAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            await _couponService.DeactivateCouponAsync(couponId, cancellationToken);
            
            var successMsg = await _localizationService.GetStringAsync("coupon.deactivated", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);
            
            await HandleCouponViewAsync(userId, couponId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }
}
