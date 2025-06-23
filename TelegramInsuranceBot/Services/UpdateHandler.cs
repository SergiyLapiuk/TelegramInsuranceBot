using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TelegramInsuranceBot.Services;

namespace TelegramInsuranceBot.Services;

public class UpdateHandler(
    ITelegramBotClient bot,
    ILogger<UpdateHandler> logger,
    MindeeService mindee,
    OpenAiChatService ai) : IUpdateHandler
{
    private static readonly Dictionary<long, string> passportData = new();
    private static readonly Dictionary<long, string> carDocData = new();
    private static readonly Dictionary<long, List<ChatMessage>> chatHistories = new();

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (update.Message is { } msg)
            await OnMessage(msg);
    }

    private async Task OnMessage(Message msg)
    {
        long chatId = msg.Chat.Id;

        if (!chatHistories.ContainsKey(chatId))
            chatHistories[chatId] = [];

        var history = chatHistories[chatId];

        if (msg.Text == "/start")
        {
            history.Clear();
            history.Add(new SystemChatMessage("""
                Ти — віртуальний агент, який допомагає користувачеві купити автострахування.
                Слідуй чітким етапам, кожен етап це окреме повідомлення твоє:
                1. Привітайся та поясни свою роль.
                2. Запроси фото паспорта.
                2. Запроси фото техпаспорта.
                3. Лише після отримання всіх двох фоото — покажи витягнуті дані (їх передає бекенд) та перейди до наступного кроку.
                4. Запитай дослівно "чи все правильно". Якщо користувач не погоджується з отриманими даними, попросіть його 
                повторно зробити та надіслати фотографії в тому ж порядку, а потім повторіть процес вилучення та підтвердження даних.
                5. Якщо користувач погодився з отриманими даними — повідом про фіксовану ціну 100 USD та запитай згоду. 
                Якщо користувач не згоден, бот повинен вибачитися та пояснити, що 100 доларів США — єдина доступна ціна і запропонувати ще раз. 
                Якщо користувач згоден, перейдіть до останнього кроку.
                6. Згенеруйте документ-заявку на страховий поліс за допомогою OpenAI (за умови використання шаблону або попередньо відформатованого тексту).
                Надішліть цей документ-заявку користувачеві як підтвердження покупки.
                Не пропускай етапів, не імпровізуй.
            """));

            var reply = ai.GetReply(history);
            history.Add(reply);
            await bot.SendMessage(msg.Chat, reply.Content[0].Text);
            return;
        }

        if (msg.Photo is not null)
        {
            if (!passportData.ContainsKey(chatId))
            {
                var passport = await AnalyzePhotoAsync(msg, analyzeCarDoc: false);
                passportData[chatId] = passport;

                var passportText = $"Паспорт:\n{passport}";
                history.Add(new UserChatMessage(passportText));

                var reply = ai.GetReply(history);
                history.Add(reply);
                await bot.SendMessage(msg.Chat, reply.Content[0].Text);
                return;
            }

            if (!carDocData.ContainsKey(chatId))
            {
                var carDoc = await AnalyzePhotoAsync(msg, analyzeCarDoc: true);
                carDocData[chatId] = carDoc;

                var carText = $"Техпаспорт:\n{carDoc}";
                history.Add(new UserChatMessage(carText));

                var reply = ai.GetReply(history);
                history.Add(reply);
                await bot.SendMessage(msg.Chat, reply.Content[0].Text);
                return;
            }
        }

        if (msg.Text is { } userText)
        {
            history.Add(new UserChatMessage(userText));

            if (userText.ToLower().Contains("ні") && history.Any(m => m is AssistantChatMessage a && a.Content[0].Text.Contains("чи все правильно")))
            {
                passportData.Remove(chatId);
                carDocData.Remove(chatId);

                history.Add(new UserChatMessage("Користувач відхилив дані документів — повторна подача."));
            }

            if (userText.ToLower().Contains("так") && history.Any(m => m is AssistantChatMessage a && a.Content[0].Text.Contains("100")))
            {
                string policy = ai.GeneratePolicyText(history);
                await SendGeneratedPolicyFileAsync(msg.Chat, policy);

                passportData.Remove(chatId);
                carDocData.Remove(chatId);
                chatHistories.Remove(chatId);
                await bot.SendMessage(msg.Chat, "Страховий поліс успішно сформовано та надіслано. Щоб оформити новий поліс, надішліть /start");
                
                return;
            }

            var reply = ai.GetReply(history);
            history.Add(reply);
            await bot.SendMessage(msg.Chat, reply.Content[0].Text);
            return;
        }

        await bot.SendMessage(msg.Chat, "Невідома команда. Напишіть /start, щоб почати.");
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error occurred while processing update.");
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private async Task<string> AnalyzePhotoAsync(Message msg, bool analyzeCarDoc)
    {
        var fileId = msg.Photo!.Last().FileId;
        var tgFile = await bot.GetFile(fileId);
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

        await using (var stream = File.Create(tempFilePath))
        {
            await bot.DownloadFile(tgFile, stream);
        }

        string result = analyzeCarDoc
            ? await mindee.AnalyzeCarCertificateAsync(tempFilePath)
            : await mindee.AnalyzeDocumentAsync(tempFilePath);

        File.Delete(tempFilePath);
        return result;
    }

    private async Task SendGeneratedPolicyFileAsync(Chat chat, string content)
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"policy_{chat.Id}.txt");
        await File.WriteAllTextAsync(filePath, content);

        await using var stream = File.OpenRead(filePath);
        await bot.SendDocument(chat.Id, InputFile.FromStream(stream, "insurance_policy.txt"));

        File.Delete(filePath);
    }

}
