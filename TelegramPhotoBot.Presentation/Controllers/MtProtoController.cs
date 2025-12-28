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
                <head><title>Invalid Token</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Invalid Token</h2>
                    <p>The provided token is invalid.</p>
                </body>
                </html>
            ", "text/html");
        }
        
        // Validate and consume the token
        var isValid = await _tokenService.ValidateAndConsumeTokenAsync(tokenGuid);
        if (!isValid)
        {
            return Content(@"
                <html>
                <head><title>Token Expired</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>⏱️ Token Expired</h2>
                    <p>This token has expired or has already been used.</p>
                    <p>Please request a new link from the bot.</p>
                </body>
                </html>
            ", "text/html");
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
    public ContentResult Status()
    {
        // Check session authentication
        if (!IsAuthenticated())
        {
            return Content(@"
                <html>
                <head><title>Unauthorized</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔒 Unauthorized</h2>
                    <p>Please request a secure access link from the bot admin panel.</p>
                    <p>Go to: Admin Panel → Settings → 🌐 Web Setup</p>
                </body>
                </html>
            ", "text/html");
        }
        switch (_mtProto.ConfigNeeded)
        {
            case "connecting":
                return Content(@"
                    <html>
                    <head>
                        <title>MTProto - Connecting</title>
                        <meta http-equiv='refresh' content='1'>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔄 WTelegram is connecting...</h2>
                        <p>Please wait...</p>
                    </body>
                    </html>
                ", "text/html");
            
            case null:
            case "authenticated":
                return Content($@"
                    <html>
                    <head><title>MTProto - Connected</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>✅ Connected as {_mtProto.User?.username ?? _mtProto.User?.first_name ?? "User"}</h2>
                        <p>MTProto authentication successful!</p>
                        <p><a href='reset' style='color: #d9534f;'>Reset & Start Over</a></p>
                    </body>
                    </html>
                ", "text/html");
            
            case "error":
                return Content(@"
                    <html>
                    <head><title>MTProto - Error</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>❌ Connection Error</h2>
                        <p>An error occurred. Please <a href='reset'>start over</a>.</p>
                    </body>
                    </html>
                ", "text/html");
            
            default:
                var label = _mtProto.ConfigNeeded switch
                {
                    "api_id" => "API ID (from my.telegram.org/apps)",
                    "api_hash" => "API Hash",
                    "phone_number" => "Phone Number (e.g., +1234567890)",
                    "verification_code" => "Verification Code (check your Telegram app)",
                    "password" => "2FA Password",
                    _ => _mtProto.ConfigNeeded
                };
                
                var inputType = _mtProto.ConfigNeeded == "password" ? "password" : "text";
                
                return Content($@"
                    <html>
                    <head><title>MTProto Setup</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔐 MTProto Setup</h2>
                        <p>Enter {label}:</p>
                        <form action='config' method='get'>
                            <input name='value' type='{inputType}' autofocus required style='padding: 10px; width: 300px;'/>
                            <button type='submit' style='padding: 10px 20px;'>Submit</button>
                        </form>
                        <p><a href='reset' style='color: #d9534f;'>Start Over</a></p>
                    </body>
                    </html>
                ", "text/html");
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

    [HttpGet("reset")]
    public async Task<ActionResult> Reset()
    {
        try
        {
            // This will require IPlatformSettingsRepository - let me add it
            var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
            
            // Clear all MTProto settings
            await settingsRepo.ClearMtProtoSettingsAsync(default);
            
            // Delete session file
            var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            if (System.IO.File.Exists(sessionPath))
            {
                System.IO.File.Delete(sessionPath);
            }
            
            return Content(@"
                <html>
                <head><title>MTProto - Reset</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔄 Settings Reset</h2>
                    <p>All MTProto settings have been cleared.</p>
                    <p><strong>Please restart the application</strong> for changes to take effect.</p>
                    <p><a href='status'>Back to Status</a></p>
                </body>
                </html>
            ", "text/html");
        }
        catch (Exception ex)
        {
            return Content($@"
                <html>
                <head><title>MTProto - Error</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Reset Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='status'>Back to Status</a></p>
                </body>
                </html>
            ", "text/html");
        }
    }

    private async Task SaveConfigValue(string key, string value)
    {
        var settingsRepo = HttpContext.RequestServices.GetRequiredService<Application.Interfaces.Repositories.IPlatformSettingsRepository>();
        await settingsRepo.SetValueAsync($"telegram:mtproto:{key}", value, isSecret: key == "api_hash" || key == "password", cancellationToken: default);
    }
}
