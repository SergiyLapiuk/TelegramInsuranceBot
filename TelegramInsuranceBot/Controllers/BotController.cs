using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramInsuranceBot.Configuration;
using TelegramInsuranceBot.Services;

namespace TelegramInsuranceBot.Controllers;

[ApiController]
[Route("bot")]
public class BotController(BotConfiguration config) : ControllerBase
{
    [HttpGet("setWebhook")]
    public async Task<string> SetWebhook([FromServices] ITelegramBotClient bot, CancellationToken ct)
    {
        var webhookUrl = config.BotWebhookUrl.AbsoluteUri;
        await bot.SetWebhook(webhookUrl, allowedUpdates: [], secretToken: config.SecretToken, cancellationToken: ct);
        return $"Webhook set to {webhookUrl}";
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromServices] ITelegramBotClient bot,
        [FromServices] UpdateHandler handler,
        CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != config.SecretToken)
            return Forbid();

        try
        {
            await handler.HandleUpdateAsync(bot, update, ct);
        }
        catch (Exception ex)
        {
            await handler.HandleErrorAsync(bot, ex, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
        }

        return Ok();
    }
}
