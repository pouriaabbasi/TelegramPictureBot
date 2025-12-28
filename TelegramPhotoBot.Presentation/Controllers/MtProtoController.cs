using Microsoft.AspNetCore.Mvc;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Infrastructure.Services;

namespace TelegramPhotoBot.Presentation.Controllers;

[ApiController]
[Route("mtproto")]
public class MtProtoController : ControllerBase
{
    private readonly MtProtoBackgroundService _mtProto;
    private readonly IMtProtoAccessTokenService _tokenService;
    private const string SessionKey = "MtProtoAuth";
    
    public MtProtoController(MtProtoBackgroundService mtProto, IMtProtoAccessTokenService tokenService)
    {
        _mtProto = mtProto;
        _tokenService = tokenService;
    }
    
    /// <summary>
    /// Auth endpoint - validates one-time token and creates session
    /// </summary>
    [HttpGet("auth")]
    public async Task<ActionResult> Auth(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var tokenGuid))
        {
            return Content(@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Invalid Token</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Invalid Token</h2>
                    <p>The provided token is invalid.</p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        
        // Validate and consume the token
        var isValid = await _tokenService.ValidateAndConsumeTokenAsync(tokenGuid);
        if (!isValid)
        {
            return Content(@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Token Expired</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>⏱️ Token Expired</h2>
                    <p>This token has expired or has already been used.</p>
                    <p>Please request a new link from the bot.</p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        
        // Set session
        HttpContext.Session.SetString(SessionKey, "authenticated");
        Console.WriteLine($"✅ Session created for token {tokenGuid}");
        
        // Redirect to status page
        return Redirect("/mtproto/status");
    }
    
    private bool IsAuthenticated()
    {
        var session = HttpContext.Session.GetString(SessionKey);
        return session == "authenticated";
    }

    [HttpGet("status")]
    public async Task<ContentResult> Status()
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Content(@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Unauthorized</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔒 Unauthorized</h2>
                    <p>Please request a secure access link from the bot admin panel.</p>
                    <p>Go to: Admin Panel → Settings → 🌐 Web Setup</p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        
        // Load existing settings
        var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
        var apiId = await settingsRepo.GetValueAsync("telegram:mtproto:api_id", default) ?? "";
        var apiHash = await settingsRepo.GetValueAsync("telegram:mtproto:api_hash", default) ?? "";
        var phoneNumber = await settingsRepo.GetValueAsync("telegram:mtproto:phone_number", default) ?? "";
        var duration = await settingsRepo.GetValueAsync("telegram:mtproto:default_duration", default) ?? "30";
        var name = await settingsRepo.GetValueAsync("telegram:mtproto:account_name", default) ?? "";
        
        // Check authentication status
        var isAuthenticated = _mtProto.User != null;
        var statusMessage = isAuthenticated 
            ? $"✅ Authenticated as <strong>{_mtProto.User?.username ?? _mtProto.User?.first_name ?? "User"}</strong>" 
            : "⚠️ Not authenticated yet";
        var needsVerification = _mtProto.ConfigNeeded == "verification_code" || _mtProto.ConfigNeeded == "password";
        
        return Content($@"
            <html>
            <head>
                <meta charset='utf-8'>
                <title>MTProto Configuration</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        max-width: 700px;
                        margin: 50px auto;
                        padding: 20px;
                        background: #f5f5f5;
                    }}
                    .container {{
                        background: white;
                        padding: 30px;
                        border-radius: 10px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    }}
                    h2 {{
                        color: #333;
                        margin-top: 0;
                    }}
                    .status {{
                        padding: 15px;
                        border-radius: 5px;
                        margin-bottom: 20px;
                        background: {(isAuthenticated ? "#d4edda" : "#fff3cd")};
                        border: 1px solid {(isAuthenticated ? "#c3e6cb" : "#ffc107")};
                        color: {(isAuthenticated ? "#155724" : "#856404")};
                    }}
                    .form-group {{
                        margin-bottom: 20px;
                    }}
                    label {{
                        display: block;
                        margin-bottom: 5px;
                        font-weight: bold;
                        color: #555;
                    }}
                    input[type='text'], input[type='number'], input[type='password'] {{
                        width: 100%;
                        padding: 10px;
                        border: 1px solid #ddd;
                        border-radius: 5px;
                        font-size: 14px;
                        box-sizing: border-box;
                    }}
                    .help-text {{
                        font-size: 12px;
                        color: #777;
                        margin-top: 5px;
                    }}
                    .button-group {{
                        display: flex;
                        gap: 10px;
                        margin-top: 30px;
                    }}
                    button {{
                        padding: 12px 24px;
                        border: none;
                        border-radius: 5px;
                        font-size: 16px;
                        cursor: pointer;
                        transition: background 0.3s;
                    }}
                    .btn-primary {{
                        background: #28a745;
                        color: white;
                        flex: 1;
                    }}
                    .btn-primary:hover {{
                        background: #218838;
                    }}
                    .btn-danger {{
                        background: #dc3545;
                        color: white;
                    }}
                    .btn-danger:hover {{
                        background: #c82333;
                    }}
                    .verification-section {{
                        background: #e7f3ff;
                        padding: 15px;
                        border-radius: 5px;
                        margin-top: 20px;
                        border: 1px solid #bee5eb;
                        {(needsVerification ? "" : "display: none;")}
                    }}
                    .external-link {{
                        color: #007bff;
                        text-decoration: none;
                    }}
                    .external-link:hover {{
                        text-decoration: underline;
                    }}
                    input[readonly] {{
                        background: #e9ecef;
                        cursor: not-allowed;
                    }}
                </style>
                <script>
                    function confirmReset() {{
                        if (confirm('⚠️ Warning!\n\nThis will:\n• Delete all MTProto credentials\n• Remove session file\n• Require full re-authentication\n\nAre you sure?')) {{
                            window.location.href = '/mtproto/reset';
                        }}
                    }}
                </script>
            </head>
            <body>
                <div class='container'>
                    <h2>🔐 MTProto Configuration</h2>
                    
                    <div class='status'>
                        {statusMessage}
                    </div>
                    
                    <form method='POST' action='/mtproto/save-config'>
                        <div class='form-group'>
                            <label for='api_id'>API ID *</label>
                            <input type='text' id='api_id' name='api_id' value='{apiId}' required {(isAuthenticated ? "readonly" : "")}>
                            <div class='help-text'>Get from <a href='https://my.telegram.org/apps' target='_blank' class='external-link'>my.telegram.org/apps</a></div>
                        </div>
                        
                        <div class='form-group'>
                            <label for='api_hash'>API Hash *</label>
                            <input type='text' id='api_hash' name='api_hash' value='{(string.IsNullOrEmpty(apiHash) ? "" : "************************")}' {(isAuthenticated ? "readonly" : "")} {(isAuthenticated ? "" : "required")}>
                            <div class='help-text'>32-character hexadecimal string from my.telegram.org</div>
                        </div>
                        
                        <div class='form-group'>
                            <label for='phone_number'>Phone Number *</label>
                            <input type='text' id='phone_number' name='phone_number' value='{phoneNumber}' placeholder='+989123456789' required {(isAuthenticated ? "readonly" : "")}>
                            <div class='help-text'>Include country code (e.g., +989123456789)</div>
                        </div>
                        
                        <div class='form-group'>
                            <label for='default_duration'>Default Self-Destruct Duration (seconds)</label>
                            <input type='number' id='default_duration' name='default_duration' value='{duration}' min='1' max='60'>
                            <div class='help-text'>Default timer for self-destructing photos/videos (1-60 seconds)</div>
                        </div>
                        
                        <div class='form-group'>
                            <label for='account_name'>Account Name (Optional)</label>
                            <input type='text' id='account_name' name='account_name' value='{name}' placeholder='My Bot Account'>
                            <div class='help-text'>Friendly name for this MTProto account</div>
                        </div>
                        
                        {(needsVerification ? $@"
                        <div class='verification-section'>
                            <h3>📱 Verification Required</h3>
                            <div class='form-group'>
                                <label for='verification_code'>{(_mtProto.ConfigNeeded == "password" ? "2FA Password" : "Verification Code")}</label>
                                <input type='{(_mtProto.ConfigNeeded == "password" ? "password" : "text")}' id='verification_code' name='verification_code' placeholder='Enter {(_mtProto.ConfigNeeded == "password" ? "password" : "code")}' required>
                                <div class='help-text'>{(_mtProto.ConfigNeeded == "password" ? "Enter your Two-Factor Authentication password" : "Check your Telegram app for the verification code")}</div>
                            </div>
                        </div>
                        " : "")}
                        
                        <div class='button-group'>
                            <button type='submit' class='btn-primary'>{(isAuthenticated ? "💾 Update Settings" : (needsVerification ? "✅ Verify & Authenticate" : "🚀 Save & Authenticate"))}</button>
                            <button type='button' class='btn-danger' onclick='confirmReset()'>🔄 Reset</button>
                        </div>
                    </form>
                </div>
            </body>
            </html>
        ", "text/html; charset=utf-8");
    }

    [HttpPost("save-config")]
    public async Task<ActionResult> SaveConfig([FromForm] string api_id, [FromForm] string? api_hash, [FromForm] string phone_number, 
        [FromForm] string? default_duration, [FromForm] string? account_name, [FromForm] string? verification_code)
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Redirect("/mtproto/status");
        }
        
        try
        {
            var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
            
            // If verification code provided, submit it
            if (!string.IsNullOrWhiteSpace(verification_code))
            {
                await _mtProto.DoLogin(verification_code);
                return Redirect("/mtproto/status");
            }
            
            // Save all settings FIRST and commit to database immediately
            Console.WriteLine($"💾 Saving API ID: {api_id}");
            await settingsRepo.SetValueAsync("telegram:mtproto:api_id", api_id, default);
            
            if (!string.IsNullOrWhiteSpace(api_hash) && api_hash != "************************")
            {
                Console.WriteLine($"💾 Saving API Hash: ***");
                await settingsRepo.SetValueAsync("telegram:mtproto:api_hash", api_hash, default);
            }
            
            Console.WriteLine($"💾 Saving Phone Number: {phone_number}");
            await settingsRepo.SetValueAsync("telegram:mtproto:phone_number", phone_number, default);
            
            if (!string.IsNullOrWhiteSpace(default_duration))
            {
                await settingsRepo.SetValueAsync("telegram:mtproto:default_duration", default_duration, default);
            }
            
            if (!string.IsNullOrWhiteSpace(account_name))
            {
                await settingsRepo.SetValueAsync("telegram:mtproto:account_name", account_name, default);
            }
            
            // CRITICAL: Save changes to database immediately!
            var dbContext = HttpContext.RequestServices.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
            await dbContext.SaveChangesAsync();
            Console.WriteLine("✅ All settings saved and committed to database");
            
            // Small delay to ensure DB write is fully committed
            await Task.Delay(100);
            
            // Start authentication process AFTER settings are saved
            if (_mtProto.User == null)
            {
                Console.WriteLine($"🔐 Starting authentication with phone: {phone_number}");
                await _mtProto.DoLogin(phone_number);
            }
            
            return Redirect("/mtproto/status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Save config error: {ex.Message}");
            return Content($@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>MTProto - Error</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='/mtproto/status'>Try Again</a> | <a href='/mtproto/reset'>Start Over</a></p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
    }

    [HttpGet("reset")]
    public async Task<ActionResult> Reset()
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Redirect("/mtproto/status");
        }
        
        try
        {
            var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
            
            // Clear all MTProto settings
            await settingsRepo.SetValueAsync("telegram:mtproto:api_id", null, default);
            await settingsRepo.SetValueAsync("telegram:mtproto:api_hash", null, default);
            await settingsRepo.SetValueAsync("telegram:mtproto:phone_number", null, default);
            
            // Delete session file
            var sessionFile = "WTelegram.session";
            if (System.IO.File.Exists(sessionFile))
            {
                System.IO.File.Delete(sessionFile);
                Console.WriteLine("✅ Session file deleted");
            }
            
            _mtProto.ConfigNeeded = "api_id";
            
            return Content(@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>MTProto - Reset Complete</title>
                    <meta http-equiv='refresh' content='2;url=/mtproto/status'>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>✅ Reset Complete</h2>
                    <p>All credentials have been cleared.</p>
                    <p>Redirecting to setup...</p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Reset error: {ex.Message}");
            return Content($@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>MTProto - Reset Error</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Reset Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='/mtproto/status'>Go Back</a></p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
    }
}

