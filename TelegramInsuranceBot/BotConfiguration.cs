namespace TelegramInsuranceBot.Configuration;

public class BotConfiguration
{
    public required string BotToken { get; init; }
    public required Uri BotWebhookUrl { get; init; }
    public required string SecretToken { get; init; }

    public static BotConfiguration LoadFromEnv() => new()
    {
        BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN")!,
        BotWebhookUrl = new(Environment.GetEnvironmentVariable("BOT_WEBHOOK_URL")!),
        SecretToken = Environment.GetEnvironmentVariable("SECRET_TOKEN")!
    };
}
