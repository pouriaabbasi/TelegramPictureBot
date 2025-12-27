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
            // Set MimeType based on fileType for proper media type detection
            var mimeType = fileType == "video" ? "video/mp4" : "image/jpeg";
            var fileInfo = new Domain.ValueObjects.FileInfo(fileId, fileUniqueId: null, filePath: null, mimeType: mimeType);
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

            // Notify admins about new demo media upload (WITHOUT secure mode)
            var uploader = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (uploader != null)
            {
                // Fire and forget - don't wait for admin notifications
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await NotifyAdminsAboutNewMediaAsync(demoPhoto, model, uploader, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error in background admin notification: {ex.Message}");
                    }
                }, cancellationToken);
            }

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
            // Set MimeType based on fileType for proper media type detection
            var mimeType = fileType == "video" ? "video/mp4" : "image/jpeg";
            var fileInfo = new Domain.ValueObjects.FileInfo(fileId, fileUniqueId: null, filePath: null, mimeType: mimeType);
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

            // Notify admins about new premium media upload (WITHOUT secure mode)
            var uploader = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (uploader != null)
            {
                // Fire and forget - don't wait for admin notifications
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await NotifyAdminsAboutNewMediaAsync(premiumPhoto, model, uploader, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error in background admin notification: {ex.Message}");
                    }
                }, cancellationToken);
            }

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
            Console.WriteLine($"🔧 HandlePlatformSettingInputAsync called - Key: {settingKey}, Value: {inputValue}");
            
            // Check for cancel commands
            if (inputValue != null && (inputValue.Equals("/cancel", StringComparison.OrdinalIgnoreCase) || 
                                       inputValue.Equals("cancel", StringComparison.OrdinalIgnoreCase) ||
                                       inputValue.Equals("/back", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("🚫 Cancel command detected");
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await HandleAdminSettingsAsync(chatId, cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(settingKey) || string.IsNullOrWhiteSpace(inputValue))
            {
                var errorMessage = "❌ Invalid input. Please send a valid value or use /cancel to return to settings menu.";
                
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
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
                return;
            }

            // Verify admin
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can edit platform settings.", cancellationToken);
                return;
            }

            // Update the setting
            var description = PlatformSettings.Keys.GetDescription(settingKey);
            var isSecret = PlatformSettings.Keys.IsSecretKey(settingKey);
            
            Console.WriteLine($"💾 Saving setting - Key: {settingKey}, Description: {description}, IsSecret: {isSecret}");
            await _platformSettingsRepository.SetValueAsync(settingKey, inputValue, description, isSecret, cancellationToken);
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"✅ Setting saved successfully");

            var successMessage = $"✅ Setting updated successfully!\n\n" +
                                $"Key: `{settingKey}`\n" +
                                $"Value: {(isSecret ? "***" : inputValue)}\n\n" +
                                "⚠️ Note: Some changes may require restarting the bot to take effect.";

            var successButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "<< Back to Settings",
                        "admin_settings")
                }
            };

            var successKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(successButtons);
            Console.WriteLine($"📤 Sending success message with back button");
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, successMessage, successKeyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling platform setting input: {ex.Message}");
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var errorMessage = $"❌ Error: {ex.Message}\n\nUse /cancel to return to settings menu.";
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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
        }
    }

    /// <summary>
    /// Handles API ID input for MTProto setup
    /// </summary>
    private async Task HandleMtProtoSetupApiIdInputAsync(Guid userId, long chatId, string? apiId, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancel
            if (apiId?.Equals("/cancel", StringComparison.OrdinalIgnoreCase) == true ||
                apiId?.Equals("cancel", StringComparison.OrdinalIgnoreCase) == true ||
                apiId?.Equals("/back", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await HandleAdminSettingsAsync(chatId, cancellationToken);
                return;
            }

            // Validate API ID (should be numeric)
            if (string.IsNullOrWhiteSpace(apiId) || !int.TryParse(apiId.Trim(), out _))
            {
                var errorMessage = "❌ Invalid API ID. Please enter a valid numeric API ID.\n\n" +
                                 "You can get your API ID from: https://my.telegram.org/apps\n\n" +
                                 "💡 Tip: Send /cancel to return to settings menu";

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
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
                return;
            }

            // Verify admin
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can configure MTProto.", cancellationToken);
                return;
            }

            // Store API ID in state and move to next step
            await _userStateRepository.SetStateAsync(
                userId,
                Domain.Enums.UserStateType.MtProtoSetupApiHash,
                apiId.Trim(),
                10, // 10 minutes timeout
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var message = $"✅ API ID saved: {apiId.Trim()}\n\n" +
                         "Now please send your **API Hash**:\n\n" +
                         "💡 Tip: Send /cancel to return to settings menu";

            var cancelButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Cancel",
                        "admin_settings")
                }
            };

            var cancelKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(cancelButtons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, cancelKeyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling MTProto API ID input: {ex.Message}");
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var errorMessage = $"❌ Error: {ex.Message}\n\nUse /cancel to return to settings menu.";
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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
        }
    }

    /// <summary>
    /// Handles API Hash input for MTProto setup
    /// </summary>
    private async Task HandleMtProtoSetupApiHashInputAsync(Guid userId, long chatId, string? stateData, string? apiHash, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancel
            if (apiHash?.Equals("/cancel", StringComparison.OrdinalIgnoreCase) == true ||
                apiHash?.Equals("cancel", StringComparison.OrdinalIgnoreCase) == true ||
                apiHash?.Equals("/back", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await HandleAdminSettingsAsync(chatId, cancellationToken);
                return;
            }

            // Validate API Hash (should not be empty)
            if (string.IsNullOrWhiteSpace(apiHash))
            {
                var errorMessage = "❌ Invalid API Hash. Please enter a valid API Hash.\n\n" +
                                 "You can get your API Hash from: https://my.telegram.org/apps\n\n" +
                                 "💡 Tip: Send /cancel to return to settings menu";

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
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
                return;
            }

            // Verify admin
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can configure MTProto.", cancellationToken);
                return;
            }

            // Store API ID and Hash in state and move to next step
            var apiId = stateData; // API ID from previous step
            var combinedData = JsonSerializer.Serialize(new { apiId, apiHash = apiHash.Trim() });
            
            await _userStateRepository.SetStateAsync(
                userId,
                Domain.Enums.UserStateType.MtProtoSetupPhoneNumber,
                combinedData,
                10, // 10 minutes timeout
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var message = $"✅ API Hash saved\n\n" +
                         "Now please send your **Phone Number** (with country code):\n\n" +
                         "Example: +1234567890\n\n" +
                         "💡 Tip: Send /cancel to return to settings menu";

            var cancelButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Cancel",
                        "admin_settings")
                }
            };

            var cancelKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(cancelButtons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, message, cancelKeyboard, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling MTProto API Hash input: {ex.Message}");
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var errorMessage = $"❌ Error: {ex.Message}\n\nUse /cancel to return to settings menu.";
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
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
        }
    }

    /// <summary>
    /// Handles Phone Number input for MTProto setup and completes the setup
    /// </summary>
    private async Task HandleMtProtoSetupPhoneNumberInputAsync(Guid userId, long chatId, string? stateData, string? phoneNumber, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"📱 HandleMtProtoSetupPhoneNumberInputAsync called. PhoneNumber: {phoneNumber ?? "(null)"}, StateData: {stateData ?? "(null)"}");
            
            // Check for cancel
            if (phoneNumber?.Equals("/cancel", StringComparison.OrdinalIgnoreCase) == true ||
                phoneNumber?.Equals("cancel", StringComparison.OrdinalIgnoreCase) == true ||
                phoneNumber?.Equals("/back", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await HandleAdminSettingsAsync(chatId, cancellationToken);
                return;
            }

            // Validate Phone Number (should start with + and contain digits)
            if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.Trim().StartsWith("+") || phoneNumber.Trim().Length < 8)
            {
                var errorMessage = "❌ Invalid Phone Number. Please enter a valid phone number with country code.\n\n" +
                                 "Example: +1234567890\n\n" +
                                 "💡 Tip: Send /cancel to return to settings menu";

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
                await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, keyboard, cancellationToken);
                return;
            }

            // Verify admin
            var isAdmin = await _authorizationService.IsAdminAsync(userId, cancellationToken);
            if (!isAdmin)
            {
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _telegramBotService.SendMessageAsync(chatId, "❌ Only admins can configure MTProto.", cancellationToken);
                return;
            }

            // Parse previous data
            if (string.IsNullOrWhiteSpace(stateData))
            {
                await _telegramBotService.SendMessageAsync(chatId, "❌ Session expired. Please start over.", cancellationToken);
                await _userStateRepository.ClearStateAsync(userId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            Console.WriteLine($"📋 Parsing state data...");
            var data = JsonSerializer.Deserialize<JsonElement>(stateData);
            var apiId = data.GetProperty("apiId").GetString();
            var apiHash = data.GetProperty("apiHash").GetString();
            var phone = phoneNumber.Trim();
            Console.WriteLine($"✅ Parsed: ApiId={apiId}, ApiHash={apiHash?.Substring(0, Math.Min(8, apiHash?.Length ?? 0))}..., Phone={phone}");

            // Save credentials to platform settings
            Console.WriteLine($"💾 Saving credentials to platform settings...");
            await _platformSettingsRepository.SetValueAsync(
                Domain.Entities.PlatformSettings.Keys.MtProtoApiId,
                apiId!,
                "Telegram API ID for MTProto User API",
                isSecret: false,
                cancellationToken);
            Console.WriteLine($"✅ API ID saved");
            
            await _platformSettingsRepository.SetValueAsync(
                Domain.Entities.PlatformSettings.Keys.MtProtoApiHash,
                apiHash!,
                "Telegram API Hash for MTProto User API",
                isSecret: true,
                cancellationToken);
            Console.WriteLine($"✅ API Hash saved");
            
            await _platformSettingsRepository.SetValueAsync(
                Domain.Entities.PlatformSettings.Keys.MtProtoPhoneNumber,
                phone,
                "Telegram Phone Number for MTProto User API",
                isSecret: false,
                cancellationToken);
            Console.WriteLine($"✅ Phone Number saved");

            Console.WriteLine($"💾 Saving changes to database...");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"✅ Database changes saved");

            // Reinitialize MTProto service with new credentials
            Console.WriteLine($"🔄 Reinitializing MTProto service...");
            var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);
            Console.WriteLine($"📁 Session path: {sessionPath}");
            
            try
            {
                await _mtProtoService.ReinitializeAsync(apiId!, apiHash!, phone, sessionPath, cancellationToken);
                Console.WriteLine($"✅ MTProto service reinitialized successfully");
            }
            catch (Exception reinitEx)
            {
                Console.WriteLine($"❌ ERROR reinitializing MTProto service: {reinitEx.Message}");
                Console.WriteLine($"❌ Stack trace: {reinitEx.StackTrace}");
                throw; // Re-throw to be caught by outer catch
            }

            // Clear state
            Console.WriteLine($"🧹 Clearing user state...");
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"✅ User state cleared");

            // Set up callbacks for authentication notifications
            Console.WriteLine($"🔔 Setting up authentication callbacks...");
            Infrastructure.Services.MtProtoAuthStore.SetCurrentChatId(chatId);
            Infrastructure.Services.MtProtoAuthStore.SetVerificationCodeCallback(async (targetChatId) =>
            {
                var codeMessage = "📱 **Verification Code Required**\n\n" +
                                "Telegram has sent a verification code to your **Telegram app** (not SMS).\n\n" +
                                "⚠️ **Important:** Check your Telegram app (mobile or desktop) for the verification code.\n\n" +
                                "The code will appear in:\n" +
                                "• Your Telegram app notifications\n" +
                                "• Or in the Telegram app itself\n\n" +
                                "Once you see the code, send it using:\n" +
                                "`/auth_code <your_code>`\n\n" +
                                "Example: `/auth_code 12345`\n\n" +
                                "💡 **Note:** The code is sent to your Telegram app, not via SMS.";
                await _telegramBotService.SendMessageAsync(targetChatId, codeMessage, cancellationToken);
            });
            
            Infrastructure.Services.MtProtoAuthStore.Set2FAPasswordCallback(async (targetChatId) =>
            {
                var passwordMessage = "🔐 **2FA Password Required**\n\n" +
                                     "Your account has 2FA enabled.\n\n" +
                                     "Please send your 2FA password using:\n" +
                                     "`/auth_password <your_password>`\n\n" +
                                     "Example: `/auth_password mypassword123`";
                await _telegramBotService.SendMessageAsync(targetChatId, passwordMessage, cancellationToken);
            });
            Console.WriteLine($"✅ Callbacks set up");

            // Start authentication in background
            Console.WriteLine($"📤 Sending success message to user...");
            var authMessage = $"✅ MTProto credentials saved successfully!\n\n" +
                            $"📱 API ID: {apiId}\n" +
                            $"📞 Phone: {phone}\n\n" +
                            "🔄 Starting authentication...\n\n" +
                            "⏳ If a verification code is required, you'll receive a message.\n" +
                            "Use `/auth_code <code>` to provide it.\n\n" +
                            "If 2FA is enabled, use `/auth_password <password>` when prompted.";

            await _telegramBotService.SendMessageAsync(chatId, authMessage, cancellationToken);
            Console.WriteLine($"✅ Success message sent");

            // Start authentication in background (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("🔄 Starting MTProto authentication in background...");
                    var authResult = await _mtProtoService.TestAuthenticationAsync(cancellationToken);
                    
                    if (authResult)
                    {
                        var successMsg = "✅ MTProto authentication successful!\n\n" +
                                       "The service is now ready to use.";
                        await _telegramBotService.SendMessageAsync(chatId, successMsg, cancellationToken);
                    }
                    else
                    {
                        var waitingMsg = "⏳ Authentication is still in progress...\n\n" +
                                       "If you haven't received a code request yet, please wait.\n\n" +
                                       "If you need to provide a verification code, use:\n" +
                                       "`/auth_code <your_code>`\n\n" +
                                       "If 2FA is required, use:\n" +
                                       "`/auth_password <your_password>`";
                        await _telegramBotService.SendMessageAsync(chatId, waitingMsg, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during background authentication: {ex.Message}");
                    var errorMsg = $"⚠️ Authentication attempt failed: {ex.Message}\n\n" +
                                 "You can retry authentication later or check the logs for details.\n\n" +
                                 "If you need to provide a verification code, use:\n" +
                                 "`/auth_code <your_code>`\n\n" +
                                 "If 2FA is required, use:\n" +
                                 "`/auth_password <your_password>`";
                    await _telegramBotService.SendMessageAsync(chatId, errorMsg, cancellationToken);
                }
            }, cancellationToken);

            // Note: We already sent a message above, so we don't need to send another one with buttons
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR in HandleMtProtoSetupPhoneNumberInputAsync: {ex.Message}");
            Console.WriteLine($"❌ Exception type: {ex.GetType().FullName}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"❌ Inner stack trace: {ex.InnerException.StackTrace}");
            }
            
            await _userStateRepository.ClearStateAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            var errorMessage = $"❌ Error: {ex.Message}\n\nUse /cancel to return to settings menu.";
            var errorButtons = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>
            {
                new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(
                        "❌ Cancel",
                        "admin_settings")
                }
            };
            var errorKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(errorButtons);
            await _telegramBotService.SendMessageWithButtonsAsync(chatId, errorMessage, errorKeyboard, cancellationToken);
        }
    }
}

