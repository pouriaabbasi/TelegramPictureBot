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
        
        // Check if already authenticated or needs setup
        if (_mtProto.User != null)
        {
            // Already authenticated - show success page
            return Content($@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Already Connected</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>✅ Already Connected</h2>
                    <p>Your MTProto is already configured and authenticated as <strong>{_mtProto.User.username ?? _mtProto.User.first_name}</strong>.</p>
                    <p><a href='/mtproto/status' style='padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 5px;'>View Status</a></p>
                    <p style='margin-top: 30px;'><a href='/mtproto/reset' style='color: #d9534f;'>Reset & Start Over</a></p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        
        // Show welcome/setup page
        return Content(@"
            <html>
            <head>
                <meta charset='utf-8'>
                <title>MTProto Setup - Welcome</title>
            </head>
            <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                <h2>🔐 MTProto Setup</h2>
                <p>Welcome to the MTProto configuration wizard!</p>
                <p>You'll need the following information:</p>
                <ul>
                    <li><strong>API ID</strong> and <strong>API Hash</strong> from <a href='https://my.telegram.org/apps' target='_blank'>my.telegram.org/apps</a></li>
                    <li><strong>Phone Number</strong> (with country code, e.g., +989123456789)</li>
                    <li>Access to your Telegram app for <strong>verification code</strong></li>
                    <li><strong>2FA Password</strong> (if enabled on your account)</li>
                </ul>
                <p style='margin-top: 30px;'>
                    <a href='/mtproto/status' style='padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 5px; font-size: 16px;'>🚀 Start Setup</a>
                </p>
            </body>
            </html>
        ", "text/html; charset=utf-8");
    }
    
    private bool IsAuthenticated()
    {
        var session = HttpContext.Session.GetString(SessionKey);
        return session == "authenticated";
    }

    [HttpGet("status")]
    public ContentResult Status()
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
        switch (_mtProto.ConfigNeeded)
        {
            case "ready":
                // Not yet configured - show setup instructions
                return Content(@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>MTProto Setup</title>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔐 MTProto Setup Required</h2>
                        <p>MTProto is not configured yet. Please enter your credentials:</p>
                        <ul>
                            <li>Get <strong>API ID</strong> and <strong>API Hash</strong> from <a href='https://my.telegram.org/apps' target='_blank'>my.telegram.org/apps</a></li>
                            <li>You'll need your <strong>Phone Number</strong> (with country code)</li>
                            <li>Keep your Telegram app ready for <strong>verification code</strong></li>
                        </ul>
                        <p style='margin-top: 30px;'>
                            <a href='/mtproto/setup/start' style='padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 5px; font-size: 16px;'>🚀 Start Configuration</a>
                        </p>
                    </body>
                    </html>
                ", "text/html; charset=utf-8");
            
            case "connecting":
                return Content(@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>MTProto - Connecting</title>
                        <meta http-equiv='refresh' content='1'>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔄 WTelegram is connecting...</h2>
                        <p>Please wait...</p>
                    </body>
                    </html>
                ", "text/html; charset=utf-8");
            
            case null:
            case "authenticated":
                return Content($@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>MTProto - Connected</title>
                        <script>
                            function confirmReset() {{
                                if (confirm('⚠️ Warning!\n\nThis will:\n• Delete all MTProto credentials\n• Remove session file\n• Require full re-authentication\n\nAre you sure you want to continue?')) {{
                                    window.location.href = 'reset';
                                }}
                            }}
                        </script>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>✅ Connected as {_mtProto.User?.username ?? _mtProto.User?.first_name ?? "User"}</h2>
                        <p>MTProto authentication successful!</p>
                        <p style='margin-top: 30px;'>
                            <button onclick='confirmReset()' style='padding: 10px 20px; background: #d9534f; color: white; border: none; border-radius: 5px; cursor: pointer;'>🔄 Reset & Start Over</button>
                        </p>
                        <p style='font-size: 12px; color: #666; margin-top: 10px;'>
                            <strong>Start Over:</strong> Clears all credentials and session data. Use this if you want to switch to a different Telegram account.
                        </p>
                    </body>
                    </html>
                ", "text/html; charset=utf-8");
            
            case "error":
                return Content(@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>MTProto - Error</title>
                        <script>
                            function confirmReset() {
                                if (confirm('⚠️ This will delete all credentials and session data.\n\nContinue?')) {
                                    window.location.href = 'reset';
                                }
                            }
                        </script>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>❌ Connection Error</h2>
                        <p>An error occurred during authentication.</p>
                        <p style='margin-top: 20px;'>
                            <button onclick='confirmReset()' style='padding: 10px 20px; background: #d9534f; color: white; border: none; border-radius: 5px; cursor: pointer;'>🔄 Reset & Try Again</button>
                        </p>
                    </body>
                    </html>
                ", "text/html; charset=utf-8");
            
            default:
                var label = _mtProto.ConfigNeeded switch
                {
                    "api_id" => "API ID (from my.telegram.org/apps)",
                    "api_hash" => "API Hash",
                    "phone_number" => "Phone Number (e.g., +989123456789)",
                    "verification_code" => "Verification Code (check your Telegram app)",
                    "password" => "2FA Password",
                    _ => _mtProto.ConfigNeeded
                };
                
                var inputType = _mtProto.ConfigNeeded == "password" ? "password" : "text";
                var placeholder = _mtProto.ConfigNeeded switch
                {
                    "api_id" => "12345678",
                    "api_hash" => "0123456789abcdef0123456789abcdef",
                    "phone_number" => "+989123456789",
                    "verification_code" => "12345",
                    "password" => "Your 2FA password",
                    _ => ""
                };
                
                return Content($@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>MTProto Setup</title>
                        <script>
                            function confirmReset() {{
                                if (confirm('⚠️ This will delete all credentials and restart setup.\n\nContinue?')) {{
                                    window.location.href = 'reset';
                                }}
                            }}
                        </script>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔐 MTProto Setup</h2>
                        <p><strong>Step: {label}</strong></p>
                        <form action='config' method='get' style='margin-top: 20px;'>
                            <input name='value' type='{inputType}' placeholder='{placeholder}' autofocus required style='padding: 10px; width: 100%; max-width: 400px; font-size: 14px; border: 2px solid #ddd; border-radius: 5px;'/>
                            <br><br>
                            <button type='submit' style='padding: 10px 30px; background: #28a745; color: white; border: none; border-radius: 5px; cursor: pointer; font-size: 14px;'>Submit</button>
                        </form>
                        <p style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                            <button onclick='confirmReset()' style='padding: 8px 16px; background: transparent; color: #d9534f; border: 1px solid #d9534f; border-radius: 5px; cursor: pointer;'>🔄 Start Over</button>
                            <br>
                            <span style='font-size: 12px; color: #666;'>Clears all data and restarts setup</span>
                        </p>
                    </body>
                    </html>
                ", "text/html; charset=utf-8");
        }
    }

    [HttpGet("config")]
    public async Task<ActionResult> Config(string value)
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Redirect("/mtproto/status");
        }
        
        if (string.IsNullOrWhiteSpace(value))
            return Redirect("status");
        
        try
        {
            // Save the value to database for the Config callback to retrieve
            var currentNeed = _mtProto.ConfigNeeded;
            await SaveConfigValue(currentNeed, value);
            
            // Call DoLogin like the working example
            await _mtProto.DoLogin(value);
            
            return Redirect("status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Config error: {ex.Message}");
            return Content($@"
                <html>
                <head><title>MTProto - Error</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='status'>Try Again</a> | <a href='reset'>Start Over</a></p>
                </body>
                </html>
            ", "text/html");
        }
    }

    [HttpGet("setup/start")]
    public async Task<ActionResult> SetupStart()
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Redirect("/mtproto/status");
        }
        
        // Set ConfigNeeded to api_id to start the wizard
        var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
        
        // Check if already has settings
        var existingApiId = await settingsRepo.GetValueAsync("telegram:mtproto:api_id", default);
        if (!string.IsNullOrEmpty(existingApiId))
        {
            // Already configured, redirect to status
            return Redirect("/mtproto/status");
        }
        
        // Start fresh setup - ConfigNeeded should be set to api_id
        _mtProto.ConfigNeeded = "api_id";
        
        return Redirect("/mtproto/status");
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
            await settingsRepo.ClearMtProtoSettingsAsync(default);
            
            // Delete session file
            var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WTelegram.session");
            if (System.IO.File.Exists(sessionPath))
            {
                System.IO.File.Delete(sessionPath);
                Console.WriteLine("🗑️ Deleted session file");
            }
            
            // Reset ConfigNeeded
            _mtProto.ConfigNeeded = "api_id";
            
            return Content(@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>MTProto - Reset Complete</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>✅ Reset Complete</h2>
                    <p>All MTProto settings and session data have been cleared.</p>
                    <p>You can now configure MTProto from scratch.</p>
                    <p style='margin-top: 30px;'>
                        <a href='/mtproto/status' style='padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Start Fresh Setup</a>
                    </p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error resetting MTProto: {ex.Message}");
            return Content($@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>MTProto - Reset Error</title>
                </head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Reset Error</h2>
                    <p>An error occurred: {ex.Message}</p>
                    <p><a href='/mtproto/status'>Back to Status</a></p>
                </body>
                </html>
            ", "text/html; charset=utf-8");
        }
    }

    private async Task SaveConfigValue(string key, string value)
    {
        var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
        await settingsRepo.SetValueAsync($"telegram:mtproto:{key}", value, isSecret: key == "api_hash" || key == "password", cancellationToken: default);
    }
}
