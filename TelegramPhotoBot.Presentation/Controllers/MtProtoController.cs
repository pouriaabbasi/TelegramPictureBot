using Microsoft.AspNetCore.Mvc;
using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;

namespace TelegramPhotoBot.Presentation.Controllers;

[ApiController]
[Route("mtproto")]
public class MtProtoController : ControllerBase
{
    private readonly IMtProtoService _mtProtoService;
    private readonly IPlatformSettingsRepository _settingsRepo;
    
    public MtProtoController(IMtProtoService mtProtoService, IPlatformSettingsRepository settingsRepo)
    {
        _mtProtoService = mtProtoService;
        _settingsRepo = settingsRepo;
    }

    [HttpGet("status")]
    public async Task<ContentResult> Status()
    {
        // Check what's needed for MTProto
        var apiId = await _settingsRepo.GetValueAsync("telegram:mtproto:api_id", default);
        var apiHash = await _settingsRepo.GetValueAsync("telegram:mtproto:api_hash", default);
        var phoneNumber = await _settingsRepo.GetValueAsync("telegram:mtproto:phone_number", default);
        
        if (string.IsNullOrWhiteSpace(apiId))
        {
            return Content(@"
                <html>
                <head><title>MTProto Setup</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔐 MTProto Setup - Step 1: API ID</h2>
                    <p>Get your API credentials from <a href='https://my.telegram.org/apps' target='_blank'>my.telegram.org/apps</a></p>
                    <form action='config' method='get'>
                        <input name='key' type='hidden' value='api_id'/>
                        <input name='value' autofocus placeholder='API ID' required style='padding: 10px; width: 300px;'/>
                        <button type='submit' style='padding: 10px 20px;'>Next →</button>
                    </form>
                </body>
                </html>
            ", "text/html");
        }
        
        if (string.IsNullOrWhiteSpace(apiHash))
        {
            return Content(@"
                <html>
                <head><title>MTProto Setup</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔐 MTProto Setup - Step 2: API Hash</h2>
                    <p>Enter your API Hash from Telegram</p>
                    <form action='config' method='get'>
                        <input name='key' type='hidden' value='api_hash'/>
                        <input name='value' autofocus placeholder='API Hash' required style='padding: 10px; width: 300px;'/>
                        <button type='submit' style='padding: 10px 20px;'>Next →</button>
                    </form>
                </body>
                </html>
            ", "text/html");
        }
        
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return Content(@"
                <html>
                <head><title>MTProto Setup</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>🔐 MTProto Setup - Step 3: Phone Number</h2>
                    <p>Enter your phone number with country code</p>
                    <form action='config' method='get'>
                        <input name='key' type='hidden' value='phone_number'/>
                        <input name='value' autofocus placeholder='+1234567890' required style='padding: 10px; width: 300px;'/>
                        <button type='submit' style='padding: 10px 20px;'>Start Login →</button>
                    </form>
                </body>
                </html>
            ", "text/html");
        }

        // All credentials are present, check login status
        try
        {
            var configNeeded = _mtProtoService.ConfigNeeded;
            
            if (configNeeded == null)
            {
                return Content(@"
                    <html>
                    <head><title>MTProto - Connected</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>✅ MTProto Connected!</h2>
                        <p>Authentication successful. You can close this page.</p>
                        <p><a href='reset' style='color: #d9534f;'>Reset & Start Over</a></p>
                    </body>
                    </html>
                ", "text/html");
            }
            else if (configNeeded == "verification_code")
            {
                return Content(@"
                    <html>
                    <head><title>MTProto - Verification Code</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>📱 Enter Verification Code</h2>
                        <p>Check your Telegram app for the verification code:</p>
                        <form action='login' method='get'>
                            <input name='value' autofocus placeholder='12345' required style='padding: 10px; width: 300px;'/>
                            <button type='submit' style='padding: 10px 20px;'>Submit Code</button>
                        </form>
                    </body>
                    </html>
                ", "text/html");
            }
            else if (configNeeded == "password")
            {
                return Content(@"
                    <html>
                    <head><title>MTProto - 2FA Password</title></head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔒 Enter 2FA Password</h2>
                        <p>Your account has 2FA enabled. Enter your password:</p>
                        <form action='login' method='get'>
                            <input name='value' type='password' autofocus placeholder='Password' required style='padding: 10px; width: 300px;'/>
                            <button type='submit' style='padding: 10px 20px;'>Submit Password</button>
                        </form>
                    </body>
                    </html>
                ", "text/html");
            }
            else
            {
                // Service is initializing or needs something else
                return Content($@"
                    <html>
                    <head>
                        <title>MTProto - Connecting</title>
                        <meta http-equiv='refresh' content='2'>
                    </head>
                    <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                        <h2>🔄 Connecting to Telegram...</h2>
                        <p>Status: {configNeeded ?? "Initializing"}</p>
                        <p>This page will refresh automatically...</p>
                        <p><a href='status'>Manual Refresh</a> | <a href='reset' style='color: #d9534f;'>Start Over</a></p>
                    </body>
                    </html>
                ", "text/html");
            }
        }
        catch (Exception ex)
        {
            return Content($@"
                <html>
                <head><title>MTProto - Error</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>⚠️ Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='reset'>Start Over</a></p>
                </body>
                </html>
            ", "text/html");
        }
    }

    [HttpGet("config")]
    public async Task<ActionResult> Config(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Redirect("status");
        
        try
        {
            // Save to settings
            await _settingsRepo.SetValueAsync($"telegram:mtproto:{key}", value, isSecret: key == "api_hash", cancellationToken: default);
            
            // If we just saved phone_number, reinitialize and start login
            if (key == "phone_number")
            {
                var apiId = await _settingsRepo.GetValueAsync("telegram:mtproto:api_id", default);
                var apiHash = await _settingsRepo.GetValueAsync("telegram:mtproto:api_hash", default);
                
                // Reinitialize MTProto service
                var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
                
                Console.WriteLine($"🔧 Initializing MTProto with ApiId={apiId}, Phone={value}");
                await _mtProtoService.ReinitializeAsync(apiId!, apiHash!, value, sessionPath, default);
                
                Console.WriteLine($"🔐 Starting login process...");
                // Start login with phone number (like the working example)
                await _mtProtoService.LoginAsync(value, default);
            }
            
            return Redirect("status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Config error: {ex.Message}");
            return Content($@"
                <html>
                <head><title>MTProto - Error</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Configuration Error</h2>
                    <p style='color: red;'>{ex.Message}</p>
                    <p><a href='status'>Try Again</a> | <a href='reset'>Start Over</a></p>
                </body>
                </html>
            ", "text/html");
        }
    }

    [HttpGet("login")]
    public async Task<ActionResult> Login(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Redirect("status");
        
        try
        {
            Console.WriteLine($"🔐 Attempting login with provided value...");
            
            // Call Login with the provided value (code or password)
            var result = await _mtProtoService.LoginAsync(value, default);
            
            Console.WriteLine($"✅ Login call completed. Result: {result ?? "SUCCESS"}");
            
            // Redirect back to status to check if more input is needed
            return Redirect("status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login error: {ex.Message}");
            return Content($@"
                <html>
                <head><title>MTProto - Login Error</title></head>
                <body style='font-family: Arial; max-width: 600px; margin: 50px auto; padding: 20px;'>
                    <h2>❌ Login Error</h2>
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
        // Clear all MTProto settings
        await _settingsRepo.ClearMtProtoSettingsAsync(default);
        
        // Delete session file
        var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
        if (System.IO.File.Exists(sessionPath))
        {
            System.IO.File.Delete(sessionPath);
        }
        
        return Redirect("status");
    }
}

