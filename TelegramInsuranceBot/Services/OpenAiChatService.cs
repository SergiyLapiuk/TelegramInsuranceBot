using OpenAI.Chat;

namespace TelegramInsuranceBot.Services;

public class OpenAiChatService
{
    private readonly ChatClient _client;

    public OpenAiChatService(IConfiguration config)
    {
        var apiKey = config["OPENAI_API_KEY"]!;
        _client = new ChatClient(model: "gpt-4o", apiKey: apiKey);
    }

    public AssistantChatMessage GetReply(List<ChatMessage> messages)
    {
        ChatCompletion completion = _client.CompleteChat(messages);
        return new AssistantChatMessage(completion);
    }

    public string GeneratePolicyText(List<ChatMessage> messages)
    {
        messages.Add(new UserChatMessage("""
            Згенеруй, будь ласка, повний текст страхового поліса на основі попередньої розмови та даних з документів.
            Не додавай жодних пояснень чи привітань — тільки повний текст поліса.

            Ось структура, якої потрібно дотримуватись:

            СТРАХОВИЙ ПОЛІС № UA-YYYY-NNNNNN

            1. Дані страхувальника:
               - ПІБ: <Ім’я Прізвище>
               - Номер паспорта: <AB123456>
               - Дата народження: <ДД.ММ.РРРР>
               - Громадянство: <країна>

            2. Дані транспортного засобу:
               - Дата реєстрації: <ДД.ММ.РРРР>
               - Номер реєстрації: <AA0000BB>
               - Ім'я власника: <Ім’я Прізвище>

            3. Умови страхування:
               - Тип полісу: Автоцивільна відповідальність
               - Термін дії: з <дата початку> до <дата завершення>
               - Страхова сума: 100 USD
               - Номер договору: <AUT-INS-XXXXXX>

            4. Додаткові умови:
               - Без франшизи
               - Територія покриття: Україна

            5. Підписано:
               - Віртуальний агент: AutoBot UA
               - Дата оформлення: <сьогоднішня дата>

            Усі дані мають бути сформовані на основі попередньої розмови.
            Документ повинен виглядати як офіційний друкований текст.
            """));


        ChatCompletion completion = _client.CompleteChat(messages);
        messages.Add(new AssistantChatMessage(completion));

        return completion.Content[0].Text;
    }
}
