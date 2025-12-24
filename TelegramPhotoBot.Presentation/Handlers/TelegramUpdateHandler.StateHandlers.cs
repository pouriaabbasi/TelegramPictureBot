using System.Text.Json;
using Telegram.Bot.Types;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using TelegramPhotoBot.Presentation.DTOs;

namespace TelegramPhotoBot.Presentation.Handlers;

/// <summary>
/// Partial class containing state-based input handlers for model content management
/// </summary>
public partial class TelegramUpdateHandler
{
    /// <summary>
    /// Handles premium media upload (photo or video)
    /// </summary>
    private async Task HandlePremiumMediaUploadAsync(Guid userId, long chatId, TelegramMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Check if message contains photo or video
            string? fileId = null;
            string? fileType = null;

            if (message.Photo != null && message.Photo.Any())
            {
                // Get the largest photo
                var photo = message.Photo.OrderByDescending(p => p.FileSize).First();
                fileId = photo.FileId;
                fileType = "photo";
            }
            else if (message.Video != null)
            {
                fileId = message.Video.FileId;
                fileType = "video";
            }

            if (fileId == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Please send a photo or video.", cancellationToken);
                return;
            }

            // Store media info in state and ask for price
            var mediaData = JsonSerializer.Serialize(new { fileId, fileType });
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.SettingPremiumMediaPrice, mediaData, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _telegramBotService.SendMessageAsync(chatId, 
                "✅ Media received!\n\n💰 Now, please send the price in Telegram Stars.\n\nExamples: `1000`, `500`, `2500`", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling premium media upload: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles demo media upload
    /// </summary>
    private async Task HandleDemoMediaUploadAsync(Guid userId, long chatId, TelegramMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Check if message contains photo or video
            string? fileId = null;
            string? fileType = null;

            if (message.Photo != null && message.Photo.Any())
            {
                var photo = message.Photo.OrderByDescending(p => p.FileSize).First();
                fileId = photo.FileId;
                fileType = "photo";
            }
            else if (message.Video != null)
            {
                fileId = message.Video.FileId;
                fileType = "video";
            }

            if (fileId == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Please send a photo or video.", cancellationToken);
                return;
            }

            // Check if model already has demo content and replace it
            var existingDemo = (await _modelDiscoveryService.GetModelPhotosAsync(model.Id, Domain.Enums.PhotoType.Demo, cancellationToken: cancellationToken)).FirstOrDefault();
            
            if (existingDemo != null)
            {
                // Soft delete existing demo
                existingDemo.MarkAsDeleted();
                await _photoRepository.UpdateAsync(existingDemo, cancellationToken);
            }

            // Create new demo content
            var fileInfo = new Domain.ValueObjects.FileInfo(fileId, fileType);
            var demoPhoto = new Photo(
                fileInfo: fileInfo,
                sellerId: model.UserId,
                modelId: model.Id,
                price: TelegramStars.Zero,
                type: Domain.Enums.PhotoType.Demo,
                caption: "Demo Content"
            );

            await _photoRepository.AddAsync(demoPhoto, cancellationToken);
            
            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = "✅ Demo content uploaded successfully!\n\n" +
                                "This content will be visible to all users as a free preview.";

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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling demo media upload: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles price input for premium media
    /// </summary>
    private async Task HandlePremiumMediaPriceInputAsync(Guid userId, long chatId, string? mediaData, string? priceText, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(priceText) || !int.TryParse(priceText, out var price) || price <= 0)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Invalid price. Please send a positive number (e.g., 1000).", cancellationToken);
                return;
            }

            // Store price and ask for caption
            var combinedData = JsonSerializer.Serialize(new { mediaData, price });
            await _userStateRepository.SetStateAsync(userId, Domain.Enums.UserStateType.SettingPremiumMediaCaption, combinedData, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _telegramBotService.SendMessageAsync(chatId,
                $"✅ Price set to {price} stars!\n\n📝 Now, please send a caption/description for this content.\n\nOr send /skip to upload without a caption.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling premium media price: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Handles caption input for premium media
    /// </summary>
    private async Task HandlePremiumMediaCaptionInputAsync(Guid userId, long chatId, string? combinedData, string? captionText, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(combinedData))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Session expired. Please try again.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var data = JsonSerializer.Deserialize<dynamic>(combinedData);
            var mediaInfo = JsonSerializer.Deserialize<dynamic>(data.GetProperty("mediaData").GetString() ?? "{}");
            var fileId = mediaInfo.GetProperty("fileId").GetString();
            var fileType = mediaInfo.GetProperty("fileType").GetString();
            var price = data.GetProperty("price").GetInt32();

            var caption = captionText?.Equals("/skip", StringComparison.OrdinalIgnoreCase) == true 
                ? null 
                : captionText;

            // Create the premium photo
            var fileInfo = new Domain.ValueObjects.FileInfo(fileId, fileType);
            var premiumPhoto = new Photo(
                fileInfo: fileInfo,
                sellerId: model.UserId,
                modelId: model.Id,
                price: new TelegramStars(price),
                type: Domain.Enums.PhotoType.Premium,
                caption: caption
            );

            await _photoRepository.AddAsync(premiumPhoto, cancellationToken);
            
            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = $"✅ Premium content uploaded successfully!\n\n" +
                                $"💰 Price: {price} stars\n" +
                                $"📝 Caption: {caption ?? "None"}\n\n" +
                                "This content is now available for purchase!";

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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling premium media caption: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles caption edit input
    /// </summary>
    private async Task HandleCaptionEditInputAsync(Guid userId, long chatId, string? photoIdStr, string? newCaption, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(photoIdStr, out var photoId))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Session expired. Please try again.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Verify ownership
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to edit this content.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            photo.UpdateCaption(newCaption);
            await _photoRepository.UpdateAsync(photo, cancellationToken);
            
            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = $"✅ Caption updated successfully!\n\n" +
                                $"New caption: {newCaption ?? "None"}";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "✏️ Edit Again",
                        $"edit_photo_{photoId}"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "📊 Dashboard",
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling caption edit: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles price edit input
    /// </summary>
    private async Task HandlePriceEditInputAsync(Guid userId, long chatId, string? photoIdStr, string? newPriceText, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(photoIdStr, out var photoId))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Session expired. Please try again.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(newPriceText) || !int.TryParse(newPriceText, out var newPrice) || newPrice <= 0)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Invalid price. Please send a positive number (e.g., 1000).", cancellationToken);
                return;
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, cancellationToken);
            if (photo == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Content not found.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Verify ownership
            var isOwner = await _authorizationService.IsPhotoOwnerAsync(userId, photoId, cancellationToken);
            if (!isOwner)
            {
                await _telegramBotService.SendMessageAsync(chatId, "You don't have permission to edit this content.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            photo.UpdatePrice(new TelegramStars(newPrice));
            await _photoRepository.UpdateAsync(photo, cancellationToken);
            
            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = $"✅ Price updated successfully!\n\n" +
                                $"New price: {newPrice} stars";

            var buttons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "✏️ Edit Again",
                        $"edit_photo_{photoId}"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "📊 Dashboard",
                        "menu_model_dashboard")
                }
            };

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(buttons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling price edit: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles subscription plan input
    /// </summary>
    private async Task HandleSubscriptionPlanInputAsync(Guid userId, long chatId, string? inputText, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _modelService.GetModelByUserIdAsync(userId, cancellationToken);
            if (model == null)
            {
                await _telegramBotService.SendMessageAsync(chatId, "Model not found.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(inputText))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Invalid input. Please try again.", cancellationToken);
                return;
            }

            var parts = inputText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var price) || !int.TryParse(parts[1], out var duration))
            {
                await _telegramBotService.SendMessageAsync(chatId, 
                    "❌ Invalid format. Please send: `<price> <duration>`\n\nExample: `1000 30`", 
                    cancellationToken);
                return;
            }

            if (price <= 0 || duration <= 0)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Price and duration must be positive numbers.", cancellationToken);
                return;
            }

            // Update model's subscription plan
            model.SetSubscriptionPricing(new TelegramStars(price), duration);
            await _modelRepository.UpdateAsync(model, cancellationToken);
            
            // Clear state
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = $"✅ Subscription plan updated successfully!\n\n" +
                                $"💰 Price: {price} stars\n" +
                                $"⏱️ Duration: {duration} days\n\n" +
                                "Users can now subscribe to your content!";

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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling subscription plan input: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Handles platform setting value input from admin
    /// </summary>
    private async Task HandlePlatformSettingInputAsync(Guid userId, long chatId, string? settingKey, string? inputValue, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settingKey) || string.IsNullOrWhiteSpace(inputValue))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Invalid input. Please try again.", cancellationToken);
                return;
            }

            // Verify admin
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can edit platform settings.", cancellationToken);
                return;
            }

            // Update the setting
            var description = PlatformSettings.Keys.GetDescription(settingKey);
            var isSecret = PlatformSettings.Keys.IsSecretKey(settingKey);
            
            await _platformSettingsRepository.SetValueAsync(settingKey, inputValue, description, isSecret, cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successMessage = $"✅ Setting updated successfully!\n\n" +
                                $"Key: `{settingKey}`\n" +
                                $"Value: {(isSecret ? "***" : inputValue)}\n\n" +
                                "⚠️ Note: Some changes may require restarting the bot to take effect.";

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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, keyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling platform setting input: {ex.Message}");
            await _telegramBotService.SendMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

