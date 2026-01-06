using Telegram.Bot.Types;
using TelegramPhotoBot.Application.DTOs;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Admin coupon management handlers for TelegramUpdateHandler
/// </summary>
public partial class TelegramUpdateHandler
{
    /// <summary>
    /// Shows admin's platform coupon management menu
    /// </summary>
    private async Task HandleAdminManageCouponsAsync(Guid userId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Access denied.", cancellationToken);
                return;
            }

            var coupons = await _couponService.GetAdminCouponsAsync(cancellationToken);
            var couponList = coupons.ToList();

            var title = await _localizationService.GetStringAsync("coupon.admin.manage", cancellationToken);
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
                            $"admin_coupon_view_{coupon.Id}")
                    });
                }
            }

            // Create new coupon button (admins have no limits)
            var createText = await _localizationService.GetStringAsync("coupon.admin.create", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    createText,
                    "admin_coupon_create_start")
            });

            // Back button
            var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "menu_admin_panel")
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
    /// Shows admin coupon details
    /// </summary>
    private async Task HandleAdminCouponViewAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Access denied.", cancellationToken);
                return;
            }

            var coupons = await _couponService.GetAdminCouponsAsync(cancellationToken);
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
                        $"admin_coupon_deactivate_{couponId}")
                });
            }
            else
            {
                var activateText = await _localizationService.GetStringAsync("coupon.activate", cancellationToken);
                buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        activateText,
                        $"admin_coupon_activate_{couponId}")
                });
            }

            // View statistics
            var statsText = await _localizationService.GetStringAsync("coupon.view_stats", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    statsText,
                    $"admin_coupon_stats_{couponId}")
            });

            // Back button
            var backText = await _localizationService.GetStringAsync("common.back_to_main", cancellationToken);
            buttons.Add(new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                    backText,
                    "admin_manage_coupons")
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
    /// Shows admin coupon usage statistics
    /// </summary>
    private async Task HandleAdminCouponStatsAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Access denied.", cancellationToken);
                return;
            }

            var coupons = await _couponService.GetAdminCouponsAsync(cancellationToken);
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
                        $"admin_coupon_view_{couponId}")
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
    /// Activates an admin coupon
    /// </summary>
    private async Task HandleAdminCouponActivateAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Access denied.", cancellationToken);
                return;
            }

            await _couponService.ActivateCouponAsync(couponId, cancellationToken);
            
            var successMsg = await _localizationService.GetStringAsync("coupon.activated", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);
            
            await HandleAdminCouponViewAsync(userId, couponId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Deactivates an admin coupon
    /// </summary>
    private async Task HandleAdminCouponDeactivateAsync(Guid userId, Guid couponId, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Access denied.", cancellationToken);
                return;
            }

            await _couponService.DeactivateCouponAsync(couponId, cancellationToken);
            
            var successMsg = await _localizationService.GetStringAsync("coupon.deactivated", cancellationToken);
            await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);
            
            await HandleAdminCouponViewAsync(userId, couponId, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _telegramBotService.SendMessageAsync(chatId, $"❌ Error: {ex.Message}", cancellationToken);
        }
    }
}
