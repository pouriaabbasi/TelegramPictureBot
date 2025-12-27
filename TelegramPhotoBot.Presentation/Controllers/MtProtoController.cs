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
        
        if (string.IsNullOrEmpty(apiId))
        {
            return Content(@"
                <h2>MTProto Setup - Step 1: API ID</h2>
                <p>Enter your API ID from <a href='https://my.telegram.org/apps' target='_blank'>my.telegram.org/apps</a></p>
                <form action='config'>
                    <input name='key' type='hidden' value='api_id'/>
                    <input name='value' autofocus placeholder='API ID'/>
                    <button type='submit'>Next</button>
                </form>
            ", "text/html");
        }
        
        if (string.IsNullOrEmpty(apiHash))
        {
            return Content(@"
                <h2>MTProto Setup - Step 2: API Hash</h2>
                <p>Enter your API Hash</p>
                <form action='config'>
                    <input name='key' type='hidden' value='api_hash'/>
                    <input name='value' autofocus placeholder='API Hash'/>
                    <button type='submit'>Next</button>
                </form>
            ", "text/html");
        }
        
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return Content(@"
                <h2>MTProto Setup - Step 3: Phone Number</h2>
                <p>Enter your phone number with country code (e.g., +1234567890)</p>
                <form action='config'>
                    <input name='key' type='hidden' value='phone_number'/>
                    <input name='value' autofocus placeholder='+1234567890'/>
                    <button type='submit'>Next</button>
                </form>
            ", "text/html");
        }

        // Try to get the login status
        try
        {
            var configNeeded = _mtProtoService.ConfigNeeded;
            
            if (configNeeded == null)
            {
                return Content(@"
                    <h2>✅ MTProto Connected!</h2>
                    <p>Authentication successful. You can close this page.</p>
                    <a href='/'>Back to bot</a>
                ", "text/html");
            }
            else if (configNeeded == "verification_code")
            {
                return Content(@"
                    <h2>MTProto Setup - Verification Code</h2>
                    <p>Enter the verification code sent to your Telegram app:</p>
                    <form action='login'>
                        <input name='value' autofocus placeholder='12345'/>
                        <button type='submit'>Submit Code</button>
                    </form>
                ", "text/html");
            }
            else if (configNeeded == "password")
            {
                return Content(@"
                    <h2>MTProto Setup - 2FA Password</h2>
                    <p>Enter your 2FA password:</p>
                    <form action='login'>
                        <input name='value' type='password' autofocus placeholder='Password'/>
                        <button type='submit'>Submit Password</button>
                    </form>
                ", "text/html");
            }
            else
            {
                return Content($@"
                    <h2>MTProto Setup</h2>
                    <p>Connecting... Refresh in a moment.</p>
                    <p>Status: {configNeeded}</p>
                    <meta http-equiv='refresh' content='2'>
                ", "text/html");
            }
        }
        catch (Exception ex)
        {
            return Content($@"
                <h2>⚠️ Error</h2>
                <p>{ex.Message}</p>
                <a href='reset'>Start Over</a>
            ", "text/html");
        }
    }

    [HttpGet("config")]
    public async Task<ActionResult> Config(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Redirect("status");
        
        // Save to settings
        await _settingsRepo.SetValueAsync($"telegram:mtproto:{key}", value, isSecret: key == "api_hash", cancellationToken: default);
        await _settingsRepo.SetValueAsync($"telegram:mtproto:{key}", value, isSecret: false, cancellationToken: default);
        
        // If we just saved phone_number, reinitialize and start login
        if (key == "phone_number")
        {
            var apiId = await _settingsRepo.GetValueAsync("telegram:mtproto:api_id", default);
            var apiHash = await _settingsRepo.GetValueAsync("telegram:mtproto:api_hash", default);
            
            // Reinitialize MTProto service
            var sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mtproto_session.dat");
            await _mtProtoService.ReinitializeAsync(apiId!, apiHash!, value, sessionPath, default);
            
            // Start login with phone number (like the working example)
            try
            {
                await _mtProtoService.LoginAsync(value, default);
            }
            catch
            {
                // It's ok if this fails, user will continue on the web page
            }
        }
        
        return Redirect("status");
    }

    [HttpGet("login")]
    public async Task<ActionResult> Login(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Redirect("status");
        
        try
        {
            // Call Login with the provided value (code or password)
            await _mtProtoService.LoginAsync(value, default);
        }
        catch (Exception ex)
        {
            return Content($@"
                <h2>❌ Login Error</h2>
                <p>{ex.Message}</p>
                <a href='status'>Try Again</a>
            ", "text/html");
        }
        
        return Redirect("status");
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

